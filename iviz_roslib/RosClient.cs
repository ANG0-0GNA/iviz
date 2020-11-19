﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Iviz.Msgs;
using Iviz.Msgs.StdMsgs;
using Iviz.Roslib.XmlRpc;
using Iviz.XmlRpc;
using Nito.AsyncEx.Synchronous;

namespace Iviz.Roslib
{
    public interface IRosClient : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        string CallerId { get; }
        string Advertise<T>(string topic, out IRosPublisher<T> publisher) where T : IMessage;
        Task<(string id, IRosPublisher<T> publisher)> AdvertiseAsync<T>(string topic) where T : IMessage;
        string Advertise(string topic, Type msgType, out IRosPublisher publisher);
        Task<(string id, IRosPublisher publisher)> AdvertiseAsync(string topic, Type msgType);

        string Subscribe<T>(string topic, Action<T> callback, out IRosSubscriber<T> subscriber,
            bool requestNoDelay = false) where T : IMessage, IDeserializable<T>, new();

        Task<(string id, IRosSubscriber<T> subscriber)>
            SubscribeAsync<T>(string topic, Action<T> callback, bool requestNoDelay = false)
            where T : IMessage, IDeserializable<T>, new();
    }

    /// <summary>
    /// Class that manages a client connection to a ROS master. 
    /// </summary>
    public sealed class RosClient : IRosClient
    {
        public const int AnyPort = 0;

        readonly NodeServer listener;

        readonly ConcurrentDictionary<string, IRosSubscriber> subscribersByTopic =
            new ConcurrentDictionary<string, IRosSubscriber>();

        readonly ConcurrentDictionary<string, IRosPublisher> publishersByTopic =
            new ConcurrentDictionary<string, IRosPublisher>();

        readonly ConcurrentDictionary<string, IServiceCaller> subscribedServicesByName =
            new ConcurrentDictionary<string, IServiceCaller>();

        readonly ConcurrentDictionary<string, IServiceRequestManager> advertisedServicesByName =
            new ConcurrentDictionary<string, IServiceRequestManager>();

        public delegate void ShutdownActionCall(
            string callerId, string reason,
            out int status, out string response);

        /// <summary>
        /// Handler of 'shutdown' XMLRPC calls from the slave API
        /// </summary>
        public ShutdownActionCall? ShutdownAction { get; set; }

        public delegate void ParamUpdateActionCall(
            string callerId, string parameterKey, object parametervalue,
            out int status, out string response);

        /// <summary>
        /// Handler of 'paramUpdate' XMLRPC calls from the slave API
        /// </summary>
        public ParamUpdateActionCall? ParamUpdateAction { get; set; }

        /// <summary>
        /// ID of this node.
        /// </summary>
        public string CallerId { get; }

        /// <summary>
        /// Wrapper for XML-RPC calls to the master.
        /// </summary>
        public Master Master { get; }

        /// <summary>
        /// Timeout in milliseconds for XML-RPC communications with the master.
        /// </summary>
        public TimeSpan RpcMasterTimeout
        {
            get => TimeSpan.FromMilliseconds(Master.TimeoutInMs);
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                Master.TimeoutInMs = (int) value.TotalMilliseconds;
            }
        }

        TimeSpan rpcNodeTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Timeout in milliseconds for XML-RPC communications with another node.
        /// </summary>
        public TimeSpan RpcNodeTimeout
        {
            get => rpcNodeTimeout;
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                rpcNodeTimeout = value;
            }
        }

        TimeSpan tcpRosTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Timeout in milliseconds for TCP-ROS communications (topics, services).
        /// </summary>
        public TimeSpan TcpRosTimeout
        {
            get => tcpRosTimeout;
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                tcpRosTimeout = value;
                int valueInMs = (int) value.TotalMilliseconds;
                foreach (IRosSubscriber subscriber in subscribersByTopic.Values)
                {
                    subscriber.TimeoutInMs = valueInMs;
                }

                foreach (IRosPublisher publisher in publishersByTopic.Values)
                {
                    publisher.TimeoutInMs = valueInMs;
                }
            }
        }


        /// <summary>
        /// Wrapper for XML-RPC calls to the master.
        /// </summary>
        public ParameterClient Parameters { get; }

        /// <summary>
        /// URI of the master node.
        /// </summary>
        public Uri MasterUri => Master.MasterUri;

        /// <summary>
        /// URI of this node.
        /// </summary>
        public Uri CallerUri { get; }


        /// <summary>
        /// Constructs and connects a ROS client.
        /// </summary>
        /// <param name="masterUri">
        /// URI to the master node. Example: new Uri("http://localhost:11311").
        /// </param>
        /// <param name="callerId">
        /// The ROS name of this node.
        /// This is your identity in the network, and must be unique. Example: /my_new_node
        /// Leave empty to generate one automatically.
        /// </param>
        /// <param name="callerUri">
        /// URI of this node.
        /// Other clients will use this address to connect to this node.
        /// Leave empty to generate one automatically. </param>
        /// <param name="ensureCleanSlate">Checks if masterUri has any previous subscriptions or advertisements, and unregisters them.</param>
        public RosClient(Uri? masterUri = null, string? callerId = null, Uri? callerUri = null,
            bool ensureCleanSlate = true)
        {
            masterUri ??= EnvironmentMasterUri;

            if (masterUri is null)
            {
                throw new ArgumentException("No valid master uri provided, and ROS_MASTER_URI is not set",
                    nameof(masterUri));
            }

            if (masterUri.Scheme != "http")
            {
                throw new ArgumentException("URI scheme must be http", nameof(masterUri));
            }

            callerUri ??= TryGetCallerUri();

            if (callerUri.Scheme != "http")
            {
                throw new ArgumentException("URI scheme must be http", nameof(callerUri));
            }

            callerId ??= CreateCallerId();

            if (!IsValidGlobalResourceName(callerId))
            {
                throw new ArgumentException($"Caller id '{callerId}' is not a valid global ROS resource name");
            }

            CallerId = callerId;
            CallerUri = callerUri;

            try
            {
                // Create an XmlRpc server. This will tell us fast whether the port is taken.
                listener = new NodeServer(this);
            }
            catch (SocketException e)
            {
                throw new ConnectionException($"Failed to bind to local URI '{callerUri}'", e);
            }

            if (CallerUri.Port == AnyPort || CallerUri.IsDefaultPort)
            {
                string absolutePath = Uri.UnescapeDataString(callerUri.AbsolutePath);
                CallerUri = new Uri($"http://{CallerUri.Host}:{listener.ListenerPort}{absolutePath}");
            }

            Master = new Master(masterUri, CallerId, CallerUri);
            Parameters = new ParameterClient(masterUri, CallerId, CallerUri);

            try
            {
                // Do a simple ping to the master. This will tell us whether the master is reachable.
                Master.GetUri();
            }
            catch (Exception e)
            {
                listener.Dispose();
                throw new ConnectionException($"Failed to contact the master URI '{masterUri}'", e);
            }

            // Start the XmlRpc server.
            listener.Start();

            NodeClient.GetPidResponse response;
            try
            {
                response = CreateTalker(CallerUri).GetPid();
            }
            catch (Exception e)
            {
                listener.Dispose();
                throw new UnreachableUriException($"My own uri '{CallerUri}' does not appear to be reachable!", e);
            }

            if (!response.IsValid)
            {
                Logger.LogErrorFormat("{0}: Failed to validate reachability response.", this);
            }
            else if (response.Pid != Process.GetCurrentProcess().Id)
            {
                listener.Dispose();
                throw new UnreachableUriException($"My uri '{CallerUri}' appears to belong to someone else!");
            }

            Logger.LogFormat("{0}: Initialized.", this);

            if (ensureCleanSlate)
            {
                EnsureCleanSlate();
            }
        }

        /// <summary>
        /// Constructs and connects a ROS client.
        /// </summary>
        /// <param name="masterUri">
        /// URI to the master node. Example: http://localhost:11311.
        /// </param>
        /// <param name="callerId">
        /// The ROS name of this node.
        /// This is your identity in the network, and must be unique. Example: /my_new_node
        /// Leave empty to generate one automatically.
        /// </param>
        /// <param name="callerUri">
        /// URI of this node.
        /// Other clients will use this address to connect to this node.
        /// Leave empty to generate one automatically. </param>
        /// <param name="ensureCleanSlate">Checks if masterUri has any previous subscriptions or advertisements, and unregisters them.</param>
        public RosClient(string? masterUri,
            string? callerId = null,
            string? callerUri = null,
            bool ensureCleanSlate = true) :
            this(masterUri != null ? new Uri(masterUri) : null,
                callerId,
                callerUri != null ? new Uri(callerUri) : null,
                ensureCleanSlate
            )
        {
        }

        /// <summary>
        /// Tries to create a unique id based on the project and computer name
        /// </summary>
        /// <returns>A more or less unique id</returns>
        public static string CreateCallerId()
        {
            string seed = EnvironmentCallerHostname + Assembly.GetCallingAssembly().GetName().Name;
            return $"/iviz_{seed.GetDeterministicHashCode():x8}";
        }

        /// <summary>
        /// Retrieves the environment variable ROS_HOSTNAME as a uri.
        /// If this fails, retrieves ROS_IP.
        /// </summary>
        public static string? EnvironmentCallerHostname =>
            Environment.GetEnvironmentVariable("ROS_HOSTNAME") ??
            Environment.GetEnvironmentVariable("ROS_IP");

        /// <summary>
        /// Try to retrieve a valid caller uri with the given port.
        /// Other clients will connect to this node using this address.
        /// If the port is 0, uses a random free port.
        /// </summary>
        public static Uri TryGetCallerUri(int usingPort = AnyPort)
        {
            string? envHostname = EnvironmentCallerHostname;
            if (envHostname != null)
            {
                return new Uri($"http://{envHostname}:{usingPort}/");
            }

            return GetUriFromInterface(NetworkInterfaceType.Wireless80211, usingPort) ??
                   GetUriFromInterface(NetworkInterfaceType.Ethernet, usingPort) ??
                   new Uri($"http://{Dns.GetHostName()}:{usingPort}/");
        }

        static IEnumerable<UnicastIPAddressInformation> GetInterfaceCandidates(NetworkInterfaceType type)
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(@interface => @interface.NetworkInterfaceType == type &&
                                     @interface.OperationalStatus == OperationalStatus.Up)
                .SelectMany(@interface => @interface.GetIPProperties().UnicastAddresses)
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork);
        }

        public static ReadOnlyCollection<Uri> GetCallerUriCandidates(int usingPort = AnyPort)
        {
            List<Uri> candidates = new List<Uri>();
            string? envHostname = EnvironmentCallerHostname;
            if (envHostname != null)
            {
                candidates.Add(new Uri($"http://{envHostname}:{usingPort}/"));
            }

            IEnumerable<Uri> GetInfosAsUri(NetworkInterfaceType type) =>
                GetInterfaceCandidates(type)
                    .Select(info => new Uri($"http://{info.Address}:{usingPort}/")).ToArray();

            candidates.AddRange(GetInfosAsUri(NetworkInterfaceType.Wireless80211));
            candidates.AddRange(GetInfosAsUri(NetworkInterfaceType.Ethernet));
            candidates.Add(new Uri($"http://{Dns.GetHostName()}:{usingPort}/"));

            return candidates.AsReadOnly();
        }

        static Uri? GetUriFromInterface(NetworkInterfaceType type, int usingPort)
        {
            UnicastIPAddressInformation? ipInfo = GetInterfaceCandidates(type).FirstOrDefault();
            return ipInfo is null ? null : new Uri($"http://{ipInfo.Address}:{usingPort}/");
        }

        /// <summary>
        /// Retrieves the environment variable ROS_MASTER_URI as a uri.
        /// </summary>
        public static Uri? EnvironmentMasterUri
        {
            get
            {
                string? envStr = Environment.GetEnvironmentVariable("ROS_MASTER_URI");
                if (envStr is null)
                {
                    return null;
                }

                if (Uri.TryCreate(envStr, UriKind.Absolute, out Uri? uri))
                {
                    return uri;
                }

                Logger.LogFormat("RosClient: Environment variable for master uri '{0}' is not a valid uri!", envStr);
                return null;
            }
        }

        /// <summary>
        /// Checks if the given name is a valid ROS resource name
        /// </summary>  
        public static bool IsValidResourceName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            static bool IsAlpha(char c) => ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z');

            if (!IsAlpha(name[0]) && name[0] != '/' && name[0] != '~')
            {
                return false;
            }

            for (int i = 1; i < name.Length; i++)
            {
                if (!IsAlpha(name[i]) && !char.IsDigit(name[i]) && name[i] != '_' && name[i] != '/')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the given name is a valid global ROS resource name
        /// </summary>         
        public static bool IsValidGlobalResourceName(string name)
        {
            return IsValidResourceName(name) && name[0] == '/';
        }

        /// <summary>
        /// Try to retrieve a valid master uri.
        /// </summary>        
        public static Uri TryGetMasterUri()
        {
            return EnvironmentMasterUri ?? new Uri("http://localhost:11311/");
        }

        /// <summary>
        /// Asks the master which topics we advertise and are subscribed to, and removes them.
        /// </summary>
        public void EnsureCleanSlate()
        {
            try
            {
                SystemState state = GetSystemState();
                foreach (TopicTuple tuple in state.Subscribers.Where(tuple => tuple.Members.Contains(CallerId)))
                {
                    Master.UnregisterSubscriber(tuple.Topic);
                }

                foreach (TopicTuple tuple in state.Publishers.Where(tuple => tuple.Members.Contains(CallerId)))
                {
                    Master.UnregisterPublisher(tuple.Topic);
                }
            }
            catch (Exception e)
            {
                throw new ConnectionException($"Failed to contact the master URI '{MasterUri}'", e);
            }
        }

        /// <summary>
        /// Asks the master which topics we advertise and are subscribed to, and removes them.
        /// If you are interested in the async version, make sure to set ensureCleanSlate to false in the constructor.
        /// </summary>
        public async Task EnsureCleanSlateAsync()
        {
            SystemState state = await GetSystemStateAsync().Caf();
            List<Task> tasks = new List<Task>();
            tasks.AddRange(
                state.Subscribers
                    .Where(tuple => tuple.Members.Contains(CallerId))
                    .Select(tuple => (Task) Master.UnregisterSubscriberAsync(tuple.Topic))
            );

            tasks.AddRange(
                state.Publishers
                    .Where(tuple => tuple.Members.Contains(CallerId))
                    .Select(tuple => Master.UnregisterPublisherAsync(tuple.Topic))
            );

            try
            {
                await Task.WhenAll(tasks).Caf();
            }
            catch (Exception e)
            {
                throw new ConnectionException($"Failed to contact the master URI '{MasterUri}'", e);
            }
        }

        internal NodeClient CreateTalker(Uri otherUri)
        {
            return new NodeClient(CallerId, CallerUri, otherUri, (int) RpcNodeTimeout.TotalMilliseconds);
        }

        (string id, RosSubscriber<T> subscriber)
            CreateSubscriber<T>(string topic, bool requestNoDelay, Action<T> firstCallback)
            where T : IMessage, IDeserializable<T>, new()
        {
            if (!IsValidResourceName(topic))
            {
                throw new ArgumentException($"'{topic}' is not a valid resource name");
            }

            TopicInfo<T> topicInfo = new TopicInfo<T>(CallerId, topic, new T());
            int timeoutInMs = (int) TcpRosTimeout.TotalMilliseconds;

            RosSubscriber<T> subscription = new RosSubscriber<T>(this, topicInfo, requestNoDelay, timeoutInMs);
            string id = subscription.Subscribe(firstCallback);

            subscribersByTopic[topic] = subscription;

            var masterResponse = Master.RegisterSubscriber(topic, topicInfo.Type);
            if (!masterResponse.IsValid)
            {
                subscribersByTopic.TryRemove(topic, out _);
                throw new RosRpcException(
                    $"Error registering publisher for topic {topic}: {masterResponse.StatusMessage}");
            }

            Task.Run(async () => await subscription.Manager.PublisherUpdateRpcAsync(masterResponse.Publishers).Caf())
                .WaitAndUnwrapException();

            return (id, subscription);
        }

        async Task<(string id, RosSubscriber<T> subscriber)>
            CreateSubscriberAsync<T>(string topic, bool requestNoDelay, Action<T> firstCallback)
            where T : IMessage, IDeserializable<T>, new()
        {
            if (!IsValidResourceName(topic))
            {
                throw new ArgumentException($"'{topic}' is not a valid resource name");
            }

            TopicInfo<T> topicInfo = new TopicInfo<T>(CallerId, topic, new T());
            int timeoutInMs = (int) TcpRosTimeout.TotalMilliseconds;

            RosSubscriber<T> subscription = new RosSubscriber<T>(this, topicInfo, requestNoDelay, timeoutInMs);
            string id = subscription.Subscribe(firstCallback);

            subscribersByTopic[topic] = subscription;

            var masterResponse = await Master.RegisterSubscriberAsync(topic, topicInfo.Type).Caf();
            if (!masterResponse.IsValid)
            {
                subscribersByTopic.TryRemove(topic, out _);
                throw new RosRpcException(
                    $"Error registering publisher for topic {topic}: {masterResponse.StatusMessage}");
            }

            await subscription.Manager.PublisherUpdateRpcAsync(masterResponse.Publishers).Caf();

            return (id, subscription);
        }

        /// <summary>
        /// Subscribes to the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="callback">Function to be called when a message arrives.</param>
        /// <param name="requestNoDelay">Whether a request of NoDelay should be sent.</param>
        /// <returns>A token that can be used to unsubscribe from this topic.</returns>
        public string Subscribe<T>(string topic, Action<T> callback, bool requestNoDelay = false)
            where T : IMessage, IDeserializable<T>, new()
        {
            return Subscribe(topic, callback, out RosSubscriber<T> _, requestNoDelay);
        }

        /// <summary>
        /// Subscribes to the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="callback">Function to be called when a message arrives.</param>
        /// <param name="subscriber">
        /// The shared subscriber for this topic, used by all subscribers from this client.
        /// </param>
        /// <param name="requestNoDelay">Whether a request of NoDelay should be sent.</param>
        /// <returns>A token that can be used to unsubscribe from this topic.</returns>
        public string Subscribe<T>(string topic, Action<T> callback, out RosSubscriber<T> subscriber,
            bool requestNoDelay = false)
            where T : IMessage, IDeserializable<T>, new()
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!TryGetSubscriber(topic, out IRosSubscriber baseSubscriber))
            {
                string id;
                (id, subscriber) = CreateSubscriber(topic, requestNoDelay, callback);
                return id;
            }

            RosSubscriber<T>? newSubscriber = baseSubscriber as RosSubscriber<T>;
            subscriber = newSubscriber ?? throw new InvalidMessageTypeException(
                $"There is already a subscriber with a different type [{baseSubscriber.TopicType}]");
            return subscriber.Subscribe(callback);
        }

        string IRosClient.Subscribe<T>(string topic, Action<T> callback, out IRosSubscriber<T> subscriber,
            bool requestNoDelay)
        {
            string id = Subscribe(topic, callback, out RosSubscriber<T> newSubscriber, requestNoDelay);
            subscriber = newSubscriber;
            return id;
        }

        /// <summary>
        /// Generic version of the subscriber function if the type is not known. You should probably use the templated versions.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="callback">Function to be called when a message arrives.</param>
        /// <param name="msgType">The C# message type.</param>
        /// <param name="subscriber">
        /// The shared subscriber for this topic, used by all subscribers from this client.
        /// </param>
        /// <param name="requestNoDelay">Whether a request of NoDelay should be sent.</param>
        /// <returns>A token that can be used to unsubscribe from this topic.</returns>
        public string Subscribe(string topic, Action<IMessage> callback, Type msgType, out IRosSubscriber subscriber,
            bool requestNoDelay = false)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (msgType == null)
            {
                throw new ArgumentNullException(nameof(msgType));
            }

            if (!TryGetSubscriber(topic, out IRosSubscriber baseSubscriber))
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo? baseMethod = GetType().GetMethod(nameof(CreateSubscriber), flags);
                MethodInfo? method = baseMethod?.MakeGenericMethod(msgType);
                if (method == null)
                {
                    throw new InvalidMessageTypeException("Type is not a message object");
                }

                object? subscriberObj = method.Invoke(this, flags, null,
                    new object[] {topic, requestNoDelay}, BuiltIns.Culture);

                subscriber = (IRosSubscriber?) subscriberObj ??
                             throw new InvalidMessageTypeException("Failed to call 'CreateSubscriber'!");
            }
            else
            {
                if (!baseSubscriber.MessageTypeMatches(msgType))
                {
                    throw new InvalidMessageTypeException(
                        $"Existing subscriber message type {baseSubscriber.TopicType} does not match the given type.");
                }

                subscriber = baseSubscriber;
            }

            return subscriber.Subscribe(callback);
        }

        /// <summary>
        /// Subscribes to the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="callback">Function to be called when a message arrives.</param>
        /// <param name="requestNoDelay">Whether a request of NoDelay should be sent.</param>
        /// <returns>A pair containing a token that can be used to unsubscribe from this topic, and the subscriber object.</returns>
        public async Task<(string id, RosSubscriber<T> subscriber)>
            SubscribeAsync<T>(string topic, Action<T> callback, bool requestNoDelay = false)
            where T : IMessage, IDeserializable<T>, new()
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!TryGetSubscriber(topic, out IRosSubscriber baseSubscriber))
            {
                return await CreateSubscriberAsync(topic, requestNoDelay, callback).Caf();
            }

            var newSubscriber = baseSubscriber as RosSubscriber<T>;
            RosSubscriber<T> subscriber = newSubscriber ?? throw new InvalidMessageTypeException(
                $"Existing subscriber message type {baseSubscriber.TopicType} does not match the given type.");
            return (subscriber.Subscribe(callback), subscriber);
        }

        async Task<(string id, IRosSubscriber<T> subscriber)>
            IRosClient.SubscribeAsync<T>(string topic, Action<T> callback, bool requestNoDelay)
        {
            return await SubscribeAsync(topic, callback, requestNoDelay);
        }

        /// <summary>
        /// Unsubscribe from the given topic.
        /// </summary>
        /// <param name="topicId">Token returned by Subscribe().</param>
        /// <returns>Whether the unsubscription succeeded.</returns>
        public bool Unsubscribe(string topicId)
        {
            if (topicId is null)
            {
                throw new ArgumentNullException(nameof(topicId));
            }

            var subscriber = subscribersByTopic.Values.FirstOrDefault(s => s.ContainsId(topicId));
            return subscriber != null && subscriber.Unsubscribe(topicId);
        }

        /// <summary>
        /// Unsubscribe from the given topic.
        /// </summary>
        /// <param name="topicId">Token returned by Subscribe().</param>
        /// <returns>Whether the unsubscription succeeded.</returns>
        public async Task<bool> UnsubscribeAsync(string topicId)
        {
            if (topicId is null)
            {
                throw new ArgumentNullException(nameof(topicId));
            }

            var subscriber = subscribersByTopic.Values.FirstOrDefault(s => s.ContainsId(topicId));
            return subscriber != null && await subscriber.UnsubscribeAsync(topicId).Caf();
        }

        internal void RemoveSubscriber(IRosSubscriber subscriber)
        {
            subscribersByTopic.TryRemove(subscriber.Topic, out _);
            Master.UnregisterSubscriber(subscriber.Topic);
        }

        internal async Task RemoveSubscriberAsync(IRosSubscriber subscriber)
        {
            subscribersByTopic.TryRemove(subscriber.Topic, out _);
            await Master.UnregisterSubscriberAsync(subscriber.Topic).Caf();
        }

        /// <summary>
        /// Tries to retrieve the subscriber for the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="subscriber">Subscriber for the given topic.</param>
        /// <returns>Whether the subscriber was found.</returns>
        public bool TryGetSubscriber(string topic, out IRosSubscriber subscriber)
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return subscribersByTopic.TryGetValue(topic, out subscriber!);
        }

        /// <summary>
        /// Retrieves the subscriber for the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <returns></returns>
        public IRosSubscriber GetSubscriber(string topic)
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (TryGetSubscriber(topic, out IRosSubscriber subscriber))
            {
                return subscriber;
            }

            throw new KeyNotFoundException($"Cannot find subscriber for topic '{topic}'");
        }

        RosPublisher<T> CreatePublisher<T>(string topic) where T : IMessage
        {
            if (!IsValidResourceName(topic))
            {
                throw new ArgumentException($"'{topic}' is not a valid resource name");
            }

            TopicInfo<T> topicInfo = new TopicInfo<T>(CallerId, topic);
            RosPublisher<T> publisher = new RosPublisher<T>(this, topicInfo, (int) TcpRosTimeout.TotalMilliseconds);

            publishersByTopic[topic] = publisher;

            RegisterPublisherResponse? response;
            try
            {
                response = Master.RegisterPublisher(topic, topicInfo.Type);
            }
            catch (Exception e)
            {
                publishersByTopic.TryRemove(topic, out _);
                throw new RosRpcException("Error registering publisher", e);
            }            

            if (response.IsValid)
            {
                return publisher;
            }

            publishersByTopic.TryRemove(topic, out _);
            throw new RosRpcException($"Error registering publisher: {response.StatusMessage}");
        }

        async Task<IRosPublisher> CreatePublisherAsync<T>(string topic) where T : IMessage
        {
            if (!IsValidResourceName(topic))
            {
                throw new ArgumentException($"'{topic}' is not a valid resource name");
            }

            TopicInfo<T> topicInfo = new TopicInfo<T>(CallerId, topic);
            RosPublisher<T> publisher = new RosPublisher<T>(this, topicInfo, (int) TcpRosTimeout.TotalMilliseconds);

            publishersByTopic[topic] = publisher;

            RegisterPublisherResponse? response;
            try
            {
                response = await Master.RegisterPublisherAsync(topic, topicInfo.Type).Caf();
            }
            catch (Exception e)
            {
                publishersByTopic.TryRemove(topic, out _);
                throw new RosRpcException("Error registering publisher", e);
            }

            if (response.IsValid)
            {
                return publisher;
            }

            publishersByTopic.TryRemove(topic, out _);
            throw new RosRpcException($"Error registering publisher: {response.StatusMessage}");
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
        public string Advertise<T>(string topic, out RosPublisher<T> publisher) where T : IMessage
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (!TryGetPublisher(topic, out IRosPublisher basePublisher))
            {
                publisher = CreatePublisher<T>(topic);
            }
            else
            {
                var newPublisher = basePublisher as RosPublisher<T>;
                publisher = newPublisher ?? throw new InvalidMessageTypeException(
                    $"There is already an advertiser with a different type [{basePublisher.TopicType}]");
            }

            return publisher.Advertise();
        }

        string IRosClient.Advertise<T>(string topic, out IRosPublisher<T> publisher)
        {
            string id = Advertise<T>(topic, out var newPublisher);
            publisher = newPublisher;
            return id;
        }

        /// <summary>
        /// Advertises the given topic.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="topic">Name of the topic.</param>
        /// <returns>A pair containing a token that can be used to unadvertise from this publisher, and the publisher object.</returns>
        public async Task<(string id, RosPublisher<T> publisher)> AdvertiseAsync<T>(string topic) where T : IMessage
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            RosPublisher<T> publisher;
            if (!TryGetPublisher(topic, out IRosPublisher basePublisher))
            {
                publisher = (RosPublisher<T>) await CreatePublisherAsync<T>(topic).Caf();
            }
            else
            {
                var newPublisher = basePublisher as RosPublisher<T>;
                publisher = newPublisher ?? throw new InvalidMessageTypeException(
                    $"Existing subscriber message type {basePublisher.TopicType} does not match the given type.");
            }

            return (publisher.Advertise(), publisher);
        }

        async Task<(string id, IRosPublisher<T> publisher)> IRosClient.AdvertiseAsync<T>(string topic)
        {
            return await AdvertiseAsync<T>(topic);
        }

        string IRosClient.Advertise(string topic, Type msgType, out IRosPublisher publisher)
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (msgType is null)
            {
                throw new ArgumentNullException(nameof(msgType));
            }

            if (!TryGetPublisher(topic, out IRosPublisher basePublisher))
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo? baseMethod = GetType().GetMethod(nameof(CreatePublisher), flags);
                MethodInfo? method = baseMethod?.MakeGenericMethod(msgType);
                if (method == null)
                {
                    throw new InvalidMessageTypeException($"Type {msgType} is not a message object");
                }

                object? publisherObj = method.Invoke(this, flags, null, new object[] {topic}, BuiltIns.Culture);
                publisher = (IRosPublisher?) publisherObj ??
                            throw new InvalidMessageTypeException("Failed to call 'CreatePublisher'!");
            }
            else
            {
                if (!basePublisher.MessageTypeMatches(msgType))
                {
                    throw new InvalidMessageTypeException(
                        $"Type {msgType} does not match existing publisher type {basePublisher.TopicType}.");
                }

                publisher = basePublisher;
            }

            return publisher.Advertise();
        }

        async Task<(string id, IRosPublisher publisher)> IRosClient.AdvertiseAsync(string topic, Type msgType)
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (msgType is null)
            {
                throw new ArgumentNullException(nameof(msgType));
            }

            IRosPublisher publisher;
            if (!TryGetPublisher(topic, out IRosPublisher basePublisher))
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo? baseMethod = GetType().GetMethod(nameof(CreatePublisherAsync), flags);
                MethodInfo? method = baseMethod?.MakeGenericMethod(msgType);
                if (method == null)
                {
                    throw new InvalidMessageTypeException($"Type {msgType} is not a message object");
                }

                object? result = method.Invoke(this, flags, null, new object[] {topic}, BuiltIns.Culture);
                if (result == null)
                {
                    throw new InvalidMessageTypeException("Failed to call 'CreatePublisherAsync'!");
                }

                Task<IRosPublisher> task = (Task<IRosPublisher>) result;
                publisher = await task;
            }
            else
            {
                if (!basePublisher.MessageTypeMatches(msgType))
                {
                    throw new InvalidMessageTypeException(
                        $"Type {msgType} does not match existing publisher type {basePublisher.TopicType}.");
                }

                publisher = basePublisher;
            }

            return (publisher.Advertise(), publisher);
        }


        /// <summary>
        /// Unadvertise the given topic.
        /// </summary>
        /// <param name="topicId">Token returned by Advertise().</param>
        /// <returns>Whether the unadvertisement succeeded.</returns>
        public bool Unadvertise(string topicId)
        {
            if (topicId is null)
            {
                throw new ArgumentNullException(nameof(topicId));
            }

            IRosPublisher? publisher = publishersByTopic.Values.FirstOrDefault(p => p.ContainsId(topicId));

            return publisher != null && publisher.Unadvertise(topicId);
        }

        /// <summary>
        /// Unadvertise the given topic.
        /// </summary>
        /// <param name="topicId">Token returned by Advertise().</param>
        /// <returns>Whether the unadvertisement succeeded.</returns>
        public async Task<bool> UnadvertiseAsync(string topicId)
        {
            if (topicId is null)
            {
                throw new ArgumentNullException(nameof(topicId));
            }

            IRosPublisher? publisher = publishersByTopic.Values.FirstOrDefault(p => p.ContainsId(topicId));

            return publisher != null && await publisher.UnadvertiseAsync(topicId).Caf();
        }

        internal void RemovePublisher(IRosPublisher publisher)
        {
            publishersByTopic.TryRemove(publisher.Topic, out _);
            Master.UnregisterPublisher(publisher.Topic);
        }

        internal async Task RemovePublisherAsync(IRosPublisher publisher)
        {
            publishersByTopic.TryRemove(publisher.Topic, out _);
            await Master.UnregisterPublisherAsync(publisher.Topic).Caf();
        }

        /// <summary>
        /// Tries to retrieve the publisher of the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="publisher">Publisher of the given topic.</param>
        /// <returns>Whether the publisher was found.</returns>
        public bool TryGetPublisher(string topic, out IRosPublisher publisher)
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return publishersByTopic.TryGetValue(topic, out publisher!);
        }

        /// <summary>
        /// Retrieves the publisher of the given topic.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <returns></returns>
        public IRosPublisher GetPublisher(string topic)
        {
            if (TryGetPublisher(topic, out IRosPublisher publisher))
            {
                return publisher;
            }

            throw new KeyNotFoundException($"Cannot find publisher for topic '{topic}'");
        }

        /// <summary>
        /// Asks the master for all the published nodes in the system.
        /// Corresponds to the function 'getPublishedTopics' in the ROS Master API.
        /// </summary>
        /// <returns>List of topic names and message types.</returns>
        public ReadOnlyCollection<BriefTopicInfo> GetSystemPublishedTopics()
        {
            var response = Master.GetPublishedTopics();
            if (response.IsValid)
            {
                return response.Topics
                    .Select(tuple => new BriefTopicInfo(tuple.name, tuple.type))
                    .ToArray().AsReadOnly();
            }

            throw new RosRpcException($"Failed to retrieve topics: {response.StatusMessage}");
        }

        /// <summary>
        /// Asks the master for all the published nodes in the system.
        /// Corresponds to the function 'getPublishedTopics' in the ROS Master API.
        /// </summary>
        /// <returns>List of topic names and message types.</returns>
        public async Task<ReadOnlyCollection<BriefTopicInfo>> GetSystemPublishedTopicsAsync()
        {
            var response = await Master.GetPublishedTopicsAsync().Caf();
            if (response.IsValid)
            {
                return response.Topics
                    .Select(tuple => new BriefTopicInfo(tuple.name, tuple.type))
                    .ToArray().AsReadOnly();
            }

            throw new RosRpcException($"Failed to retrieve topics: {response.StatusMessage}");
        }

        /// <summary>
        /// Gets the topics published by this node.
        /// </summary>
        public ReadOnlyCollection<BriefTopicInfo> SubscribedTopics => GetSubscriptionsRcp().AsReadOnly();

        /// <summary>
        /// Asks the master for the nodes and topics in the system.
        /// Corresponds to the function 'getSystemState' in the ROS Master API.
        /// </summary>
        /// <returns>List of advertised topics, subscribed topics, and offered services, together with the involved nodes.</returns>
        public SystemState GetSystemState()
        {
            var response = Master.GetSystemState();
            if (response.IsValid)
            {
                return new SystemState(response.Publishers, response.Subscribers, response.Services);
            }

            throw new RosRpcException($"Failed to retrieve system state: {response.StatusMessage}");
        }

        /// <summary>
        /// Asks the master for the nodes and topics in the system.
        /// Corresponds to the function 'getSystemState' in the ROS Master API.
        /// </summary>
        /// <returns>List of advertised topics, subscribed topics, and offered services, together with the involved nodes.</returns>
        public async Task<SystemState> GetSystemStateAsync()
        {
            var response = await Master.GetSystemStateAsync().Caf();
            if (response.IsValid)
            {
                return new SystemState(response.Publishers, response.Subscribers, response.Services);
            }

            throw new RosRpcException($"Failed to retrieve system state: {response.StatusMessage}");
        }

        /// <summary>
        /// Gets the topics published by this node.
        /// </summary>
        public ReadOnlyCollection<BriefTopicInfo> PublishedTopics => GetPublicationsRcp().AsReadOnly();

        internal BriefTopicInfo[] GetSubscriptionsRcp()
        {
            return subscribersByTopic.Values
                .Select(subscriber => new BriefTopicInfo(subscriber.Topic, subscriber.TopicType))
                .ToArray();
        }

        internal BriefTopicInfo[] GetPublicationsRcp()
        {
            return publishersByTopic.Values
                .Select(publisher => new BriefTopicInfo(publisher.Topic, publisher.TopicType))
                .ToArray();
        }

        internal async Task PublisherUpdateRcpAsync(string topic, IEnumerable<Uri> publishers)
        {
            if (!TryGetSubscriber(topic, out IRosSubscriber subscriber))
            {
                Logger.LogFormat("{0}: PublisherUpdate called for nonexisting topic '{1}'", this, topic);
                return;
            }

            try
            {
                await subscriber.PublisherUpdateRcpAsync(publishers).Caf();
            }
            catch (Exception e)
            {
                Logger.LogFormat("{0}: PublisherUpdateRcp failed: {1}", this, e);
            }
        }

        internal Endpoint? RequestTopicRpc(string remoteCallerId, string topic)
        {
            if (!TryGetPublisher(topic, out IRosPublisher publisher))
            {
                Logger.LogFormat("{0}: '{1} is requesting topic '{2}' but we don't publish it", this,
                    remoteCallerId, topic);
                return null;
            }

            try
            {
                return publisher.RequestTopicRpc(remoteCallerId);
            }
            catch (Exception e)
            {
                Logger.LogFormat("{0}: RequestTopicRpc failed: {1}", this, e);
                return null;
            }
        }

        /// <summary>
        /// Close this connection. Unsubscribes and unadvertises all topics.
        /// </summary>
        public void Close()
        {
            listener.Dispose();

            IRosPublisher[] publishers = publishersByTopic.Values.ToArray();
            publishersByTopic.Clear();

            foreach (IRosPublisher publisher in publishers)
            {
                try
                {
                    publisher.Dispose();
                    Master.UnregisterPublisher(publisher.Topic);
                }
                catch (Exception e)
                {
                    Logger.LogDebugFormat("Error unregistering publisher {0}: {1}", publisher, e);
                }
            }

            IRosSubscriber[] subscribers = subscribersByTopic.Values.ToArray();
            subscribersByTopic.Clear();

            foreach (IRosSubscriber subscriber in subscribers)
            {
                try
                {
                    subscriber.Dispose();
                    Master.UnregisterSubscriber(subscriber.Topic);
                }
                catch (Exception e)
                {
                    Logger.LogDebugFormat("Error unregistering subscriber {0}: {1}", subscriber, e);
                }
            }

            IServiceCaller[] receivers = subscribedServicesByName.Values.ToArray();
            subscribedServicesByName.Clear();

            foreach (IServiceCaller receiver in receivers)
            {
                receiver.Dispose();
            }

            IServiceRequestManager[] serviceManagers = advertisedServicesByName.Values.ToArray();
            advertisedServicesByName.Clear();

            foreach (IServiceRequestManager serviceSender in serviceManagers)
            {
                try
                {
                    serviceSender.Dispose();
                    Master.UnregisterService(serviceSender.Service, serviceSender.Uri);
                }
                catch (Exception e)
                {
                    Logger.LogDebugFormat("Error unregistering subscriber {0}: {1}", serviceSender, e);
                }
            }
        }

        /// <summary>
        /// Close this connection. Unsubscribes and unadvertises all topics.
        /// </summary>
        public async Task CloseAsync()
        {
            listener.Dispose();

            var publishers = publishersByTopic.Values.ToArray();
            publishersByTopic.Clear();

            List<Task> tasks = new List<Task>();
            tasks.AddRange(publishers.Select(async publisher =>
            {
                publisher.Dispose();
                await Master.UnregisterPublisherAsync(publisher.Topic).Caf();
            }));

            var subscribers = subscribersByTopic.Values.ToArray();
            subscribersByTopic.Clear();

            tasks.AddRange(subscribers.Select(async subscriber =>
            {
                subscriber.Dispose();
                await Master.UnregisterSubscriberAsync(subscriber.Topic).Caf();
            }));

            IServiceCaller[] receivers = subscribedServicesByName.Values.ToArray();
            subscribedServicesByName.Clear();

            foreach (IServiceCaller receiver in receivers)
            {
                receiver.Dispose();
            }

            IServiceRequestManager[] serviceManagers = advertisedServicesByName.Values.ToArray();
            advertisedServicesByName.Clear();

            tasks.AddRange(serviceManagers.Select(async senderManager =>
            {
                await senderManager.DisposeAsync().Caf();
                await Master.UnregisterServiceAsync(senderManager.Service, senderManager.Uri).Caf();
            }));

            await Task.WhenAll(tasks).Caf();
        }

        public SubscriberState GetSubscriberStatistics()
        {
            return new SubscriberState(subscribersByTopic.Values.Select(subscriber => subscriber.GetState()).ToArray());
        }

        public PublisherState GetPublisherStatistics()
        {
            return new PublisherState(publishersByTopic.Values.Select(publisher => publisher.GetState()).ToArray());
        }

        internal IEnumerable<BusInfo> GetBusInfoRcp()
        {
            List<BusInfo> busInfos = new List<BusInfo>();

            SubscriberState state = GetSubscriberStatistics();
            foreach (var topic in state.Topics)
            {
                foreach (var receiver in topic.Receivers)
                {
                    busInfos.Add(new BusInfo(busInfos.Count, topic.Topic, receiver));
                }
            }

            try
            {
                PublisherState pstate = GetPublisherStatistics();
                foreach (var topic in pstate.Topics)
                {
                    foreach (var sender in topic.Senders)
                    {
                        LookupNodeResponse response;
                        try
                        {
                            response = Master.LookupNode(sender.RemoteId);
                        }
                        catch (Exception e)
                        {
                            Logger.LogFormat("{0}: LookupNode for {1} failed: {2}", this, sender.RemoteId, e);
                            continue;
                        }

                        if (!response.IsValid)
                        {
                            continue;
                        }

                        BusInfo busInfo = new BusInfo(busInfos.Count, response.Uri!, "o", topic.Topic, sender.IsAlive);
                        busInfos.Add(busInfo);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"{this}: GetBusInfoRcp failed: {e}");
            }

            return busInfos;
        }

        /// <summary>
        /// Calls the given ROS service.
        /// </summary>
        /// <param name="serviceName">Name of the ROS service</param>
        /// <param name="service">Service message. The response will be written in the response field.</param>
        /// <param name="persistent">Whether a persistent connection with the provider should be maintained.</param>
        /// <param name="timeoutInMs">Maximal time to wait.</param>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>Whether the call succeeded.</returns>
        public bool CallService<T>(string serviceName, T service, bool persistent = false, int timeoutInMs = 5000)
            where T : IService
        {
            CancellationTokenSource timeoutTs = new CancellationTokenSource(timeoutInMs);

            if (subscribedServicesByName.TryGetValue(serviceName, out var baseExistingReceiver))
            {
                if (!(baseExistingReceiver is ServiceCallerAsync<T> existingReceiver))
                {
                    throw new InvalidMessageTypeException(
                        $"Existing connection with service type {baseExistingReceiver.ServiceType} does not match the given type.");
                }

                // is there a persistent connection? use it
                if (existingReceiver.IsAlive)
                {
                    return existingReceiver.Execute(service, timeoutTs.Token);
                }

                existingReceiver.Dispose();
                subscribedServicesByName.TryRemove(serviceName, out _);
            }

            // otherwise, create a new transient one
            LookupServiceResponse response = Master.LookupService(serviceName);
            if (!response.IsValid)
            {
                throw new RosRpcException($"Failed to call service: {response.StatusMessage}");
            }

            Uri serviceUri = response.ServiceUrl!;
            ServiceInfo<T> serviceInfo = new ServiceInfo<T>(CallerId, serviceName);
            try
            {
                if (persistent)
                {
                    var serviceCaller = new ServiceCallerAsync<T>(serviceInfo, serviceUri, true);
                    serviceCaller.Start(persistent);
                    subscribedServicesByName.TryAdd(serviceName, serviceCaller);
                    return serviceCaller.Execute(service, timeoutTs.Token);
                }
                else
                {
                    using var serviceCaller = new ServiceCallerAsync<T>(serviceInfo, serviceUri, true);
                    serviceCaller.Start(persistent);
                    return serviceCaller.Execute(service, timeoutTs.Token);
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Service call to uri '{serviceUri}' timed out");
            }
            catch (Exception e)
            {
                throw new RoslibException($"Service uri '{serviceUri}' is not reachable", e);
            }
        }

        /// <summary>
        /// Calls the given ROS service.
        /// </summary>
        /// <param name="serviceName">Name of the ROS service</param>
        /// <param name="service">Service message. The response will be written in the response field.</param>
        /// <param name="persistent">Whether a persistent connection with the provider should be maintained.</param>
        /// <param name="timeoutInMs">Maximal time to wait.</param>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>Whether the call succeeded.</returns>
        public async Task<bool> CallServiceAsync<T>(string serviceName, T service, bool persistent = false,
            int timeoutInMs = 5000)
            where T : IService
        {
            CancellationTokenSource timeoutTs = new CancellationTokenSource(timeoutInMs);

            if (subscribedServicesByName.TryGetValue(serviceName, out var baseExistingReceiver))
            {
                if (!(baseExistingReceiver is ServiceCallerAsync<T> existingReceiver))
                {
                    throw new InvalidMessageTypeException(
                        $"Existing connection with service type {baseExistingReceiver.ServiceType} does not match the given type.");
                }

                // is there a persistent connection? use it
                if (existingReceiver.IsAlive)
                {
                    return await existingReceiver.ExecuteAsync(service, timeoutTs.Token);
                }

                existingReceiver.Dispose();
                subscribedServicesByName.TryRemove(serviceName, out _);
            }


            LookupServiceResponse response = await Master.LookupServiceAsync(serviceName).Caf();
            if (!response.IsValid)
            {
                throw new RosRpcException($"Failed to call service: {response.StatusMessage}");
            }

            Uri serviceUri = response.ServiceUrl!;
            ServiceInfo<T> serviceInfo = new ServiceInfo<T>(CallerId, serviceName);
            try
            {
                if (persistent)
                {
                    var serviceCaller = new ServiceCallerAsync<T>(serviceInfo, serviceUri, true);
                    await serviceCaller.StartAsync(persistent).Caf();
                    subscribedServicesByName.TryAdd(serviceName, serviceCaller);
                    return await serviceCaller.ExecuteAsync(service, timeoutTs.Token).Caf();
                }
                else
                {
                    using var serviceCaller = new ServiceCallerAsync<T>(serviceInfo, serviceUri, true);
                    await serviceCaller.StartAsync(persistent).Caf();
                    return await serviceCaller.ExecuteAsync(service, timeoutTs.Token).Caf();
                }
            }
            catch (Exception e)
            {
                throw new RoslibException($"Service uri '{serviceUri}' is not reachable", e);
            }
        }

        bool ServiceAlreadyExists<T>(string serviceName) where T : IService
        {
            if (!advertisedServicesByName.TryGetValue(serviceName, out var existingSender))
            {
                return false;
            }

            if (!(existingSender is ServiceRequestManager<T>))
            {
                throw new InvalidMessageTypeException(
                    $"Existing advertised service type {existingSender.ServiceType} does not match the given type.");
            }

            return true;
        }

        /// <summary>
        /// Advertises the given service.
        /// </summary>
        /// <param name="serviceName">Name of the ROS service.</param>
        /// <param name="callback">Function to be called when a service request arrives. The response should be written in the response field.</param>
        /// <typeparam name="T">Service type.</typeparam>
        public bool AdvertiseService<T>(string serviceName, Action<T> callback) where T : IService, new()
        {
            if (ServiceAlreadyExists<T>(serviceName))
            {
                return false;
            }

            async Task Wrapper(T x)
            {
                callback(x);
                await Task.CompletedTask;
            }

            ServiceInfo<T> serviceInfo = new ServiceInfo<T>(CallerId, serviceName, new T());
            var advertisedService = new ServiceRequestManager<T>(serviceInfo, CallerUri.Host, Wrapper);

            advertisedServicesByName.TryAdd(serviceName, advertisedService);

            Master.RegisterService(serviceName, advertisedService.Uri);
            return true;
        }

        /// <summary>
        /// Advertises the given service.
        /// </summary>
        /// <param name="serviceName">Name of the ROS service.</param>
        /// <param name="callback">Function to be called when a service request arrives. The response should be written in the response field.</param>
        /// <typeparam name="T">Service type.</typeparam>
        public async Task<bool> AdvertiseServiceAsync<T>(string serviceName, Func<T, Task> callback)
            where T : IService, new()
        {
            if (ServiceAlreadyExists<T>(serviceName))
            {
                return false;
            }

            ServiceInfo<T> serviceInfo = new ServiceInfo<T>(CallerId, serviceName, new T());
            var advertisedService = new ServiceRequestManager<T>(serviceInfo, CallerUri.Host, callback);

            advertisedServicesByName.TryAdd(serviceName, advertisedService);

            await Master.RegisterServiceAsync(serviceName, advertisedService.Uri).Caf();
            return true;
        }

        /// <summary>
        /// Unadvertises the service.
        /// </summary>
        /// <param name="name">Name of the service</param>
        /// <exception cref="ArgumentException">Thrown if name is null</exception>
        public void UnadvertiseService(string name)
        {
            if (!advertisedServicesByName.TryGetValue(name, out var advertisedService))
            {
                throw new ArgumentException("Service does not exist", nameof(name));
            }

            advertisedServicesByName.TryRemove(name, out _);

            advertisedService.Dispose();
            Master.UnregisterService(name, advertisedService.Uri);
        }

        /// <summary>
        /// Unadvertises the service.
        /// </summary>
        /// <param name="name">Name of the service</param>
        /// <exception cref="ArgumentException">Thrown if name is null</exception>        
        public async Task UnadvertiseServiceAsync(string name)
        {
            if (!advertisedServicesByName.TryGetValue(name, out var advertisedService))
            {
                throw new ArgumentException("Service does not exist", nameof(name));
            }

            advertisedServicesByName.TryRemove(name, out _);

            await advertisedService.DisposeAsync();
            await Master.UnregisterServiceAsync(name, advertisedService.Uri);
        }

        public void Dispose()
        {
            Close();
        }

#if !NETSTANDARD2_0
        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
        }
#endif

        public override string ToString()
        {
            return $"[RosClient MyUri='{CallerUri}' MyId='{CallerId}' MasterUri='{MasterUri}']";
        }
    }
}