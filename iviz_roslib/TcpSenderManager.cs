﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using Iviz.Msgs;

namespace Iviz.Roslib
{
    internal class TcpSenderManager
    {
        readonly TopicInfo topicInfo;
        readonly Dictionary<string, TcpSender> connectionsByCallerId = new Dictionary<string, TcpSender>();

        public Uri CallerUri { get; }
        public string Topic => topicInfo.Topic;
        public string CallerId => topicInfo.CallerId;
        public string TopicType => topicInfo.Type;
        public int NumConnections => connectionsByCallerId.Count;

        public int TimeoutInMs { get; set; } = 5000;

        public IMessage LatchedMessage { get; private set; }

        bool latching;
        public bool Latching
        {
            get => latching;
            set
            {
                latching = value;
                if (!value)
                {
                    LatchedMessage = null;
                }
            }
        }

        int maxQueueSizeInBytes;
        public int MaxQueueSizeInBytes
        {
            get => maxQueueSizeInBytes;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException($"Cannot set max queue size to {value}");
                }

                maxQueueSizeInBytes = value;
                lock (connectionsByCallerId)
                {
                    connectionsByCallerId.Values.ForEach(x => x.MaxQueueSizeInBytes = value);
                }
            }
        }

        public TcpSenderManager(TopicInfo topicInfo, Uri callerUri)
        {
            this.topicInfo = topicInfo;
            CallerUri = callerUri;
        }

        public IPEndPoint CreateConnection(string remoteCallerId)
        {
            Logger.LogDebug($"{this}: '{remoteCallerId}' is requesting {Topic}");
            TcpSender newSender = new TcpSender(CallerUri, remoteCallerId, topicInfo, Latching);

            IPEndPoint endPoint;
            lock (connectionsByCallerId)
            {
                if (connectionsByCallerId.TryGetValue(remoteCallerId, out TcpSender oldSender) && 
                    oldSender.IsAlive)
                {
                    Logger.LogDebug($"{this}: '{remoteCallerId} is requesting {Topic} again?");
                    oldSender.Stop();
                }

                endPoint = newSender.Start(TimeoutInMs);
                connectionsByCallerId[remoteCallerId] = newSender;
            }

            // while we're here
            Cleanup();

            // ugh
            for (int i = 0; i < 10 && newSender.Status == SenderStatus.Inactive; i++)
            {
                Thread.Sleep(10);
            }

            if (Latching && LatchedMessage != null)
            {
                newSender.Publish(LatchedMessage);
            }

            newSender.MaxQueueSizeInBytes = MaxQueueSizeInBytes;
            return endPoint;
        }

        public bool Cleanup()
        {
            bool subscribersChanged = false;
            lock (connectionsByCallerId)
            {
                string[] toDelete = connectionsByCallerId.Where(x => !x.Value.IsAlive).Select(x => x.Key).ToArray();
                foreach (string callerId in toDelete)
                {
                    Logger.LogDebug($"{this}: Removing connection with '{callerId}' - dead x_x");
                    connectionsByCallerId[callerId].Stop();
                    connectionsByCallerId.Remove(callerId);
                }

                subscribersChanged = toDelete.Length != 0;
            }

            return subscribersChanged;
        }

        public void Publish(IMessage msg)
        {
            if (Latching)
            {
                LatchedMessage = msg;
            }

            lock (connectionsByCallerId)
            {
                foreach (TcpSender connection in connectionsByCallerId.Values)
                {
                    connection.Publish(msg);
                }
            }
        }

        public void Stop()
        {
            lock (connectionsByCallerId)
            {
                foreach (TcpSender sender in connectionsByCallerId.Values)
                {
                    sender.Stop();
                }
                connectionsByCallerId.Clear();
            }
        }

        public ReadOnlyCollection<PublisherSenderState> GetStates()
        {
            lock (connectionsByCallerId)
            {
                return new ReadOnlyCollection<PublisherSenderState>(
                    connectionsByCallerId.Values.Select(x => x.State).ToArray()
                );
            }
        }

        public ReadOnlyDictionary<string, TcpSender> GetConnections()
        {
            lock (connectionsByCallerId)
            {
                return new ReadOnlyDictionary<string, TcpSender>(connectionsByCallerId);
            }
        }

        public override string ToString()
        {
            return $"[TcpSenderManager '{Topic}']";
        }
    }
}