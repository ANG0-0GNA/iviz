﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.XmlRpc;
using HttpListenerContext = Iviz.XmlRpc.HttpListenerContext;

namespace Iviz.Roslib.XmlRpc
{
    internal sealed class NodeServer : IDisposable
    {
        readonly Dictionary<string, Func<object[], Arg[]>> methods;
        readonly Dictionary<string, Func<object[], Task>> lateCallbacks;
        readonly Iviz.XmlRpc.HttpListener listener;
        readonly RosClient client;

        readonly SemaphoreSlim signal = new SemaphoreSlim(0, 1);
        Task task;

        public Uri ListenerUri => listener.LocalEndpoint;
        public Uri Uri => client.CallerUri;

        public NodeServer(RosClient client)
        {
            this.client = client;

            listener = new Iviz.XmlRpc.HttpListener(client.CallerUri);

            methods = new Dictionary<string, Func<object[], Arg[]>>
            {
                ["getBusStats"] = GetBusStats,
                ["getBusInfo"] = GetBusInfo,
                ["getMasterUri"] = GetMasterUri,
                ["shutdown"] = Shutdown,
                ["getSubscriptions"] = GetSubscriptions,
                ["getPublications"] = GetPublications,
                ["paramUpdate"] = ParamUpdate,
                ["publisherUpdate"] = PublisherUpdate,
                ["requestTopic"] = RequestTopic,
                ["getPid"] = GetPid,
            };

            lateCallbacks = new Dictionary<string, Func<object[], Task>>
            {
                ["publisherUpdate"] = PublisherUpdateLateCallback,
            };
        }

        public void Start()
        {
            task = Task.Run(async () => await Run());
        }

        public override string ToString()
        {
            return $"[RcpNodeServer {Uri}]";
        }

        async Task Run()
        {
            Logger.LogDebug($"{this}: Starting!");

            Task listenerTask = listener.StartAsync(StartContext);

            // wait until we're disposed
            await signal.WaitAsync();

            // tell the listener in every possible way to stop listening
            listener.Dispose();

            // and that is usually not enough. so we bail out
            if (!await listenerTask.WaitFor(2000))
            {
                Logger.LogDebug($"{this}: Listener stuck. Abandoning.");
            }

            Logger.LogDebug($"{this}: Leaving thread");
        }

        async Task StartContext(HttpListenerContext context)
        {
            using (context)
            {
                try
                {
                    await Service.MethodResponseAsync(context, methods, lateCallbacks).Caf();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        bool disposed;
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (task == null)
            {
                // not initialized, dispose directly
                listener.Dispose();
                return;
            }

            // tell task thread to dispose
            signal.Release();

            task.Wait();
        }


        static Arg[] OkResponse(Arg arg)
        {
            return new Arg[] {StatusCode.Success, "ok", arg};
        }

        Arg[] GetBusStats(object[] _)
        {
            Logger.Log("Was called: getBusStats");
            return new Arg[]
            {
                StatusCode.Error,
                "error=NYI",
                Array.Empty<Arg>()
            };
        }

        Arg[] GetBusInfo(object[] _)
        {
            var busInfo = client.GetBusInfoRcp();
            Arg[][] response = busInfo.Select(
                x => new Arg[]
                {
                    x.ConnectionId,
                    x.DestinationId,
                    x.Direction,
                    x.Transport,
                    x.Topic,
                    x.Connected,
                }).ToArray();

            return OkResponse(response);
        }

        Arg[] GetMasterUri(object[] _)
        {
            return OkResponse(client.MasterUri);
        }

        Arg[] Shutdown(object[] args)
        {
            if (client.ShutdownAction == null)
            {
                return new Arg[] {StatusCode.Failure, "No shutdown handler set", 0};
            }

            string callerId = (string) args[0];
            string reason = args.Length > 1 ? (string) args[1] : "";
            client.ShutdownAction(callerId, reason, out int status, out string response);

            return OkResponse(0);
        }

        static Arg[] GetPid(object[] _)
        {
            int id = Process.GetCurrentProcess().Id;

            return OkResponse(id);
        }

        Arg[] GetSubscriptions(object[] _)
        {
            var subscriptions = client.GetSubscriptionsRcp();
            return OkResponse(new Arg(subscriptions.Select(info => (info.Topic, info.Type))));
        }

        Arg[] GetPublications(object[] _)
        {
            var publications = client.GetPublicationsRcp();
            return OkResponse(new Arg(publications.Select(info => (info.Topic, info.Type))));
        }

        Arg[] ParamUpdate(object[] args)
        {
            if (client.ParamUpdateAction == null)
            {
                return OkResponse(0);
            }

            string callerId = (string) args[0];
            string parameterKey = (string) args[1];
            object parameterValue = args[2];
            client.ParamUpdateAction(callerId, parameterKey, parameterValue, out _, out _);

            return OkResponse(0);
        }

        static Arg[] PublisherUpdate(object[] args)
        {
            return OkResponse(0);
        }

        async Task PublisherUpdateLateCallback(object[] args)
        {
            if (args.Length < 3 ||
                !(args[1] is string topic) ||
                !(args[2] is object[] publishers))
            {
                return;
            }

            List<Uri> publisherUris = new List<Uri>();
            foreach (object publisherObj in publishers)
            {
                if (!(publisherObj is string publisherStr) ||
                    !Uri.TryCreate(publisherStr, UriKind.Absolute, out Uri publisherUri))
                {
                    Logger.Log($"{this}: Invalid uri '{publisherObj}'");
                    continue;
                }
                
                publisherUris.Add(publisherUri);
            }

            try
            {
                await client.PublisherUpdateRcpAsync(topic, publisherUris);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        Arg[] RequestTopic(object[] args)
        {
            if (args.Length < 3 ||
                !(args[0] is string callerId) ||
                !(args[1] is string topic) ||
                !(args[2] is object[] protocols))
            {
                return new Arg[]
                {
                    StatusCode.Error, "Failed to parse arguments", 0
                };
            }

            if (protocols.Length == 0)
            {
                return new Arg[]
                {
                    StatusCode.Failure, "No compatible protocols found", Array.Empty<string[]>()
                };
            }

            bool success = protocols.Any(entry =>
                entry is object[] protocol &&
                protocol.Length != 0 &&
                protocol[0] is string protocolName &&
                protocolName == "TCPROS"
            );

            if (!success)
            {
                return new Arg[]
                {
                    StatusCode.Failure, "Client only supports TCPROS", Array.Empty<string[]>()
                };
            }

            try
            {
                if (!client.RequestTopicRpc(callerId, topic, out string hostname, out int port))
                {
                    return new Arg[]
                    {
                        StatusCode.Failure, $"Client is not publishing topic '{topic}'", Array.Empty<string[]>()
                    };
                }

                return OkResponse(new Arg[] {"TCPROS", hostname, port});
            }
            catch (Exception e)
            {
                Logger.Log(e);
                return new Arg[]
                {
                    StatusCode.Error, "Unknown error: " + e.Message, Array.Empty<string[]>()
                };
            }
        }
    }
}