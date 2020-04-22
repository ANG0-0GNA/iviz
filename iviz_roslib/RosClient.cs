﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Iviz.Msgs;

namespace Iviz.RoslibSharp
{
    public class RosClient
    {
        public readonly string CallerId;
        internal readonly RpcMaster Master;
        internal readonly RpcNodeClient Talker;
        internal readonly RpcNodeServer Listener;

        readonly Dictionary<string, RosSubscriber> subscribersByTopic = new Dictionary<string, RosSubscriber>();
        readonly Dictionary<string, RosPublisher> publishersByTopic = new Dictionary<string, RosPublisher>();

        public delegate void ShutdownActionCall(
            string callerId, string reason,
            out RpcStatusCode status, out string response);

        /// <summary>
        /// Handler of 'shutdown' XMLRPC calls from the slave API
        /// </summary>
        public ShutdownActionCall ShutdownAction { get; set; }

        public delegate void ParamUpdateActionCall(
            string callerId, string parameterKey, object parametervalue,
            out RpcStatusCode status, out string response);

        /// <summary>
        /// Handler of 'paramUpdate' XMLRPC calls from the slave API
        /// </summary>
        public ParamUpdateActionCall ParamUpdateAction { get; set; }

        /// <summary>
        /// URI of the master node.
        /// </summary>
        public Uri MasterUri => Master.Uri;

        /// <summary>
        /// URI of this node.
        /// </summary>
        public Uri CallerUri { get; }

        /// <summary>
        /// Constructs and connects a ROS client.
        /// </summary>
        /// <param name="masterUri">URI to the master node. Example: new Uri("http://localhost:11311")</param>
        /// <param name="callerId">The ROS name of this node</param>
        /// <param name="callerUri">URI of this node. Leave empty to generate one automatically</param>
        public RosClient(Uri masterUri, string callerId = null, Uri callerUri = null)
        {
            if (callerUri.Scheme != "http" || masterUri.Scheme != "http")
            {
                throw new ArgumentException("URI scheme must be http for the master and the caller");
            }

            if (callerUri == null)
            {
                callerUri = new Uri($"http://localhost:7613/");
            }

            /*
            if (callerUri.Host == "localhost")
            {
                    callerUri = new Uri($"http://{Dns.GetHostName()}:{callerUri.Port}{callerUri.AbsolutePath}");
            }
            */

            CallerId = callerId ?? "/RosClient";
            CallerUri = callerUri;



            Listener = new RpcNodeServer(this);
            Listener.Start();

            Master = new RpcMaster(masterUri, CallerId, CallerUri);
            Talker = new RpcNodeClient(CallerId, CallerUri);

            Logger.Log($"Starting: My id is {CallerId}, my uri is {CallerUri}, and I'm talking to {MasterUri}");

            try
            {
                // check if the system thinks we are subscribed to or advertise topic, possibly from a previous session
                // if so, remove them
                EnsureCleanSlate();
            }
            catch (WebException e)
            {
                throw new ArgumentException($"RosClient: Failed to contact the master URI '{masterUri.ToString()}'", e);
            }
        }

        /// <summary>
        /// Constructs and connects a ROS client.
        /// </summary>
        /// <param name="masterUri">URI to the master node. Example: "http://localhost:11311"</param>
        /// <param name="callerId">The name of this node</param>
        /// <param name="callerUri">URI of this node. Leave empty to generate one automatically</param>
        public RosClient(string masterUri, string callerId = null, string callerUri = null) :
            this(new Uri(masterUri), callerId, callerUri == null ? null : new Uri(callerUri))
        {
        }

        /// <summary>
        /// Asks the master which topics we advertise and are subscribed to, and removes them.
        /// </summary>
        void EnsureCleanSlate()
        {
            SystemState state = GetSystemState();
            state.subscribers.
                Where(x => x.nodes.Contains(CallerId)).
                ForEach(x => Master.UnregisterSubscriber(x.name));
            state.publishers.
                Where(x => x.nodes.Contains(CallerId)).
                ForEach(x => Master.UnregisterPublisher(x.name));
        }

        RosSubscriber CreateSubscriber(string topic, bool requestNoDelay, Type type, IMessage generator)
        {
            TopicInfo topicInfo = new TopicInfo(CallerId, topic, type, generator);
            TcpReceiverManager manager = new TcpReceiverManager(topicInfo, requestNoDelay);
            RosSubscriber subscription = new RosSubscriber(this, manager);

            lock (subscribersByTopic)
            {
                subscribersByTopic[topic] = subscription;
            }

            RpcMaster.RegisterSubscriberResponse masterResponse = Master.RegisterSubscriber(topic, topicInfo.Type);
            manager.PublisherUpdateRpc(Talker, masterResponse.publishers);

            return subscription;
        }

        /// <summary>
        /// Subscribes to the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="callback">Function to be called when a message arrives.</param>
        /// <returns>A token that can be used to unadvertise from this publisher.</returns>
        public string Subscribe<T>(string topic, Action<T> callback, bool requestNoDelay = false)
            where T : IMessage, new()
        {
            return Subscribe(topic, callback, out RosSubscriber _, requestNoDelay);
        }

        /// <summary>
        /// Subscribes to the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="subscriber">
        /// The shared subscriber for this topic, used by all subscribers from this client.
        /// </param>
        /// <returns>A token that can be used to unadvertise from this publisher.</returns>
        public string Subscribe<T>(string topic, Action<T> callback, out RosSubscriber subscriber, bool requestNoDelay = false)
            where T : IMessage, new()
        {
            if (!TryGetSubscriber(topic, out subscriber))
            {
                subscriber = CreateSubscriber(topic, requestNoDelay, typeof(T), new T());
            }

            if (!subscriber.MessageTypeMatches(typeof(T)))
            {
                throw new ArgumentException("Incorrect message type.");
            }

            // local lambda wrapper for casting
            void wrapper(IMessage x) { callback((T)x); }

            return subscriber.Subscribe(wrapper);
        }

        public string Subscribe(string topic, Action<IMessage> callback, Type type, out RosSubscriber subscriber, bool requestNoDelay = false)
        {
            if (!typeof(IMessage).IsAssignableFrom(type))
            {
                throw new ArgumentException("Incorrect message type.");
            }

            if (!TryGetSubscriber(topic, out subscriber))
            {
                subscriber = CreateSubscriber(topic, requestNoDelay, type, BuiltIns.CreateGenerator(type));
            }

            if (!subscriber.MessageTypeMatches(type))
            {
                throw new ArgumentException("Incorrect message type.");
            }

            return subscriber.Subscribe(callback);
        }

        /// <summary>
        /// Unsubscribe from the given topic.
        /// </summary>
        /// <param name="topicId">Token returned by Subscribe().</param>
        /// <returns>Whether the unsubscription succeeded.</returns>
        public bool Unsubscribe(string topicId)
        {
            RosSubscriber subscriber;
            lock (subscribersByTopic)
            {
                subscriber = subscribersByTopic.Values.FirstOrDefault(x => x.ContainsId(topicId));
            }
            if (subscriber == null)
            {
                return false;
            }
            return subscriber.Unsubscribe(topicId);
        }

        internal void RemoveSubscriber(RosSubscriber subscriber)
        {
            subscribersByTopic.Remove(subscriber.Topic);
            Master.UnregisterSubscriber(subscriber.Topic);
        }

        /// <summary>
        /// Tries to retrieve the subscriber for the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="subscriber">Subscriber for the given topic.</param>
        /// <returns>Whether the subscriber was found.</returns>
        public bool TryGetSubscriber(string topic, out RosSubscriber subscriber)
        {
            lock (subscribersByTopic)
            {
                return subscribersByTopic.TryGetValue(topic, out subscriber);
            }
        }

        /// <summary>
        /// Retrieves the subscriber for the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <returns></returns>
        public RosSubscriber GetSubscriber(string topic)
        {
            if (TryGetSubscriber(topic, out RosSubscriber subscriber))
            {
                return subscriber;
            }
            else
            {
                throw new KeyNotFoundException($"Cannot find subscriber for topic '{topic}'");
            }
        }

        RosPublisher CreatePublisher(string topic, Type type)
        {
            TopicInfo topicInfo = new TopicInfo(CallerId, topic, type);
            TcpSenderManager manager = new TcpSenderManager(topicInfo, CallerUri);
            RosPublisher publisher = new RosPublisher(this, manager);

            lock (publishersByTopic)
            {
                publishersByTopic[topic] = publisher;
            }

            Master.RegisterPublisher(topic, topicInfo.Type);

            return publisher;
        }

        /// <summary>
        /// Advertises the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="publisher">
        /// The shared publisher for this topic, used by all advertisers from this client.
        /// Use this structure to publish messages to your topic.
        /// </param>
        /// <returns>A token that can be used to unadvertise from this publisher.</returns>
        public string Advertise<T>(string topic, out RosPublisher publisher) where T : IMessage
        {
            return Advertise(topic, typeof(T), out publisher);
        }

        public string Advertise(string topic, Type type, out RosPublisher publisher)
        {
            if (!TryGetPublisher(topic, out publisher))
            {
                publisher = CreatePublisher(topic, type);
            }
            return publisher.Advertise();
        }

        /// <summary>
        /// Unadvertise the given topic.
        /// </summary>
        /// <param name="topicId">Token returned by Advertise().</param>
        /// <returns>Whether the unadvertisement succeeded.</returns>
        public bool Unadvertise(string topicId)
        {
            RosPublisher publisher;
            lock (publishersByTopic)
            {
                publisher = publishersByTopic.Values.FirstOrDefault(x => x.ContainsId(topicId));
            }
            if (publisher == null)
            {
                return false;
            }
            return publisher.Unadvertise(topicId);
        }

        internal void RemovePublisher(RosPublisher publisher)
        {
            publishersByTopic.Remove(publisher.Topic);
            Master.UnregisterPublisher(publisher.Topic);
        }

        /// <summary>
        /// Tries to retrieve the publisher of the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="publisher">Publisher of the given topic.</param>
        /// <returns>Whether the publisher was found.</returns>
        public bool TryGetPublisher(string topic, out RosPublisher publisher)
        {
            lock (publishersByTopic)
            {
                return publishersByTopic.TryGetValue(topic, out publisher);
            }
        }

        /// <summary>
        /// Retrieves the publisher of the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <returns></returns>
        public RosPublisher GetPublisher(string topic)
        {
            if (TryGetPublisher(topic, out RosPublisher publisher))
            {
                return publisher;
            }
            else
            {
                throw new KeyNotFoundException($"Cannot find publisher for topic '{topic}'");
            }
        }

        [Serializable]
        public class PublishedTopic : JsonToString
        {
            public readonly string topic;
            public readonly string type;

            public PublishedTopic(string topic, string type)
            {
                this.topic = topic;
                this.type = type;
            }
        }

        public PublishedTopic[] GetSystemPublishedTopics()
        {
            return Master.GetPublishedTopics().topics.
                Select(x => new PublishedTopic(x.Item1, x.Item2)).ToArray();
        }

        /// <summary>
        /// Asks the master for the nodes and topics in the system.
        /// Corresponds to the function 'getSystemState' in the ROS Master API.
        /// </summary>
        /// <returns></returns>
        public SystemState GetSystemState()
        {
            RpcMaster.GetSystemStateResponse response = Master.GetSystemState();
            return new SystemState(response.publishers, response.subscribers, response.services);
        }

        internal string[][] GetSubscriptionsRcp()
        {
            lock (subscribersByTopic)
            {
                string[][] result = new string[subscribersByTopic.Count][];
                int i = 0;
                foreach (var entry in subscribersByTopic)
                {
                    result[i++] = new[] { entry.Key, entry.Value.TopicType };
                }
                return result;
            }
        }

        internal string[][] GetPublicationsRcp()
        {
            lock (publishersByTopic)
            {
                string[][] result = new string[publishersByTopic.Count][];
                int i = 0;
                foreach (var entry in publishersByTopic)
                {
                    result[i++] = new[] { entry.Key, entry.Value.TopicType };
                }
                return result;
            }
        }

        internal void PublisherUpdateRcp(string topic, Uri[] publishers)
        {
            if (!TryGetSubscriber(topic, out RosSubscriber subscriber))
            {
                Logger.Log($"{this}: PublisherUpdate called for nonexisting topic '{topic}'");
                return;
            }
            subscriber.PublisherUpdateRcp(Talker, publishers);
        }

        internal bool RequestTopicRpc(string remoteCallerId, string topic, out string hostname, out int port)
        {
            if (!TryGetPublisher(topic, out RosPublisher publisher))
            {
                Logger.Log($"{this}: '{remoteCallerId} is requesting nonexisting topic '{topic}'");
                hostname = null;
                port = 0;
                return false;
            }
            publisher.RequestTopicRpc(remoteCallerId, out hostname, out port);
            return true;
        }

        /// <summary>
        /// Close this connection. Unsubscribes and unadvertises all topics.
        /// </summary>
        public void Close()
        {
            lock (publishersByTopic)
            {
                RosPublisher[] publishers = publishersByTopic.Values.ToArray();
                publishers.ForEach(x => x.Stop());
                publishersByTopic.Clear();
            }
            lock (subscribersByTopic)
            {
                RosSubscriber[] subscribers = subscribersByTopic.Values.ToArray();
                subscribers.ForEach(x => x.Stop());
                subscribersByTopic.Clear();
            }
        }

        public SubscriberState GetSubscriberStatistics()
        {
            lock (subscribersByTopic)
                return new SubscriberState(subscribersByTopic.Values.Select(x => x.GetState()).ToArray());
        }

        public PublisherState GetPublisherStatistics()
        {
            lock (publishersByTopic)
                return new PublisherState(publishersByTopic.Values.Select(x => x.GetState()).ToArray());
        }

        [Serializable]
        internal class BusInfo : JsonToString
        {
            public readonly int connectionId;
            public readonly string destinationId;
            public readonly string direction;
            public readonly string transport;
            public readonly string topic;
            public readonly int connected;

            public BusInfo(int id, string destinationId, string direction, string transport, string topic, int status)
            {
                this.connectionId = id;
                this.destinationId = destinationId;
                this.direction = direction;
                this.transport = transport;
                this.topic = topic;
                this.connected = status;
            }
        }

        internal List<BusInfo> GetBusInfoRcp()
        {
            List<BusInfo> busInfos = new List<BusInfo>();
            SubscriberState sstate = GetSubscriberStatistics();
            foreach (var topic in sstate.topics)
            {
                foreach (var receiver in topic.receivers)
                {
                    busInfos.Add(new BusInfo(
                        busInfos.Count,
                        receiver.remoteUri,
                        "i", "TCPROS",
                        topic.topic,
                        1));
                }
            }
            PublisherState pstate = GetPublisherStatistics();
            foreach (var topic in pstate.topics)
            {
                foreach (var sender in topic.senders)
                {
                    busInfos.Add(new BusInfo(
                        busInfos.Count,
                        Master.LookupNode(sender.remoteId).uri,
                        "o", "TCPROS",
                        topic.topic,
                        1));
                }
            }
            return busInfos;
        }

    }
}
