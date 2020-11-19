﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.Msgs.RosgraphMsgs;
using Iviz.Roslib.XmlRpc;
using Iviz.XmlRpc;
using Nito.AsyncEx;

namespace Iviz.Roslib
{
    internal sealed class TcpReceiverManager<T> where T : IMessage
    {
        const int DefaultTimeoutInMs = 5000;

        readonly AsyncLock mutex = new AsyncLock();

        readonly ConcurrentDictionary<Uri, TcpReceiverAsync<T>> connectionsByUri =
            new ConcurrentDictionary<Uri, TcpReceiverAsync<T>>();

        readonly RosClient client;
        readonly RosSubscriber<T> subscriber;
        readonly TopicInfo<T> topicInfo;

        public TcpReceiverManager(RosSubscriber<T> subscriber, RosClient client, TopicInfo<T> topicInfo,
            bool requestNoDelay)
        {
            this.subscriber = subscriber;
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
            IDisposable @lock;
            using (CancellationTokenSource tokenSource = new CancellationTokenSource(100))
            {
                try
                {
                    @lock = mutex.Lock(tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            bool numConnectionsChanged;
            using (@lock)
            {
                numConnectionsChanged = Cleanup();
            }

            if (numConnectionsChanged)
            {
                subscriber.RaiseNumPublishersChanged();
            }
        }

        public bool RequestNoDelay { get; }
        public int TimeoutInMs { get; set; } = DefaultTimeoutInMs;

        internal async Task<Endpoint?> RequestConnectionFromPublisherAsync(Uri remoteUri)
        {
            NodeClient.RequestTopicResponse response;
            try
            {
                response = await client.CreateTalker(remoteUri).RequestTopicAsync(Topic).Caf();
            }
            catch (Exception e) when (
                e is TimeoutException || 
                e is XmlRpcException || 
                e is SocketException ||
                e is IOException)
            {
                Logger.LogDebugFormat("{0}: Connection request to publisher {1} failed: {2}",
                    this, remoteUri, e);
                return null;
            }
            catch (Exception e)
            {
                Logger.LogErrorFormat("{0}: Connection request to publisher {1} failed: {2}",
                    this, remoteUri, e);
                return null;
            }

            if (!response.IsValid || response.Protocol == null)
            {
                Logger.LogDebugFormat("{0}: Connection request to publisher {1} failed: {2}",
                    this, remoteUri, response.StatusMessage);
                return null;
            }

            if (response.Protocol.Port == 0)
            {
                Logger.LogDebugFormat("{0}: Connection request to publisher {1} returned an uninitialized address!",
                    this, remoteUri);
                return null;
            }

            return new Endpoint(response.Protocol.Hostname, response.Protocol.Port);
        }

        internal void MessageCallback(in T msg)
        {
            subscriber.MessageCallback(msg);
        }

        async Task<bool> AddPublisherAsync(Uri remoteUri)
        {
            try
            {
                Endpoint? remoteEndpoint = await RequestConnectionFromPublisherAsync(remoteUri).Caf();
                if (remoteEndpoint == null)
                {
                    return false;
                }

                CreateConnection(remoteEndpoint, remoteUri);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogErrorFormat("{0}: Connection request to publisher {1} returned an unexpected exception: {2}",
                    this, remoteUri, e);
                return false;
            }
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
                //           string.Join(",", newPublishers) + " toDie: " + string.Join<TcpReceiverAsync<T>>(",", toDelete));

                foreach (TcpReceiverAsync<T> receiver in toDelete)
                {
                    //Logger.Log(this + " disposing: " + receiver);
                    receiver.Dispose();
                }

                // these will run concurrently!
                bool[] results = await Task.WhenAll(toAdd.Select(AddPublisherAsync)).Caf();
                numConnectionsChanged = results.Any(b => b) | Cleanup();
            }

            if (numConnectionsChanged)
            {
                subscriber.RaiseNumPublishersChanged();
            }
        }


        bool Cleanup()
        {
            TcpReceiverAsync<T>[] toDelete = connectionsByUri.Values.Where(receiver => !receiver.IsAlive).ToArray();
            foreach (TcpReceiverAsync<T> receiver in toDelete)
            {
                connectionsByUri.TryRemove(receiver.RemoteUri, out _);
                Logger.LogFormat("{0}: Removing connection with '{1}' - dead x_x", this, receiver.RemoteUri);
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
                subscriber.RaiseNumPublishersChanged();
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