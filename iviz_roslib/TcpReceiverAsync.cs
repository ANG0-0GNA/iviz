﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.XmlRpc;
using Nito.AsyncEx.Synchronous;
using Buffer = Iviz.Msgs.Buffer;

namespace Iviz.Roslib
{
    internal sealed class TcpReceiverAsync<T> : IDisposable where T : IMessage
    {
        const int BufferSizeIncrease = 1024;
        const int MaxConnectionRetries = 60;

        const int WaitBetweenRetriesInMs = 1000;
        //const int SleepTimeInMs = 1000;

        readonly TcpReceiverManager<T> manager;

        //readonly SemaphoreSlim signal = new SemaphoreSlim(0, 1);
        readonly TopicInfo<T> topicInfo;
        readonly bool requestNoDelay;

        Endpoint remoteEndpoint;
        Endpoint endpoint;
        int numReceived;
        int bytesReceived;

        // TODO: replace this with cancellation token
        //volatile bool keepRunning;
        readonly CancellationTokenSource runningTs = new CancellationTokenSource();
        bool KeepRunning => !runningTs.IsCancellationRequested;

        byte[] readBuffer = new byte[BufferSizeIncrease];
        NetworkStream stream;
        Task task;
        TcpClient tcpClient;
        bool disposed;

        public TcpReceiverAsync(TcpReceiverManager<T> manager,
            Uri remoteUri, Endpoint remoteEndpoint, TopicInfo<T> topicInfo,
            bool requestNoDelay)
        {
            RemoteUri = remoteUri;
            this.remoteEndpoint = remoteEndpoint;
            this.topicInfo = topicInfo;
            this.manager = manager;
            this.requestNoDelay = requestNoDelay;
        }

        public Uri RemoteUri { get; }
        string Topic => topicInfo.Topic;
        public bool IsAlive => task != null && !task.IsCompleted;

        public SubscriberReceiverState State => new SubscriberReceiverState(
            IsAlive, requestNoDelay, endpoint,
            RemoteUri, remoteEndpoint,
            numReceived, bytesReceived
        );

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            runningTs.Cancel();
            //keepRunning = false;

            //try { signal.Release(); }
            //catch (SemaphoreFullException) { }

            //signal.Dispose();

            try
            {
                task?.WaitAndUnwrapException();
            }
            catch (Exception e)
            {
                Logger.Log($"{this}: {e}");
            }

            runningTs.Dispose();
            task = null;
        }

        public void Start(int timeoutInMs)
        { 
            task = Task.Run(async () => await Run(timeoutInMs).Caf());
        }

        async Task SerializeHeader()
        {
            string[] contents =
            {
                $"message_definition={topicInfo.MessageDefinition}",
                $"callerid={topicInfo.CallerId}",
                $"topic={topicInfo.Topic}",
                $"md5sum={topicInfo.Md5Sum}",
                $"type={topicInfo.Type}",
                $"tcp_nodelay={(requestNoDelay ? "1" : "0")}"
            };
            int totalLength = 4 * contents.Length + contents.Sum(entry => entry.Length);

            byte[] array = new byte[totalLength + 4];
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(array)))
            {
                writer.Write(totalLength);
                foreach (string entry in contents)
                {
                    writer.Write(entry.Length);
                    writer.Write(BuiltIns.UTF8.GetBytes(entry));
                }
            }

            await stream.WriteAsync(array, 0, array.Length).Caf();
        }

        List<string> ParseHeader(int totalLength)
        {
            int numRead = 0;

            List<string> contents = new List<string>();
            while (numRead < totalLength)
            {
                int length = BitConverter.ToInt32(readBuffer, numRead);
                numRead += 4;
                string entry = BuiltIns.UTF8.GetString(readBuffer, numRead, length);
                numRead += length;
                contents.Add(entry);
            }

            return contents;
        }

        async Task<int> ReceivePacket()
        {
            int numRead = 0;
            while (numRead < 4)
            {
                int readNow = await stream.ReadAsync(readBuffer, numRead, 4 - numRead, runningTs.Token).Caf();
                if (readNow == 0)
                {
                    return 0;
                }

                numRead += readNow;
            }

            int length = BitConverter.ToInt32(readBuffer, 0);
            if (readBuffer.Length < length)
            {
                readBuffer = new byte[length + BufferSizeIncrease];
            }

            numRead = 0;
            while (numRead < length)
            {
                int readNow = await stream.ReadAsync(readBuffer, numRead, length - numRead, runningTs.Token).Caf();
                if (readNow == 0)
                {
                    return 0;
                }

                numRead += readNow;
            }

            return length;
        }


        async Task<List<string>> DoHandshake()
        {
            await SerializeHeader().Caf();

            int receivedLength = await ReceivePacket().Caf();
            if (receivedLength == 0)
            {
                throw new TimeoutException("Connection closed before handshake finished.");
            }

            return ParseHeader(receivedLength);
        }

        async Task Run(int timeoutInMs)
        {
            Task runLoopTask = StartSession(timeoutInMs);

            /*
            while (keepRunning)
            {
                await signal.WaitAsync(SleepTimeInMs).Caf();
            }

            */

            await runLoopTask.Caf();

            stream?.Dispose();
            tcpClient?.Dispose();
        }

        async Task<TcpClient> TryToConnect(int timeoutInMs)
        {
            TcpClient client = new TcpClient();
            try
            {
                Task connectionTask = client.ConnectAsync(remoteEndpoint.Hostname, remoteEndpoint.Port);
                if (!await connectionTask.WaitFor(timeoutInMs).Caf() ||
                    !connectionTask.RanToCompletion() ||
                    client.Client?.LocalEndPoint == null)
                {
                    client.Dispose();
                    return null;
                }
            }
            catch (Exception e) when (e is IOException || e is SocketException)
            {
                client.Dispose();
                return null;
            }
            catch (Exception e)
            {
                Logger.Log($"{this}: {e}");
                client.Dispose();
                return null;
            }

            return client;
        }

        async Task<TcpClient> KeepReconnecting(int timeoutInMs)
        {
            for (int i = 0; i < MaxConnectionRetries && KeepRunning; i++)
            {
                var client = await TryToConnect(timeoutInMs).Caf();
                if (client != null)
                {
                    return client;
                }

                await Task.Delay(WaitBetweenRetriesInMs).Caf();
                if (!KeepRunning)
                {
                    return null;
                }

                Endpoint newEndpoint = await manager.RequestConnectionFromPublisherAsync(RemoteUri).Caf();
                if (newEndpoint == null || newEndpoint.Equals(remoteEndpoint))
                {
                    continue;
                }

                Logger.Log($"{this}: Changed endpoint from {remoteEndpoint} to {newEndpoint}");
                remoteEndpoint = newEndpoint;
            }

            return null;
        }

        async Task StartSession(int timeoutInMs)
        {
            while (KeepRunning)
            {
                tcpClient = null;

                Logger.LogDebug($"{this}: Trying to connect!");
                tcpClient = await KeepReconnecting(timeoutInMs).Caf();
                if (tcpClient == null)
                {
                    Logger.LogDebug(KeepRunning
                        ? $"{this}: Ran out of retries. Leaving!"
                        : $"{this}: Disposed! Getting out.");
                    break;
                }

                Logger.LogDebug($"{this}: Connected!");

                try
                {
                    using (tcpClient)
                    using (stream = tcpClient.GetStream())
                    {
                        endpoint = new Endpoint((IPEndPoint) tcpClient.Client.LocalEndPoint);
                        await ProcessLoop().Caf();
                    }
                }
                catch (Exception e) when (e is ObjectDisposedException || e is OperationCanceledException)
                {
                }
                catch (Exception e) when (e is IOException || e is SocketException || e is TimeoutException)
                {
                    Logger.LogDebug($"{this}: {e}");
                }
                catch (Exception e)
                {
                    Logger.Log($"{this}: {e}");
                }
            }

            tcpClient = null;
            stream = null;

            runningTs.Cancel();
            Logger.Log($"{this}: Stopped!");
        }

        async Task ProcessLoop()
        {
            if (!await ProcessHandshake().Caf())
            {
                return;
            }

            while (KeepRunning)
            {
                int rcvLength = await ReceivePacket().Caf();
                if (rcvLength == 0)
                {
                    Logger.LogDebug($"{this}: Partner closed connection.");
                    return;
                }

                T message = Buffer.Deserialize(topicInfo.Generator, readBuffer, rcvLength);
                manager.MessageCallback(message);

                numReceived++;
                bytesReceived += rcvLength + 4;
            }
        }

        async Task<bool> ProcessHandshake()
        {
            List<string> responses = await DoHandshake().Caf();
            if (responses.Count == 0 || !responses[0].HasPrefix("error"))
            {
                return true;
            }

            int index = responses[0].IndexOf('=');
            string errorMsg = index != -1 ? responses[0].Substring(index + 1) : responses[0];
            Logger.LogDebug($"{this}: Partner sent error code: {errorMsg}");
            return false;
        }

        public override string ToString()
        {
            return $"[TcpReceiver Uri={RemoteUri} {remoteEndpoint.Hostname}:{remoteEndpoint.Port} '{Topic}']";
        }
    }
}