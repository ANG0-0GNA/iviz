﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.Roslib.XmlRpc;
using Iviz.XmlRpc;
using Buffer = Iviz.Msgs.Buffer;

namespace Iviz.Roslib
{
    internal sealed class TcpReceiverManager<T> where T : IMessage
    {
        const int DefaultTimeoutInMs = 5000;

        readonly AsyncLock mutex = new AsyncLock();
        readonly Dictionary<Uri, TcpReceiverAsync<T>> connectionsByUri = new Dictionary<Uri, TcpReceiverAsync<T>>();
        readonly RosClient client;
        readonly TopicInfo<T> topicInfo;

        public TcpReceiverManager(RosClient client, TopicInfo<T> topicInfo, bool requestNoDelay)
        {
            this.client = client;
            this.topicInfo = topicInfo;
            RequestNoDelay = requestNoDelay;
        }

        public string Topic => topicInfo.Topic;
        public string TopicType => topicInfo.Type;

        public int NumConnections
        {
            get
            {
                TryToCleanup();
                return connectionsByUri.Count;
            }
        }

        void TryToCleanup()
        {
            AsyncLock.InnerLock? @lock = mutex.TryLock();
            if (@lock == null)
            {
                return;
            }

            bool numConnectionsChanged;
            using (@lock.Value)
            {
                numConnectionsChanged = Cleanup();
            }

            if (numConnectionsChanged)
            {
                Subscriber.RaiseNumPublishersChanged();
            }
        }

        public bool RequestNoDelay { get; }
        public int TimeoutInMs { get; set; } = DefaultTimeoutInMs;
        public RosSubscriber<T> Subscriber { private get; set; }

        internal async Task<Endpoint> RequestConnectionFromPublisherAsync(Uri remoteUri)
        {
            NodeClient.RequestTopicResponse response;
            try
            {
                response = await client.CreateTalker(remoteUri).RequestTopicAsync(Topic).Caf();
            }
            catch (Exception e) when (e is TimeoutException || e is AggregateException || e is XmlRpcException)
            {
                Logger.LogDebug($"{this}: Connection request to publisher {remoteUri} failed: {e}");
                return null;
            }
            catch (Exception e)
            {
                Logger.LogError($"{this}: Connection request to publisher {remoteUri} failed: {e}");
                return null;
            }

            if (!response.IsValid || response.Protocol.Type == null)
            {
                Logger.LogDebug(
                    $"{this}: Connection request to publisher {remoteUri} has failed: {response.StatusMessage}");
                return null;
            }

            if (response.Protocol.Port == 0)
            {
                Logger.LogDebug(
                    $"{this}: Connection request to publisher {remoteUri} returned an uninitialized address!");
                return null;
            }

            return new Endpoint(response.Protocol.Hostname, response.Protocol.Port);
        }

        internal void MessageCallback(in T msg)
        {
            Subscriber.MessageCallback(msg);
        }

        async Task<bool> AddPublisherAsync(Uri remoteUri)
        {
            Endpoint remoteEndpoint = await RequestConnectionFromPublisherAsync(remoteUri).Caf();
            if (remoteEndpoint == null)
            {
                return false;
            }

            CreateConnection(remoteEndpoint, remoteUri);
            return true;
        }

        void CreateConnection(Endpoint remoteEndpoint, Uri remoteUri)
        {
            TcpReceiverAsync<T> connection =
                new TcpReceiverAsync<T>(this, remoteUri, remoteEndpoint, topicInfo, RequestNoDelay);
            connectionsByUri[remoteUri] = connection;
            connection.Start(TimeoutInMs);
        }

        public async Task PublisherUpdateRpcAsync(IEnumerable<Uri> publisherUris)
        {
            bool numConnectionsChanged;
            using (await mutex.LockAsync())
            {
                HashSet<Uri> newPublishers = new HashSet<Uri>(publisherUris);
                IEnumerable<Uri> toAdd = newPublishers.Where(uri => uri != null && !connectionsByUri.ContainsKey(uri));

                // if an uri is not registered as a publisher anymore,
                // we kill existing receivers only if they are still trying to reconnect
                // existing sessions should continue
                TcpReceiverAsync<T>[] toDelete = connectionsByUri
                    .Where(pair => !newPublishers.Contains(pair.Key) /*&& !pair.Value.IsConnected*/)
                    .Select(pair => pair.Value).ToArray();

                //Logger.Log(this + " old: " + string.Join(",", connectionsByUri.Keys) + " new: " +
                //           string.Join(",", newPublishers) + " todie: " + string.Join(",", toDelete));

                foreach (TcpReceiverAsync<T> receiver in toDelete)
                {
                    //Logger.Log(this + " disposing: " + receiver);
                    receiver.Dispose();
                }

                bool[] results = await Task.WhenAll(toAdd.Select(AddPublisherAsync)).Caf();
                numConnectionsChanged = results.Any(b => b) | Cleanup();
            }

            if (numConnectionsChanged)
            {
                Subscriber.RaiseNumPublishersChanged();
            }
        }
        

        bool Cleanup()
        {
            TcpReceiverAsync<T>[] toDelete = connectionsByUri.Values.Where(receiver => !receiver.IsAlive).ToArray();
            foreach (TcpReceiverAsync<T> receiver in toDelete)
            {
                connectionsByUri.Remove(receiver.RemoteUri);
                Logger.Log($"{this}: Removing connection with '{receiver.RemoteUri}' - dead x_x");
                receiver.Dispose();
            }

            return toDelete.Length != 0;
        }

        public void Stop()
        {
            using (mutex.Lock())
            {
                foreach (TcpReceiverAsync<T> receiver in connectionsByUri.Values)
                {
                    receiver.Dispose();
                }

                connectionsByUri.Clear();
                Subscriber.RaiseNumPublishersChanged();
            }
        }

        public ReadOnlyCollection<SubscriberReceiverState> GetStates()
        {
            return connectionsByUri.Values.Select(receiver => receiver.State).ToList().AsReadOnly();
        }

        public override string ToString()
        {
            return $"[TcpReceiverManager '{Topic}']";
        }
    }
}