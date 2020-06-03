﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using TopicTuple = System.Tuple<string, string>;
using TopicTuples = System.Tuple<string, string[]>;

namespace Iviz.RoslibSharp.XmlRpc
{
    public enum StatusCode
    {
        Error = -1,
        Failure = 0,
        Success = 1
    }

    public sealed class Master
    {
        public Uri MasterUri { get; }
        public Uri CallerUri { get; }
        public string CallerId { get; }
        public int TimeoutInMs { get; set; }

        internal Master(Uri masterUri, string callerId, Uri callerUri)
        {
            MasterUri = masterUri;
            CallerUri = callerUri;
            CallerId = callerId;
        }

        public GetUriResponse GetUri()
        {
            Arg[] args = { new Arg(CallerId) };
            object[] response = MethodCall("getUri", args);
            return new GetUriResponse(response);
        }

        public LookupNodeResponse LookupNode(string nodeId)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(nodeId),
            };
            object[] response = MethodCall("lookupNode", args);
            return new LookupNodeResponse(response);
        }

        public GetPublishedTopicsResponse GetPublishedTopics(string subgraph = "")
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(subgraph),
            };
            object[] response = MethodCall("getPublishedTopics", args);
            return new GetPublishedTopicsResponse(response);
        }

        public RegisterSubscriberResponse RegisterSubscriber(string topic, string topicType)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(topic),
                new Arg(topicType),
                new Arg(CallerUri.ToString()),
            };
            object[] response = MethodCall("registerSubscriber", args);
            return new RegisterSubscriberResponse(response);
        }

        public UnregisterSubscriberResponse UnregisterSubscriber(string topic)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(topic),
                new Arg(CallerUri.ToString()),
            };
            object[] response = MethodCall("unregisterSubscriber", args);
            return new UnregisterSubscriberResponse(response);
        }

        public RegisterPublisherResponse RegisterPublisher(string topic, string topicType)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(topic),
                new Arg(topicType),
                new Arg(CallerUri),
            };
            object[] response = MethodCall("registerPublisher", args);
            return new RegisterPublisherResponse(response);
        }

        public UnregisterPublisherResponse UnregisterPublisher(string topic)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(topic),
                new Arg(CallerUri),
            };
            object[] response = MethodCall("unregisterPublisher", args);
            return new UnregisterPublisherResponse(response);
        }

        public GetSystemStateResponse GetSystemState()
        {
            Arg[] args = {
                new Arg(CallerId),
            };
            object[] response = MethodCall("getSystemState", args);
            return new GetSystemStateResponse(response);
        }

        public LookupServiceResponse LookupService(string service)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(service),
            };
            object[] response = MethodCall("lookupService", args);
            return new LookupServiceResponse(response);
        }

        public DefaultResponse RegisterService(string service, Uri rosRpcUri)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(service),
                new Arg(rosRpcUri),
                new Arg(CallerUri),
            };
            object[] response = MethodCall("registerService", args);
            return new DefaultResponse(response);
        }

        public UnregisterServiceResponse UnregisterService(string service, Uri rosRpcUri)
        {
            Arg[] args = {
                new Arg(CallerId),
                new Arg(service),
                new Arg(rosRpcUri),
            };
            object[] response = MethodCall("unregisterService", args);
            return new UnregisterServiceResponse(response);
        }

        object[] MethodCall(string function, Arg[] args)
        {
            return (object[])Service.MethodCall(MasterUri, CallerUri, function, args, TimeoutInMs);

        }
    }

    public abstract class BaseResponse
    {
        public StatusCode Code { get; }
        public string StatusMessage { get; }

        protected private BaseResponse(object[] a)
        {
            Code = (StatusCode)a[0];
            StatusMessage = (string)a[1];
        }
    }

    public sealed class GetSystemStateResponse : BaseResponse
    {
        public ReadOnlyCollection<TopicTuples> Publishers { get; }
        public ReadOnlyCollection<TopicTuples> Subscribers { get; }
        public ReadOnlyCollection<TopicTuples> Services { get; }

        internal GetSystemStateResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                object[] root = (object[])a[2];
                Publishers = CreateTuple(root[0]);
                Subscribers = CreateTuple(root[1]);
                Services = CreateTuple(root[2]);
            }
            else
            {
                Logger.Log($"RcpMaster: GetSystemStateResponse failed: " + StatusMessage);
                Publishers = new ReadOnlyCollection<TopicTuples>(Array.Empty<TopicTuples>());
                Subscribers = new ReadOnlyCollection<TopicTuples>(Array.Empty<TopicTuples>());
                Services = new ReadOnlyCollection<TopicTuples>(Array.Empty<TopicTuples>());
            }
        }

        static ReadOnlyCollection<TopicTuples> CreateTuple(object root)
        {
            object[] list = (object[])root;
            TopicTuples[] result = new TopicTuples[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                object[] tuple = (object[])list[i];
                string topic = (string)tuple[0];
                string[] members = ((object[])tuple[1]).Cast<string>().ToArray();
                result[i] = Tuple.Create(topic, members);
            }
            return new ReadOnlyCollection<TopicTuples>(result);
        }
    }

    public sealed class DefaultResponse : BaseResponse
    {
        internal DefaultResponse(object[] a) : base(a) { }
    }

    public sealed class GetUriResponse : BaseResponse
    {
        public Uri Uri { get; }

        internal GetUriResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                if (Uri.TryCreate((string)a[2], UriKind.Absolute, out Uri uri))
                {
                    Uri = uri;
                }
                else
                {
                    Logger.Log($"RcpMaster: Failed to parse GetUriResponse uri: " + a[2]);
                    Uri = null;
                }
            }
            else
            {
                Logger.Log($"RcpMaster: GetUriResponse failed: " + StatusMessage);
                Uri = null;
            }
        }
    }

    public sealed class LookupNodeResponse : BaseResponse
    {
        public Uri Uri { get; }

        internal LookupNodeResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                if (Uri.TryCreate((string)a[2], UriKind.Absolute, out Uri uri))
                {
                    Uri = uri;
                }
                else
                {
                    Logger.Log($"RcpMaster: Failed to parse LookupNodeResponse uri: " + a[2]);
                    Uri = null;
                }
            }
            else
            {
                Logger.Log($"RcpMaster: LookupNodeResponse failed: " + StatusMessage);
                Uri = null;
            }
        }
    }

    public sealed class GetPublishedTopicsResponse : BaseResponse
    {
        public ReadOnlyCollection<TopicTuple> Topics { get; }

        internal GetPublishedTopicsResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                object[] tmp = (object[])a[2];

                TopicTuple[] topics = new TopicTuple[tmp.Length];
                for (int i = 0; i < topics.Length; i++)
                {
                    object[] topic = (object[])tmp[i];
                    topics[i] = Tuple.Create((string)topic[0], (string)topic[1]);
                }
                Topics = new ReadOnlyCollection<TopicTuple>(topics);
            }
            else
            {
                Logger.Log($"RcpMaster: GetPublishedTopicsResponse failed: " + StatusMessage);
                Topics = new ReadOnlyCollection<TopicTuple>(Array.Empty<TopicTuple>());
            }
        }
    }

    public sealed class RegisterSubscriberResponse : BaseResponse
    {
        public ReadOnlyCollection<Uri> Publishers { get; }

        internal RegisterSubscriberResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                object[] tmp = (object[])a[2];
                Uri[] publishers = new Uri[tmp.Length];
                for (int i = 0; i < publishers.Length; i++)
                {
                    if (!Uri.TryCreate((string)tmp[i], UriKind.Absolute, out publishers[i]))
                    {
                        Logger.Log($"RcpMaster: Invalid uri '{tmp[i]}'");
                    }
                }
                Publishers = new ReadOnlyCollection<Uri>(publishers);
            }
            else
            {
                Logger.Log($"RcpMaster: RegisterSubscriberResponse failed: " + StatusMessage);
                Publishers = new ReadOnlyCollection<Uri>(Array.Empty<Uri>());
            }
        }
    }

    public sealed class UnregisterSubscriberResponse : BaseResponse
    {
        public int NumUnsubscribed { get; }

        internal UnregisterSubscriberResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                NumUnsubscribed = (int)a[2];
            }
            else
            {
                Logger.Log($"RcpMaster: UnregisterSubscriberResponse failed: " + StatusMessage);
            }
        }
    }

    public sealed class RegisterPublisherResponse : BaseResponse
    {
        public ReadOnlyCollection<string> Subscribers { get; }

        internal RegisterPublisherResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                object[] tmp = (object[])a[2];
                string[] subscribers = new string[tmp.Length];
                for (int i = 0; i < subscribers.Length; i++)
                {
                    subscribers[i] = (string)tmp[i];
                }
                Subscribers = new ReadOnlyCollection<string>(subscribers);
            }
            else
            {
                Logger.Log($"RcpMaster: RegisterPublisherResponse failed: " + StatusMessage);
                Subscribers = new ReadOnlyCollection<string>(Array.Empty<string>());
            }
        }
    }

    public sealed class UnregisterPublisherResponse : BaseResponse
    {
        public int NumUnregistered { get; }

        internal UnregisterPublisherResponse(object[] a) : base(a)
        {
            if (Code == StatusCode.Success)
            {
                NumUnregistered = (int)a[2];
            }
            else
            {
                Logger.Log($"RcpMaster: UnregisterPublisherResponse failed: " + StatusMessage);
            }
        }
    }

    public sealed class LookupServiceResponse : BaseResponse
    {
        public Uri ServiceUrl { get; }

        internal LookupServiceResponse(object[] a) : base(a)
        {
            ServiceUrl = new Uri((string)a[2]);
        }
    }

    public sealed class UnregisterServiceResponse : BaseResponse
    {
        public int NumUnregistered { get; }

        internal UnregisterServiceResponse(object[] a) : base(a)
        {
            NumUnregistered = (int)a[2];
        }
    }


}
