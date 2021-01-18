﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Iviz.Roslib
{
    public sealed class SubscriberReceiverState
    {
        public bool IsAlive { get; }
        public bool IsConnected { get; }
        public bool RequestNoDelay { get; }
        public Endpoint? EndPoint { get; }
        public Uri RemoteUri { get; }
        public Endpoint? RemoteEndpoint { get; }
        public long NumReceived { get; }
        public long BytesReceived { get; }
        public string? ErrorDescription { get; }

        internal SubscriberReceiverState(bool isAlive, bool isConnected,
            bool requestNoDelay,
            Endpoint? endPoint,
            Uri remoteUri, Endpoint? remoteEndpoint,
            long numReceived, long bytesReceived,
            string? errorDescription)
        {
            IsAlive = isAlive;
            IsConnected = isConnected;
            RequestNoDelay = requestNoDelay;
            EndPoint = endPoint; 
            RemoteUri = remoteUri;
            RemoteEndpoint = remoteEndpoint;
            NumReceived = numReceived;
            BytesReceived = bytesReceived;
            ErrorDescription = errorDescription;
        }

        internal SubscriberReceiverState(Uri remoteUri)
        {
            IsAlive = false;
            IsConnected = false;
            RequestNoDelay = false;
            EndPoint = null; 
            RemoteUri = remoteUri;
            RemoteEndpoint = null;
            NumReceived = 0;
            BytesReceived = 0;
            ErrorDescription = null;            
        }
        

    }

    public sealed class SubscriberTopicState
    {
        public string Topic { get; }
        public string Type { get; }
        public ReadOnlyCollection<string> TopicIds { get; }
        public ReadOnlyCollection<SubscriberReceiverState> Receivers { get; }

        public SubscriberTopicState(string topic, string type, IList<string> topicIds, IList<SubscriberReceiverState> receivers)
        {
            Topic = topic;
            Type = type;
            TopicIds = topicIds.AsReadOnly();
            Receivers = receivers.AsReadOnly();
        }
    }

    public sealed class SubscriberState
    {
        public ReadOnlyCollection<SubscriberTopicState> Topics { get; }

        internal SubscriberState(IList<SubscriberTopicState> topics)
        {
            Topics = topics.AsReadOnly();
        }
    }
}
