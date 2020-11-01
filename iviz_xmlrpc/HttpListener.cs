﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        bool disposed;
        bool keepGoing;
        
        /// <summary>
        /// Creates a new HTTP listener that listens on the given port.
        /// </summary>
        /// <param name="requestedPort">The port to listen on. Ports 0 and 80 are assumed to be 'any'.</param>
        public HttpListener(int requestedPort = AnyPort)
        {
            listener = new TcpListener(IPAddress.Any,
                requestedPort == DefaultHttpPort ? AnyPort : requestedPort);
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

            if (!keepGoing)
            {
                // not started, dispose directly
                listener.Stop();
                return;
            }

            keepGoing = false;

            // now we throw everything at the listener to try to leave AcceptTcpClientAsync()

            // first we enqueue a connection
            using (TcpClient client = new TcpClient())
            {
                Logger.LogDebug($"{this}: Using fake client");
                client.Connect(IPAddress.Loopback, LocalPort);
            }

            // now we close the listener
            Logger.LogDebug($"{this}: Stopping listener");
            listener.Stop();

            // now we close the underlying socket
            listener.Server.Close();

            // and hope that this is enough to leave AcceptTcpClientAsync()
            Logger.LogDebug($"{this}: Dispose out");
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
        public async Task StartAsync(Func<HttpListenerContext, Task> handler, bool runInBackground)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            keepGoing = true;
            while (keepGoing)
                try
                {
                    //Logger.LogDebug($"{this}: Accepting request...");
                    TcpClient client = await listener.AcceptTcpClientAsync().Caf();
                    //Logger.LogDebug($"{this}: Accept Out!");

                    if (!keepGoing)
                    {
                        client.Dispose();
                        break;
                    }

                    async Task CreateContextTask()
                    {
                        using var context = new HttpListenerContext(client);
                        await handler(context);
                    }

                    if (runInBackground)
                    {
                        AddToBackgroundTasks(CreateContextTask());
                    }
                    else
                    {
                        await CreateContextTask();
                    }
                }
                catch (ObjectDisposedException)
                {
                    Logger.LogDebug($"{this}: Leaving thread");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Log($"{this}: Leaving thread " + e);
                    break;
                }

            Logger.LogDebug($"{this}: Leaving thread normally");
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

            DateTime now = DateTime.Now;
            int count = backgroundTasks.Count(tuple => (tuple.start - now).TotalMilliseconds > BackgroundTimeoutInMs);
            if (count > 0)
            {
                Logger.Log($"{this}: There appear to be {count} tasks deadlocked!");
            }

            try
            {
                await Task.WhenAll(backgroundTasks.Select(tuple => tuple.task)).WaitFor(timeoutInMs);
            }
            catch (Exception e)
            {
                Logger.Log($"{this}: Got an exception while waiting: {e}");                
            }
        }

        public override string ToString()
        {
            return $"[HttpListener :{LocalPort}]";
        }
    }
}