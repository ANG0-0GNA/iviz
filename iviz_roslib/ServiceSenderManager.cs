﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.XmlRpc;

namespace Iviz.Roslib
{
    internal sealed class ServiceSenderManager
    {
        public Uri Uri { get; }
        public string Service => serviceInfo.Service;
        public string ServiceType => serviceInfo.Type;
        readonly TcpListener listener;
        readonly ServiceInfo serviceInfo;
        readonly Func<IService, Task> callback;

        readonly Task task;
        readonly SemaphoreSlim signal = new SemaphoreSlim(0, 1);

        bool keepGoing;

        readonly ConcurrentDictionary<ServiceSenderAsync, object> connections =
            new ConcurrentDictionary<ServiceSenderAsync, object>();

        public ServiceSenderManager(ServiceInfo serviceInfo, string host, Func<IService, Task> callback)
        {
            this.serviceInfo = serviceInfo;
            this.callback = callback;

            keepGoing = true;

            listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();

            IPEndPoint localEndpoint = (IPEndPoint) listener.LocalEndpoint;
            Uri = new Uri($"rosrpc://{host}:{localEndpoint.Port}/");
            Logger.LogDebug($"{this}: Starting {serviceInfo.Service} [{serviceInfo.Type}] at {Uri}");

            task = Task.Run(StartAsync);
        }

        async Task StartAsync()
        {
            Task loopTask = RunLoop();
            await signal.WaitAsync();
            keepGoing = false;
            listener.Stop();
            if (!await loopTask.WaitFor(2000))
            {
                Logger.LogDebug($"{this}: Listener stuck. Abandoning.");
            }
        }

        async Task RunLoop()
        {
            try
            {
                while (keepGoing)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().Caf();
                    if (!keepGoing)
                    {
                        break;
                    }    

                    ServiceSenderAsync sender = new ServiceSenderAsync(serviceInfo, client, callback);
                    connections[sender] = null;
                    
                    Cleanup();
                }
            }
            catch (ObjectDisposedException)
            {
                Logger.LogDebug($"{this}: Leaving thread."); // expected
                return;
            }
            catch (Exception e)
            {
                Logger.Log($"{this}: Stopped thread" + e);
                return;
            }

            Logger.LogDebug($"{this}: Leaving thread (normally)"); // also expected
        }

        void Cleanup()
        {
            var toRemove = connections.Keys.Where(connection => !connection.IsAlive).ToArray();
            foreach (var connection in toRemove)
            {
                Logger.LogDebug(
                    $"{this}: Removing service connection with '{connection.Hostname}' - dead x_x");
                connection.Stop();
                connections.TryRemove(connection, out _);
            }
        }


        public void Stop()
        {
            connections.Keys.ForEach(sender => sender.Stop());
            connections.Clear();
            signal.Release();
            task?.Wait();
        }

        public override string ToString()
        {
            return $"[ServiceSenderManager {Service} [{ServiceType}] at {Uri}]";
        }
    }
}