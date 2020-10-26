﻿//#define DEBUG__

using System;
using System.Collections.Concurrent;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Buffer = Iviz.Msgs.Buffer;

namespace Iviz.Roslib
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SenderStatus
    {
        Inactive,
        Waiting,
        Active,
        Dead
    }

    internal sealed class TcpSenderAsync<T> : IDisposable where T : IMessage
    {
        const int BufferSizeIncrease = 1024;
        const int MinQueueSizeInPackets = 2;
        const int MaxConnectionRetries = 10;
        const int WaitBetweenRetriesInMs = 1000;

        readonly AsyncLock mutex = new AsyncLock();
        readonly List<T> messageQueue = new List<T>();
        
        readonly SemaphoreSlim signal = new SemaphoreSlim(0, 1);
        readonly TopicInfo<T> topicInfo;
        readonly bool latching;

        int bytesDropped;
        int bytesSent;
        bool disposed;
        Endpoint endpoint;
        Endpoint remoteEndpoint;
        volatile bool keepRunning;
        int numDropped;
        int numSent;
        SenderStatus status;
        NetworkStream stream;
        Task task;
        TcpClient tcpClient;
        TcpListener tcpListener;

        byte[] writeBuffer = new byte[BufferSizeIncrease];

        public TcpSenderAsync(string remoteCallerId, TopicInfo<T> topicInfo, bool latching)
        {
            RemoteCallerId = remoteCallerId;
            this.topicInfo = topicInfo;
            status = SenderStatus.Inactive;
            this.latching = latching;
        }

        public string RemoteCallerId { get; }
        string Topic => topicInfo.Topic;
        public bool IsAlive => task != null && !task.IsCompleted && !task.IsFaulted;
        public int MaxQueueSizeInBytes { get; set; } = 50000;

        int CurrentQueueSize => messageQueue.Count;

        public PublisherSenderState State =>
            new PublisherSenderState(
                IsAlive, latching, status,
                endpoint, RemoteCallerId, remoteEndpoint,
                CurrentQueueSize, MaxQueueSizeInBytes,
                numSent, bytesSent, numDropped, bytesDropped
            );

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            keepRunning = false;

            try { signal.Release(); }
            catch (SemaphoreFullException) { }

            try
            {
                task?.Wait();
            }
            catch (Exception e)
            {
                Logger.Log($"{this}: {e}");
            }
        }

        public Endpoint Start(int timeoutInMs, SemaphoreSlim managerSignal)
        {
            tcpListener = new TcpListener(IPAddress.Any, 0);
            tcpListener.Start();

            IPEndPoint localEndpoint = (IPEndPoint) tcpListener.LocalEndpoint;
            endpoint = new Endpoint(localEndpoint);

            keepRunning = true;
            task = Task.Run(async () => await Run(timeoutInMs, managerSignal));

            return endpoint;
        }

        static List<string> ParseHeader(byte[] readBuffer)
        {
            int numRead = 0;

            List<string> contents = new List<string>();
            while (numRead < readBuffer.Length)
            {
                int length = BitConverter.ToInt32(readBuffer, numRead);
                numRead += 4;
                string entry = BuiltIns.UTF8.GetString(readBuffer, numRead, length);
                numRead += length;
#if DEBUG__
                Logger.Log("<<< " + entry);
#endif
                contents.Add(entry);
            }

            return contents;
        }

        async Task<byte[]> ReceiveHeader()
        {
            byte[] lengthBuffer = new byte[4];
            int numRead = 0;
            while (numRead < 4)
            {
                int readNow = await stream.ReadAsync(lengthBuffer, numRead, 4 - numRead);
                if (readNow == 0)
                {
                    return null;
                }

                numRead += readNow;
            }

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] readBuffer = new byte[length];
            numRead = 0;
            while (numRead < length)
            {
                int readNow = await stream.ReadAsync(readBuffer, numRead, length - numRead);
                if (readNow == 0)
                {
                    return null;
                }

                numRead += readNow;
            }

            return readBuffer;
        }

        async Task SendResponseHeader(string errorMessage)
        {
            string[] contents;
            if (errorMessage != null)
            {
                contents = new[]
                {
                    errorMessage,
                    $"md5sum={topicInfo.Md5Sum}",
                    $"type={topicInfo.Type}",
                    $"callerid={topicInfo.CallerId}"
                };
            }
            else
            {
                contents = new[]
                {
                    $"md5sum={topicInfo.Md5Sum}",
                    $"type={topicInfo.Type}",
                    $"callerid={topicInfo.CallerId}",
                    $"latching={(latching ? "1" : "0")}"
                };
            }

            int totalLength = 4 * contents.Length;
            foreach (string entry in contents) totalLength += entry.Length;

            byte[] array = new byte[4 + totalLength];
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(array)))
            {
                writer.Write(totalLength);
                foreach (string entry in contents)
                {
                    writer.Write(entry.Length);
                    writer.Write(BuiltIns.UTF8.GetBytes(entry));
#if DEBUG__
                Logger.Log(">>> " + contents[i]);
#endif
                }
            }

            await stream.WriteAsync(array, 0, array.Length);
        }

        string ProcessRemoteHeader(IReadOnlyCollection<string> fields)
        {
            if (fields.Count < 5)
            {
                return "error=Expected at least 5 fields, closing connection";
            }

            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string field in fields)
            {
                int index = field.IndexOf('=');
                if (index < 0)
                {
                    return $"error=Invalid field '{field}'";
                }

                string key = field.Substring(0, index);
                values[key] = field.Substring(index + 1);
            }

            if (!values.TryGetValue("callerid", out string receivedId) || receivedId != RemoteCallerId)
            {
                return
                    $"error=Expected callerid '{RemoteCallerId}' but received instead '{receivedId}', closing connection";
            }

            if (!values.TryGetValue("topic", out string receivedTopic) || receivedTopic != topicInfo.Topic)
            {
                return
                    $"error=Expected topic '{topicInfo.Topic}' but received instead '{receivedTopic}', closing connection";
            }

            if (!values.TryGetValue("type", out string receivedType) || receivedType != topicInfo.Type)
            {
                if (receivedType == "*")
                {
                    Logger.LogDebug(
                        $"{this}: Expected type '{topicInfo.Type}' but received instead '{receivedType}'. Continuing...");
                }
                else
                {
                    return
                        $"error=Expected type '{topicInfo.Type}' but received instead '{receivedType}', closing connection";
                }
            }

            if (!values.TryGetValue("md5sum", out string receivedMd5Sum) || receivedMd5Sum != topicInfo.Md5Sum)
            {
                if (receivedMd5Sum == "*")
                {
                    Logger.LogDebug(
                        $"{this}: Expected md5 '{topicInfo.Md5Sum}' but received instead '{receivedMd5Sum}'. Continuing...");
                }
                else
                {
                    return
                        $"error=Expected md5 '{topicInfo.Md5Sum}' but received instead '{receivedMd5Sum}', closing connection";
                }
            }

            if (values.TryGetValue("tcp_nodelay", out string receivedNoDelay) && receivedNoDelay == "1")
            {
                tcpClient.NoDelay = true;
                Logger.LogDebug($"{this}: requested tcp_nodelay");
            }

            return null;
        }

        async Task<bool> ProcessHandshake()
        {
            byte[] readBuffer = await ReceiveHeader();
            if (readBuffer == null)
            {
                throw new TimeoutException("Connection closed during handshake.");
            }

            List<string> fields = ParseHeader(readBuffer);
            string errorMessage = ProcessRemoteHeader(fields);

            if (errorMessage != null)
            {
                Logger.Log($"{this}: Failed handshake\n{errorMessage}");
            }

            await SendResponseHeader(errorMessage);

            return errorMessage == null;
        }

        async Task Run(int timeoutInMs, SemaphoreSlim managerSignal)
        {
            Logger.LogDebug($"{this}: initialized!");
            status = SenderStatus.Waiting;


            for (int round = 0; round < MaxConnectionRetries && keepRunning; round++)
            {
                try
                {
                    Task<TcpClient> connectionTask = tcpListener.AcceptTcpClientAsync();
                    managerSignal?.Release();
                    managerSignal = null;

                    if (!keepRunning)
                    {
                        break;
                    }

                    if (!await connectionTask.WaitFor(timeoutInMs) || !connectionTask.IsCompleted)
                    {
                        Logger.Log(
                            $"{this}: Connection timed out (round {round + 1}/{MaxConnectionRetries}): {connectionTask.Exception}");
                        continue;
                    }

                    round = 0;
                    tcpClient = await connectionTask;

                    IPEndPoint remoteEndPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;
                    remoteEndpoint = new Endpoint(remoteEndPoint);

                    status = SenderStatus.Active;

                    Logger.LogDebug($"{this}: started!");
                    stream = tcpClient.GetStream();

                    using (tcpClient)
                    {
                        await ProcessLoop();
                    }
                }
                catch (Exception e) when
                    (e is IOException || e is TimeoutException || e is AggregateException || e is SocketException)
                {
                    Logger.LogDebug($"{this}: {e}");
                }
                catch (Exception e)
                {
                    Logger.Log($"{this}: {e}");
                }
            }

            status = SenderStatus.Dead;
            tcpListener?.Stop();
            tcpClient?.Dispose();
            tcpClient = null;
            stream = null;
        }

        async Task ProcessLoop()
        {
            if (!await ProcessHandshake())
            {
                keepRunning = false;
            }

            List<(T msg, int length)> localQueue = new List<(T, int)>();

            byte[] lengthArray = new byte[4];

            byte[] ToLengthArray(uint i)
            {
                lengthArray[0] = (byte) i;
                lengthArray[1] = (byte) (i >> 8);
                lengthArray[2] = (byte) (i >> 0x10);
                lengthArray[3] = (byte) (i >> 0x18);
                return lengthArray;
            }

            while (keepRunning)
            {
                await signal.WaitAsync(WaitBetweenRetriesInMs);
                if (!keepRunning)
                {
                    break;
                }

                if (messageQueue.Count == 0)
                {
                    continue;
                }

                localQueue.Clear();
                
                using (await mutex.LockAsync())
                {
                    foreach (T msg in messageQueue)
                    {
                        localQueue.Add((msg, msg.RosMessageLength));
                    }
                    messageQueue.Clear();
                }

                ApplyQueueSizeConstraint(localQueue, MinQueueSizeInPackets, MaxQueueSizeInBytes,
                    out int startIndex, out int newBytesDropped);

                numDropped += startIndex;
                bytesDropped += newBytesDropped;

                for (int i = startIndex; i < localQueue.Count; i++)
                {
                    T message = localQueue[i].msg;
                    int msgLength = localQueue[i].length;
                    if (writeBuffer.Length < msgLength)
                    {
                        writeBuffer = new byte[msgLength + BufferSizeIncrease];
                    }

                    uint sendLength = Buffer.Serialize(message, writeBuffer);
                    await stream.WriteAsync(ToLengthArray(sendLength), 0, 4);
                    await stream.WriteAsync(writeBuffer, 0, (int) sendLength);

                    numSent++;
                    bytesSent += (int) sendLength + 4;
                }
            }
        }

        public void Publish(in T message)
        {
            if (!IsAlive)
            {
                numDropped++;
                return;
            }

            using (mutex.Lock())
            {
                messageQueue.Add(message);
            }

            try { signal.Release(); }
            catch (SemaphoreFullException) { }
        }

        static void ApplyQueueSizeConstraint(List<(T msg, int length)> queue,
            int minQueueSizeInPackets, int maxQueueSizeInBytes,
            out int numDropped, out int bytesDropped)
        {
            // start discarding old messages
            int totalQueueSizeInBytes = 0;
            foreach ((T msg, int length) message in queue)
            {
                totalQueueSizeInBytes += message.length;
            }

            if (totalQueueSizeInBytes <= maxQueueSizeInBytes)
            {
                numDropped = 0;
                bytesDropped = 0;
                return;
            }

            int overflowInBytes = totalQueueSizeInBytes - maxQueueSizeInBytes;
            int toDrop = 0;
            while (toDrop < queue.Count - minQueueSizeInPackets && overflowInBytes > 0)
            {
                overflowInBytes -= queue[toDrop].length;
                toDrop++;
            }

            numDropped = toDrop;
            bytesDropped = totalQueueSizeInBytes - maxQueueSizeInBytes - overflowInBytes;
        }

        public override string ToString()
        {
            return $"[TcpSender :{endpoint.Port} '{Topic}' >>'{RemoteCallerId}']";
        }
    }
}