﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;

namespace Iviz.XmlRpc
{
    /// <summary>
    /// A very simple HTTP listener for XML-RPC calls
    /// </summary>
    public sealed class HttpListener : IDisposable
    {
        const int AnyPort = 0;
        const int DefaultHttpPort = 80;
        const int DefaultTimeoutInMs = 2000;
        const int BackgroundTimeoutInMs = 5000;

        readonly List<(DateTime start, Task task)> backgroundTasks = new List<(DateTime, Task)>();
        readonly TcpListener listener;

        bool started;
        bool disposed;

        readonly CancellationTokenSource runningTs = new CancellationTokenSource();
        bool KeepRunning => !runningTs.IsCancellationRequested;

        /// <summary>
        /// Creates a new HTTP listener that listens on the given port.
        /// </summary>
        /// <param name="requestedPort">The port to listen on. Ports 0 and 80 are assumed to be 'any'.</param>
        public HttpListener(int requestedPort = AnyPort)
        {
            listener = new TcpListener(IPAddress.IPv6Any, requestedPort == DefaultHttpPort ? AnyPort : requestedPort)
                {Server = {DualMode = true}};
            listener.Start();

            IPEndPoint endpoint = (IPEndPoint) listener.LocalEndpoint;
            LocalPort = endpoint.Port;
        }

        /// <summary>
        /// The port on which the listener is listening
        /// </summary>
        public int LocalPort { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            runningTs.Cancel();

            if (!started)
            {
                listener.Stop();
                return;
            }

            Logger.LogDebugFormat("{0}: Disposing listener...", this);
            using (TcpClient client = new TcpClient(AddressFamily.InterNetworkV6)
                {Client = {DualMode = true, NoDelay = true}})
            {
                client.Connect(IPAddress.Loopback, LocalPort);
            }

            listener.Stop();
            Logger.LogDebugFormat("{0}: Listener dispose out", this);
            runningTs.Dispose();
        }

        /// <summary>
        /// Starts listening.
        /// </summary>
        /// <param name="handler">
        /// Function to call when a request arrives.
        /// The function should take the form 'async Task Handler(HttpListenerContext context) {}'
        /// Use await context.GetRequest() to get the request string.
        /// Use await context.Respond() to send the response.
        /// </param>
        /// <param name="runInBackground">
        /// If true, multiple requests can run at the same time.
        /// If false, the listener will wait for each request before accepting the next one.
        /// </param>
        /// <returns>An awaitable task.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handler is null</exception>
        public async Task StartAsync(Func<HttpListenerContext, CancellationToken, Task> handler, bool runInBackground)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            started = true;
            while (KeepRunning)
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().Caf();

                    if (!KeepRunning)
                    {
                        client.Dispose();
                        break;
                    }

                    async Task CreateContextTask()
                    {
                        using var context = new HttpListenerContext(client);
                        await handler(context, runningTs.Token);
                    }

                    if (runInBackground)
                    {
                        AddToBackgroundTasks(Task.Run(CreateContextTask, runningTs.Token));
                    }
                    else
                    {
                        await CreateContextTask().AwaitForWithTimeout(2000).AwaitNoThrow(this);
                    }
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException || e is OperationCanceledException)
                    {
                        break;
                    }

                    Logger.LogFormat("{0}: Leaving thread {1}", this, e);
                    return;
                }

            Logger.LogDebugFormat("{0}: Leaving thread normally", this);
        }


        void AddToBackgroundTasks(Task task)
        {
            backgroundTasks.RemoveAll(tuple => tuple.task.IsCompleted);
            backgroundTasks.Add((DateTime.Now, task));
        }

        /// <summary>
        /// If <see cref="StartAsync" /> was called with runInBackground,
        /// this waits for the handlers in the background to finish.
        /// </summary>
        /// <param name="timeoutInMs">Maximal time to wait</param>
        /// <returns>An awaitable task</returns>
        public async Task AwaitRunningTasks(int timeoutInMs = DefaultTimeoutInMs)
        {
            backgroundTasks.RemoveAll(tuple => tuple.task.IsCompleted);
            if (backgroundTasks.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.Now;
            int count = backgroundTasks.Count(tuple => (tuple.start - now).TotalMilliseconds > BackgroundTimeoutInMs);
            if (count > 0)
            {
                Logger.LogFormat("{0}: There appear to be {1} tasks deadlocked!", this, count);
            }

            try
            {
                await Task.WhenAll(backgroundTasks.Select(tuple => tuple.task)).AwaitFor(timeoutInMs);
            }
            catch (Exception e)
            {
                Logger.LogFormat("{0}: Got an exception while waiting: {1}", this, e);
            }
        }

        public override string ToString()
        {
            return $"[HttpListener :{LocalPort}]";
        }
    }
}