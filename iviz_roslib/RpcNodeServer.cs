﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.XmlRpc;

namespace Iviz.Roslib.XmlRpc
{
    internal sealed class NodeServer : IDisposable
    {
        static readonly Arg[] DefaultOkResponse = OkResponse(0);

        readonly RosClient client;
        readonly Dictionary<string, Func<object[], Task>> lateCallbacks;
        readonly HttpListener listener;
        readonly Dictionary<string, Func<object[], Arg[]>> methods;
        readonly SemaphoreSlim signal = new SemaphoreSlim(0, 1);

        Task? task;
        bool disposed;

        public NodeServer(RosClient client)
        {
            this.client = client;

            listener = new HttpListener(client.CallerUri.Port);

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
                ["getPid"] = GetPid
            };

            lateCallbacks = new Dictionary<string, Func<object[], Task>>
            {
                ["publisherUpdate"] = PublisherUpdateLateCallback
            };
        }

        public int ListenerPort => listener.LocalPort;
        Uri Uri => client.CallerUri;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            //Logger.LogDebug(this + ": Releasing signal!");
            
            // tell task thread to dispose
            signal.Release();

            if (task == null)
            {
                //Logger.LogDebug(this + ": Direct Disposing listener!");
                // not initialized, dispose directly
                listener.Dispose();
                return;
            }

            //Logger.LogDebugFormat("{0}: Waiting for task...", this);
            task.WaitNoThrow(this);
            //Logger.LogDebugFormat("{0}: Task done!", this);
        }

        public async Task DisposeAsync()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            //Logger.LogDebug(this + ": Releasing signal!");
            
            // tell task thread to dispose
            signal.Release();

            if (task == null)
            {
                //Logger.LogDebug(this + ": Direct Disposing listener!");
                // not initialized, dispose directly
                listener.Dispose();
                return;
            }

            //Logger.LogDebug(this + ": Waiting for task!");
            await task.AwaitNoThrow(this);
        }
        
        public void Start()
        {
            task = Task.Run(Run);
        }

        public override string ToString()
        {
            return $"[NodeServer {Uri}]";
        }

        async Task Run()
        {
            Logger.LogDebugFormat("{0}: Starting!", this);

            Task listenerTask = Task.Run(
                async() => await listener.StartAsync(StartContext, true).AwaitNoThrow(this));

            //Logger.LogDebug(this + ": Waiting for signal...");
            // wait until we're disposed
            await signal.WaitAsync().Caf();

            //Logger.LogDebug(this + ": Telling listener to stop listening...");
            // tell the listener to stop listening
            listener.Dispose();

            //Logger.LogDebug(this + ": Awaiting for RPC calls...");
            // wait for any remaining rpc calls
            await listener.AwaitRunningTasks().Caf();

            //Logger.LogDebug(this + ": Waiting...");
            // and that is usually not enough. so we bail out
            if (!await listenerTask.WaitFor(2000).Caf())
            {
                Logger.LogErrorFormat("{0}: Listener stuck. Abandoning.", this);
            }

            Logger.LogDebugFormat("{0}: Leaving thread", this);
        }

        async Task StartContext(HttpListenerContext context)
        {
            try
            {
                await XmlRpcService.MethodResponseAsync(context, methods, lateCallbacks).Caf();
            }
            catch (Exception e)
            {
                Logger.LogErrorFormat(Utils.GenericExceptionFormat, this, e);
            }
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
                info => new Arg[]
                {
                    info.ConnectionId,
                    info.DestinationId,
                    info.Direction,
                    info.Transport,
                    info.Topic,
                    info.Connected
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
            client.ShutdownAction(callerId, reason, out _, out _);

            return DefaultOkResponse;
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
                return DefaultOkResponse;
            }

            string callerId = (string) args[0];
            string parameterKey = (string) args[1];
            object parameterValue = args[2];
            client.ParamUpdateAction(callerId, parameterKey, parameterValue, out _, out _);

            return DefaultOkResponse;
        }

        static Arg[] PublisherUpdate(object[] args)
        {
            // processing happens in PublisherUpdateLateCallback
            return DefaultOkResponse;
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
                    !Uri.TryCreate(publisherStr, UriKind.Absolute, out Uri? publisherUri) ||
                    publisherUri == null)
                {
                    Logger.LogFormat("{0}: Invalid uri '{1}'", this, publisherObj);
                    continue;
                }

                publisherUris.Add(publisherUri);
            }

            try
            {
                await client.PublisherUpdateRcpAsync(topic, publisherUris).Caf();
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
                return new Arg[] {StatusCode.Error, "Failed to parse arguments", 0};
            }

            if (protocols.Length == 0)
            {
                return new Arg[] {StatusCode.Failure, "No compatible protocols found", 0};
            }

            bool success = protocols.Any(entry =>
                entry is object[] protocol &&
                protocol.Length != 0 &&
                protocol[0] is string protocolName &&
                protocolName == "TCPROS"
            );

            if (!success)
            {
                return new Arg[] {StatusCode.Failure, "Client only supports TCPROS", 0};
            }

            Endpoint? endpoint;
            try
            {
                endpoint = client.RequestTopicRpc(callerId, topic);
            }
            catch (Exception e)
            {
                Logger.Log(e);
                return new Arg[] {StatusCode.Error, $"Unknown error: {e.Message}", 0};
            }

            return endpoint?.Hostname == null
                ? new Arg[] {StatusCode.Error, "Internal error [duplicate request]", 0}
                : OkResponse(new Arg[] {"TCPROS", endpoint.Hostname, endpoint.Port});
        }
    }
}