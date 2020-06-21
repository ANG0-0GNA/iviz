﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Iviz.Msgs;

namespace Iviz.RoslibSharp
{
    class ServiceSenderManager
    {
        public readonly Uri Uri;
        readonly TcpListener listener;
        readonly ServiceInfo serviceInfo;
        readonly Task task;
        readonly Action<IService> callback;
        volatile bool keepGoing;

        readonly List<ServiceSender> connections = new List<ServiceSender>();

        public ServiceSenderManager(ServiceInfo serviceInfo, string host, Action<IService> callback)
        {
            this.serviceInfo = serviceInfo;
            this.callback = callback;

            keepGoing = true;

            listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();

            IPEndPoint localEndpoint = (IPEndPoint)listener.LocalEndpoint;
            Uri = new Uri($"rosrpc://{host}:{localEndpoint.Port}/");
            Logger.Log("RosRpcServer: Starting at " + Uri);

            task = Task.Run(Run);
        }


        void Run()
        {
            try
            {
                while (keepGoing)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ServiceSender sender = new ServiceSender(serviceInfo, client, callback);
                    lock (connections)
                    {
                        connections.Add(sender);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("RosRcpServer: Stopped thread");
                Logger.Log(e);
            }
        }

        public void Cleanup()
        {
            lock (connections)
            {
                List<int> toDelete = new List<int>();
                for (int i = connections.Count - 1; i >= 0; i--)
                {
                    if (!connections[i].IsAlive)
                    {
                        toDelete.Add(i);
                    }
                }
                foreach (int id in toDelete)
                {
                    Logger.LogDebug($"{this}: Removing service connection with '{connections[id].Hostname}' - dead x_x");
                    connections[id].Stop();
                    connections.RemoveAt(id);
                }
            }
        }


        public void Stop()
        {
            lock (connections)
            {
                connections.ForEach(x => x.Stop());
                connections.Clear();
            }
            keepGoing = false;
            try
            {
                listener.Server.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }
            try
            {
                listener.Stop();
            }
            catch (Exception) { }
            task?.Wait();
        }
    }
}
