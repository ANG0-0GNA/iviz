﻿#define LOG_ENABLED

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Core;
using Iviz.Msgs;
using Iviz.Roslib;
using Iviz.Roslib.XmlRpc;
using Iviz.XmlRpc;
using JetBrains.Annotations;
using UnityEngine;
using Logger = Iviz.Msgs.Logger;

namespace Iviz.Ros
{
    public sealed class RoslibConnection : RosConnection
    {
        static readonly ReadOnlyCollection<string> EmptyParameters = Array.Empty<string>().AsReadOnly();
        readonly List<IRosPublisher> publishers = new List<IRosPublisher>();
        readonly Dictionary<string, IAdvertisedTopic> publishersByTopic = new Dictionary<string, IAdvertisedTopic>();
        readonly Dictionary<string, IAdvertisedService> servicesByTopic = new Dictionary<string, IAdvertisedService>();
        readonly Dictionary<string, ISubscribedTopic> subscribersByTopic = new Dictionary<string, ISubscribedTopic>();

        [NotNull] ReadOnlyCollection<string> cachedParameters = EmptyParameters;
        [NotNull] ReadOnlyCollection<BriefTopicInfo> cachedTopics = EmptyTopics;
        [CanBeNull] RosClient client;

        CancellationTokenSource watchdogSource;
        Task watchdogTask;

        Uri masterUri;
        string myId;
        Uri myUri;

        [CanBeNull]
        public Uri MasterUri
        {
            get => masterUri;
            set
            {
                masterUri = value;
                Disconnect();
            }
        }

        [CanBeNull]
        public string MyId
        {
            get => myId;
            set
            {
                myId = value;
                Disconnect();
            }
        }

        [CanBeNull]
        public Uri MyUri
        {
            get => myUri;
            set
            {
                myUri = value;
                Disconnect();
            }
        }

        async Task DisposeClient()
        {
            if (client == null)
            {
                return;
            }

            await client.CloseAsync().AwaitNoThrow(this);
            client = null;
        }

        protected override async Task<bool> Connect()
        {
            if (MasterUri == null ||
                MasterUri.Scheme != "http" ||
                MyId == null ||
                MyUri == null ||
                MyUri.Scheme != "http")
            {
                return false;
            }

            if (client != null)
            {
                Debug.LogWarning("Warning: New client requested, but old client still running?!");
                await DisposeClient();
            }

            try
            {
#if LOG_ENABLED
                Logger.LogDebug = Core.Logger.Debug;
                Logger.LogError = Core.Logger.Error;
                Logger.Log = Core.Logger.Info;
#endif
                Core.Logger.Internal("Connecting...");

                RosClient newClient = new RosClient(MasterUri, MyId, MyUri, false);
                client = newClient;
                client.RosMasterApi.TimeoutInMs = 750;
                client.Parameters.TimeoutInMs = 750;
                
                await newClient.EnsureCleanSlateAsync();

                if (publishersByTopic.Count != 0 || subscribersByTopic.Count != 0)
                {
                    Core.Logger.Internal("Resubscribing and readvertising...");
                }

                Core.Logger.Debug("*** ReAdvertising...");
                await Task.WhenAll(publishersByTopic.Values.Select(ReAdvertise))
                    .WaitForWithTimeout(5000, "ReAdvertise task timed out");
                /*
                foreach (var publisher in publishersByTopic.Values)
                {
                    await ReAdvertise(publisher);
                }
                */
                Core.Logger.Debug("*** Done ReAdvertising");
                Core.Logger.Debug("*** Resubscribing...");
                await Task.WhenAll(subscribersByTopic.Values.Select(ReSubscribe))
                    .WaitForWithTimeout(5000, "ReSubscribe task timed out");
                /*
                foreach (var subscriber in subscribersByTopic.Values)
                {
                    await ReSubscribe(subscriber);
                }
                */
                Core.Logger.Debug("*** Done Resubscribing");
                Core.Logger.Debug("*** Requesting topics...");
                cachedTopics = await newClient.GetSystemPublishedTopicsAsync();
                Core.Logger.Debug("*** Done Requesting topics");

                Core.Logger.Debug("*** Advertising services...");
                /*
                foreach (var entry in servicesByTopic.Values)
                {
                    await entry.AdvertiseAsync(newClient);
                }
                */
                await Task.WhenAll(servicesByTopic.Values.Select(ReAdvertiseService))
                    .WaitForWithTimeout(5000, "ReAdvertiseService task timed out");
                Core.Logger.Debug("*** Done Advertising services!");
                Core.Logger.Debug("*** Connected!");

                Core.Logger.Internal("<b>Connected!</b>");

                watchdogSource = new CancellationTokenSource();
                watchdogTask = Task.Run(async () => await WatchdogAsync(newClient.MasterUri, newClient.CallerId,
                    newClient.CallerUri,
                    watchdogSource.Token));

                return true;
            }
            catch (Exception e) when
            (e is UnreachableUriException ||
             e is ConnectionException ||
             e is RosRpcException ||
             e is TimeoutException ||
             e is XmlRpcException)
            {
                Core.Logger.Internal("Error:", e);
                if (RosServerManager.IsActive && RosServerManager.MasterUri == MasterUri)
                {
                    Core.Logger.Internal(
                        "Note: This appears to be my own master. Are you sure the uri network is reachable?");
                }
            }
            catch (Exception e)
            {
                Core.Logger.Error("Exception during Connect():", e);
            }

            //Core.Logger.Debug("*** Disconnecting!");
            await DisconnectImpl();
            return false;
        }

        static async Task WatchdogAsync(
            [NotNull] Uri masterUri,
            [NotNull] string callerId,
            [NotNull] Uri callerUri,
            CancellationToken token)
        {
            RosMasterApi masterApi = new RosMasterApi(masterUri, callerId, callerUri);
            DateTime lastMasterAccess = DateTime.Now;
            bool warningSet = false;
            Uri lastRosOutUri = null;
            var instance = ConnectionManager.Connection;
            instance.SetConnectionWarningState(false);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    DateTime now = DateTime.Now;
                    try
                    {
                        LookupNodeResponse response = await masterApi.LookupNodeAsync("/rosout", token);

                        TimeSpan timeSinceLastAccess = now - lastMasterAccess;
                        lastMasterAccess = now;
                        if (warningSet)
                        {
                            Core.Logger.Internal("The master is visible again, but we may be out of sync. Restarting!");
                            instance.Disconnect();
                            break;
                        }

                        if (timeSinceLastAccess.TotalMilliseconds > 10000)
                        {
                            // we haven't seen the master in a while, but no error has been thrown
                            // by the routine that checks every 5 seconds. maybe the app was suspended?
                            Core.Logger.Internal(
                                "Haven't seen the master in a while. We may be out of sync. Restarting!");
                            instance.Disconnect();
                            break;
                        }

                        if (response.IsValid)
                        {
                            if (lastRosOutUri == null)
                            {
                                lastRosOutUri = response.Uri;
                            }
                            else if (lastRosOutUri != response.Uri)
                            {
                                Core.Logger.Internal("<b>Warning:</b> The master appears to have changed. Restarting!");
                                instance.Disconnect();
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //TimeSpan diff = now - lastMasterAccess;
                        if (!warningSet)
                        {
                            Core.Logger.Internal("<b>Warning:</b> The master is not responding. It was last seen at" +
                                                 $" [{lastMasterAccess:HH:mm:ss}].");
                            instance.SetConnectionWarningState(true);
                            warningSet = true;
                        }
                    }

                    await Task.Delay(5000, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            
            instance.SetConnectionWarningState(false);
        }

        async Task ReAdvertise([NotNull] IAdvertisedTopic topic)
        {
            await topic.AdvertiseAsync(client);
            topic.Id = publishers.Count;
            publishers.Add(topic.Publisher);
        }

        async Task ReSubscribe([NotNull] ISubscribedTopic topic)
        {
            await topic.SubscribeAsync(client);
        }

        async Task ReAdvertiseService([NotNull] IAdvertisedService service)
        {
            await service.AdvertiseAsync(client);
        }

        public override void Disconnect()
        {
            ClearTaskQueue();

            if (client == null)
            {
                Signal();
                return;
            }
            
            AddTask(DisconnectImpl);
        }

        async Task DisconnectImpl()
        {
            Core.Logger.Internal("Disconnecting...");
            await DisposeClient();
            Core.Logger.Internal("<b>Disconnected.</b>");

            if (watchdogTask != null)
            {
                watchdogSource.Cancel();
                await watchdogTask.AwaitNoThrow(this);
                watchdogSource = null;
                watchdogTask = null;
            }

            foreach (var entry in publishersByTopic.Values)
            {
                entry.Invalidate();
            }

            foreach (var entry in subscribersByTopic.Values)
            {
                entry.Invalidate();
            }

            publishers.Clear();
            base.Disconnect();
        }

        internal void Advertise<T>([NotNull] Sender<T> advertiser) where T : IMessage
        {
            if (advertiser == null)
            {
                throw new ArgumentNullException(nameof(advertiser));
            }

            AddTask(async () =>
            {
                try
                {
                    await AdvertiseImpl(advertiser);
                }
                catch (Exception e)
                {
                    Core.Logger.Error(e);
                }
            });
        }

        async Task AdvertiseImpl<T>([NotNull] Sender<T> advertiser) where T : IMessage
        {
            if (publishersByTopic.TryGetValue(advertiser.Topic, out var advertisedTopic))
            {
                advertisedTopic.Add(advertiser);
                advertiser.SetId(advertisedTopic.Id);
                return;
            }

            var newAdvertisedTopic = new AdvertisedTopic<T>(advertiser.Topic);

            int id;
            if (client != null)
            {
                await newAdvertisedTopic.AdvertiseAsync(client);

                var publisher = newAdvertisedTopic.Publisher;

                id = publishers.FindIndex(x => x is null);
                if (id == -1)
                {
                    id = publishers.Count;
                    publishers.Add(publisher);
                }
                else
                {
                    publishers[id] = publisher;
                }

                PublishedTopics = client.PublishedTopics;
            }
            else
            {
                id = -1;
            }

            newAdvertisedTopic.Id = id;
            publishersByTopic.Add(advertiser.Topic, newAdvertisedTopic);
            newAdvertisedTopic.Add(advertiser);
            advertiser.SetId(newAdvertisedTopic.Id);
        }

        public void AdvertiseService<T>([NotNull] string service, [NotNull] Action<T> callback)
            where T : IService, new()
        {
            AdvertiseService(service, (T t) =>
            {
                callback(t);
                return Task.CompletedTask;
            });
        }

        public void AdvertiseService<T>([NotNull] string service, [NotNull] Func<T, Task> callback)
            where T : IService, new()
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            AddTask(async () =>
            {
                try
                {
                    await AdvertiseServiceImpl(service, callback);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.AdvertiseService(): ", e);
                }
            });
        }

        async Task AdvertiseServiceImpl<T>([NotNull] string service, [NotNull] Func<T, Task> callback)
            where T : IService, new()
        {
            if (servicesByTopic.ContainsKey(service))
            {
                return;
            }

            Core.Logger.Info($"Advertising service <b>{service}</b> <i>[{BuiltIns.GetServiceType(typeof(T))}]</i>.");

            var newAdvertisedService = new AdvertisedService<T>(service, callback);

            if (client != null)
            {
                await newAdvertisedService.AdvertiseAsync(client);
            }

            servicesByTopic.Add(service, newAdvertisedService);
        }

        public override async Task<bool> CallServiceAsync<T>(string service, T srv, CancellationToken token)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (srv == null)
            {
                throw new ArgumentNullException(nameof(srv));
            }

            var signal = new SemaphoreSlim(0, 1);
            bool hasClient = false;
            Exception exception = null;

            AddTask(async () =>
            {
                try
                {
                    if (client == null)
                    {
                        return;
                    }

                    hasClient = true;
                    await client.CallServiceAsync(service, srv, true, token);
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    signal.Release();
                }
            });

            await signal.WaitAsync(token);
            if (exception != null)
            {
                throw exception;
            }

            return hasClient;
        }

        internal void Publish<T>([NotNull] Sender<T> advertiser, [NotNull] T msg) where T : IMessage
        {
            if (advertiser == null)
            {
                throw new ArgumentNullException(nameof(advertiser));
            }

            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }

            AddTask(async () =>
            {
                try
                {
                    PublishImpl(advertiser, msg);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.Publish(): ", e);
                }

                await Task.CompletedTask;
            });
        }


        void PublishImpl<T>([NotNull] ISender advertiser, T msg) where T : IMessage
        {
            if (advertiser.Id == -1)
            {
                return;
            }

            var basePublisher = publishers[advertiser.Id];
            if (basePublisher != null && basePublisher is IRosPublisher<T> publisher)
            {
                publisher.Publish(msg);
            }
        }

        internal bool TryGetResolvedTopicName([NotNull] ISender advertiser, [CanBeNull] out string topicName)
        {
            if (advertiser.Id == -1)
            {
                topicName = default;
                return false;
            }

            var basePublisher = publishers[advertiser.Id];
            topicName = basePublisher.Topic;
            return true;
        }

        internal void Subscribe<T>([NotNull] Listener<T> listener) where T : IMessage, IDeserializable<T>, new()
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            AddTask(async () =>
            {
                try
                {
                    await SubscribeImpl<T>(listener);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.Subscribe()", e);
                }
            });
        }

        async Task SubscribeImpl<T>([NotNull] IListener listener) where T : IMessage, IDeserializable<T>, new()
        {
            if (subscribersByTopic.TryGetValue(listener.Topic, out var subscribedTopic))
            {
                subscribedTopic.Add(listener);
                return;
            }

            var newSubscribedTopic = new SubscribedTopic<T>(listener.Topic);
            await newSubscribedTopic.SubscribeAsync(client, listener);
            subscribersByTopic.Add(listener.Topic, newSubscribedTopic);
        }

        internal void Unadvertise([NotNull] ISender advertiser)
        {
            if (advertiser == null)
            {
                throw new ArgumentNullException(nameof(advertiser));
            }

            AddTask(async () =>
            {
                try
                {
                    await UnadvertiseImpl(advertiser);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.Unadvertise()", e);
                }
            });
        }

        async Task UnadvertiseImpl([NotNull] ISender advertiser)
        {
            if (!publishersByTopic.TryGetValue(advertiser.Topic, out var advertisedTopic))
            {
                return;
            }

            advertisedTopic.Remove(advertiser);
            if (advertisedTopic.Count != 0)
            {
                return;
            }

            publishersByTopic.Remove(advertiser.Topic);
            if (advertiser.Id != -1)
            {
                publishers[advertiser.Id] = null;
            }

            if (client != null)
            {
                await advertisedTopic.UnadvertiseAsync(client);
                PublishedTopics = client.PublishedTopics;
            }
        }

        internal void Unsubscribe([NotNull] IListener subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            AddTask(async () =>
            {
                try
                {
                    await UnsubscribeImpl(subscriber);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.Unsubscribe()", e);
                }
            });
        }


        async Task UnsubscribeImpl([NotNull] IListener subscriber)
        {
            if (!subscribersByTopic.TryGetValue(subscriber.Topic, out var subscribedTopic))
            {
                return;
            }

            subscribedTopic.Remove(subscriber);
            if (subscribedTopic.Count == 0)
            {
                subscribersByTopic.Remove(subscriber.Topic);
                if (client != null)
                {
                    await subscribedTopic.UnsubscribeAsync(client);
                }
            }
        }

        [NotNull]
        public ReadOnlyCollection<BriefTopicInfo> GetSystemTopicTypes(
            RequestType type = RequestType.CachedButRequestInBackground)
        {
            if (type == RequestType.CachedOnly)
            {
                return cachedTopics;
            }

            AddTask(async () =>
            {
                try
                {
                    cachedTopics = client == null ? EmptyTopics : await client.GetSystemPublishedTopicsAsync();
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.GetSystemTopicTypes()", e);
                }
            });

            return cachedTopics;
        }

        [NotNull, ItemCanBeNull]
        public async Task<ReadOnlyCollection<BriefTopicInfo>> GetSystemTopicTypesAsync(int timeoutInMs,
            CancellationToken token = default)
        {
            SemaphoreSlim signal = new SemaphoreSlim(0, 1);

            AddTask(async () =>
            {
                try
                {
                    cachedTopics = client == null ? EmptyTopics : await client.GetSystemPublishedTopicsAsync(token);
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.GetSystemTopicTypes()", e);
                }
                finally
                {
                    signal.Release();
                }
            });

            return await signal.WaitAsync(timeoutInMs, token) ? cachedTopics : null;
        }

        [NotNull, ItemNotNull]
        public IEnumerable<string> GetSystemParameterList()
        {
            AddTask(async () =>
            {
                try
                {
                    if (client?.Parameters is null)
                    {
                        cachedParameters = EmptyParameters;
                        return;
                    }

                    cachedParameters = await client.Parameters.GetParameterNamesAsync();
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.GetSystemParameterList()", e);
                }
            });

            return cachedParameters;
        }

        [NotNull]
        public async Task<(object result, string errorMsg)> GetParameterAsync([NotNull] string parameter,
            int timeoutInMs, CancellationToken token = default)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            var signal = new SemaphoreSlim(0);
            object result = null;
            string errorMsg = null;

            AddTask(async () =>
            {
                try
                {
                    if (client?.Parameters == null)
                    {
                        errorMsg = "Not connected";
                        return;
                    }

                    var (success, param) = await client.Parameters.GetParameterAsync(parameter, token);
                    if (!success)
                    {
                        errorMsg = $"'{parameter}' not found";
                        return;
                    }

                    result = param;
                }
                catch (Exception e)
                {
                    Core.Logger.Error("Exception during RoslibConnection.GetParameter()", e);
                }
                finally
                {
                    signal.Release();
                }
            });

            if (!await signal.WaitAsync(timeoutInMs, token))
            {
                return (null, "Request timed out");
            }

            return (result, errorMsg);
        }

        public int GetNumPublishers([NotNull] string topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            subscribersByTopic.TryGetValue(topic, out var subscribedTopic);
            return subscribedTopic?.Subscriber?.NumPublishers ?? 0;
        }

        public int GetNumSubscribers([NotNull] string topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            publishersByTopic.TryGetValue(topic, out var advertisedTopic);
            return advertisedTopic?.Publisher?.NumSubscribers ?? 0;
        }

        internal override void Stop()
        {
            Disconnect();
            base.Stop();
        }

        public void GenerateReport(StringBuilder builder)
        {
            var mClient = client;
            if (mClient == null)
            {
                return;
            }

            var subscriberStats = mClient.GetSubscriberStatistics();
            var publisherStats = mClient.GetPublisherStatistics();

            foreach (var stat in subscriberStats.Topics)
            {
                builder.Append("<color=navy><b>** Subscribed to ").Append(stat.Topic).Append("</b></color>")
                    .AppendLine();
                builder.Append("<b>Type: </b><i>").Append(stat.Type).Append("</i>").AppendLine();

                long totalMessages = 0;
                long totalBytes = 0;
                foreach (var receiver in stat.Receivers)
                {
                    totalMessages += receiver.NumReceived;
                    totalBytes += receiver.BytesReceived;
                }

                long totalKbytes = totalBytes / 1000;
                builder.Append("<b>Received ").Append(totalMessages.ToString("N0")).Append(" msgs ↓")
                    .Append(totalKbytes.ToString("N0")).Append("kB</b> total").AppendLine();

                if (stat.Receivers.Count == 0)
                {
                    builder.Append("(None)").AppendLine();
                    builder.AppendLine();
                    continue;
                }

                foreach (var receiver in stat.Receivers)
                {
                    if (receiver.EndPoint == null &&
                        receiver.RemoteEndpoint == null &&
                        receiver.ErrorDescription == null)
                    {
                        builder.Append("<color=#808080><b>←</b> [")
                            .Append(receiver.RemoteUri.Host).Append(":")
                            .Append(receiver.RemoteUri.Port).Append("] (Unreachable)</color>")
                            .AppendLine();
                        continue;
                    }

                    bool isConnected = receiver.IsConnected;
                    bool isAlive = receiver.IsAlive;
                    builder.Append("<b>←</b> [");
                    if (receiver.RemoteUri == mClient.CallerUri)
                    {
                        builder.Append("<i>Me</i>] [");
                    }

                    builder.Append(receiver.RemoteUri.Host);
                    builder.Append(":").Append(receiver.RemoteUri.Port).Append("]");
                    if (isAlive && isConnected)
                    {
                        long kbytes = receiver.BytesReceived / 1000;
                        builder.Append(" ↓").Append(kbytes.ToString("N0")).Append("kB");
                    }
                    else if (!isAlive)
                    {
                        builder.Append(" <color=red>(dead)</color>");
                    }
                    else
                    {
                        builder.Append(" <color=navy>(Trying to connect...)</color>");
                    }

                    if (receiver.ErrorDescription != null)
                    {
                        builder.AppendLine();
                        builder.Append("<color=brown>\"").Append(receiver.ErrorDescription).Append("\"</color>");
                    }

                    builder.AppendLine();
                }

                builder.AppendLine();
            }

            foreach (var stat in publisherStats.Topics)
            {
                builder.Append("<color=maroon><b>** Publishing to ").Append(stat.Topic).Append("</b></color>")
                    .AppendLine();
                builder.Append("<b>Type: </b><i>").Append(stat.Type).Append("</i>").AppendLine();

                long totalMessages = 0;
                long totalBytes = 0;
                foreach (var sender in stat.Senders)
                {
                    totalMessages += sender.NumSent;
                    totalBytes += sender.BytesSent;
                }

                long totalKbytes = totalBytes / 1000;
                builder.Append("<b>Sent ").Append(totalMessages.ToString("N0")).Append(" msgs ↑")
                    .Append(totalKbytes.ToString("N0")).Append("kB</b> total").AppendLine();

                if (stat.Senders.Count == 0)
                {
                    builder.Append("(None)").AppendLine();
                    continue;
                }

                foreach (var receiver in stat.Senders)
                {
                    bool isAlive = receiver.IsAlive;
                    builder.Append("<b>→</b> ");
                    if (receiver.RemoteId == mClient.CallerId)
                    {
                        builder.Append("[<i>Me</i>] ");
                    }
                    else
                    {
                        builder.Append("[").Append(receiver.RemoteId).Append("] ");
                    }

                    builder.Append(receiver.RemoteEndpoint != null
                        ? receiver.RemoteEndpoint.Hostname
                        : "(Unknown address)");

                    if (isAlive)
                    {
                        long kbytes = receiver.BytesSent / 1000;
                        builder.Append(" ↑").Append(kbytes.ToString("N0")).Append("kB");
                    }
                    else
                    {
                        builder.Append(" <color=red>(dead)</color>");
                    }

                    builder.AppendLine();
                }

                builder.AppendLine();
            }
        }

        interface IAdvertisedTopic
        {
            [CanBeNull] IRosPublisher Publisher { get; }
            int Id { get; set; }
            int Count { get; }
            void Add([NotNull] ISender subscriber);
            void Remove([NotNull] ISender subscriber);
            Task AdvertiseAsync([CanBeNull] RosClient client);
            Task UnadvertiseAsync([NotNull] RosClient client);
            void Invalidate();
        }

        class AdvertisedTopic<T> : IAdvertisedTopic where T : IMessage
        {
            readonly HashSet<Sender<T>> senders = new HashSet<Sender<T>>();
            [NotNull] readonly string topic;
            int id;

            public AdvertisedTopic([NotNull] string topic)
            {
                this.topic = topic ?? throw new ArgumentNullException(nameof(topic));
            }

            public IRosPublisher Publisher { get; private set; }

            public int Id
            {
                get => id;
                set
                {
                    id = value;
                    foreach (var sender in senders)
                    {
                        sender.SetId(value);
                    }
                }
            }

            public void Add(ISender publisher)
            {
                senders.Add((Sender<T>) publisher);
            }

            public void Remove(ISender publisher)
            {
                senders.Remove((Sender<T>) publisher);
            }

            public int Count => senders.Count;

            public async Task AdvertiseAsync(RosClient client)
            {
                string fullTopic = topic[0] == '/' ? topic : $"{client?.CallerId}/{topic}";
                IRosPublisher publisher;
                if (client != null)
                {
                    (_, publisher) = await client.AdvertiseAsync<T>(fullTopic);
                }
                else
                {
                    publisher = null;
                }

                Publisher = publisher;
            }

            public async Task UnadvertiseAsync(RosClient client)
            {
                if (client == null)
                {
                    throw new ArgumentNullException(nameof(client));
                }

                var fullTopic = topic[0] == '/' ? topic : $"{client.CallerId}/{topic}";
                if (Publisher != null)
                {
                    await Publisher.UnadvertiseAsync(fullTopic);
                }
            }

            public void Invalidate()
            {
                Id = -1;
                Publisher = null;
            }

            [NotNull]
            public override string ToString()
            {
                return $"[AdvertisedTopic '{topic}']";
            }
        }

        interface ISubscribedTopic
        {
            [CanBeNull] IRosSubscriber Subscriber { get; }
            int Count { get; }
            void Add([NotNull] IListener subscriber);
            void Remove([NotNull] IListener subscriber);
            Task SubscribeAsync([CanBeNull] RosClient client, [CanBeNull] IListener listener = null);
            Task UnsubscribeAsync([NotNull] RosClient client);
            void Invalidate();
        }

        class SubscribedTopic<T> : ISubscribedTopic where T : IMessage, IDeserializable<T>, new()
        {
            readonly HashSet<Listener<T>> listeners = new HashSet<Listener<T>>();
            [NotNull] readonly string topic;

            public SubscribedTopic([NotNull] string topic)
            {
                this.topic = topic ?? throw new ArgumentNullException(nameof(topic));
            }

            public IRosSubscriber Subscriber { get; private set; }

            public void Add(IListener subscriber)
            {
                listeners.Add((Listener<T>) subscriber);
            }

            public void Remove(IListener subscriber)
            {
                listeners.Remove((Listener<T>) subscriber);
            }

            public async Task SubscribeAsync(RosClient client, IListener listener)
            {
                var fullTopic = topic[0] == '/' ? topic : $"{client?.CallerId}/{topic}";
                IRosSubscriber subscriber;
                if (listener != null)
                {
                    listeners.Add((Listener<T>) listener);
                }

                if (client != null)
                {
                    //Core.Logger.Debug(this + ": Calling SubscribeAsync");
                    (_, subscriber) = await client.SubscribeAsync<T>(fullTopic, Callback);
                }
                else
                {
                    subscriber = null;
                }

                Subscriber = subscriber;
            }

            public async Task UnsubscribeAsync(RosClient client)
            {
                var fullTopic = topic[0] == '/' ? topic : $"{client.CallerId}/{topic}";

                if (Subscriber != null)
                {
                    await Subscriber.UnsubscribeAsync(fullTopic);
                }
            }

            public int Count => listeners.Count;

            public void Invalidate()
            {
                Subscriber = null;
            }

            void Callback(T msg)
            {
                foreach (var listener in listeners)
                {
                    listener.EnqueueMessage(msg);
                }
            }

            [NotNull]
            public override string ToString()
            {
                return $"[SubscribedTopic '{topic}']";
            }
        }

        interface IAdvertisedService
        {
            Task AdvertiseAsync([CanBeNull] RosClient client);
        }

        class AdvertisedService<T> : IAdvertisedService where T : IService, new()
        {
            [NotNull] readonly Func<T, Task> callback;
            [NotNull] readonly string service;

            public AdvertisedService([NotNull] string service, [NotNull] Func<T, Task> callback)
            {
                this.service = service ?? throw new ArgumentNullException(nameof(service));
                this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public async Task AdvertiseAsync(RosClient client)
            {
                var fullService = service[0] == '/' ? service : $"{client?.CallerId}/{service}";
                if (client != null)
                {
                    await client.AdvertiseServiceAsync(fullService, callback);
                }
            }

            [NotNull]
            public override string ToString()
            {
                return $"[AdvertisedService '{service}']";
            }
        }
    }
}