﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Iviz.Msgs;
using Iviz.XmlRpc;

namespace Iviz.Roslib.XmlRpc
{
    internal sealed class NodeClient
    {
        public string CallerId { get; }
        public Uri CallerUri { get; }
        public int TimeoutInMs { get; }
        public Uri Uri { get; }

        public NodeClient(string callerId, Uri callerUri, Uri otherUri, int timeoutInMs = 2000)
        {
            CallerId = callerId;
            CallerUri = callerUri;
            Uri = otherUri;
            TimeoutInMs = timeoutInMs;
        }

        public RequestTopicResponse RequestTopic(string topic, string[][] protocols)
        {
            Arg[] args = {CallerId, topic, protocols};
            object[] response = MethodCall("requestTopic", args);
            return new RequestTopicResponse(response);
        }
        
        public async Task<RequestTopicResponse> RequestTopicAsync(string topic, string[][] protocols)
        {
            Arg[] args = {CallerId, topic, protocols};
            object[] response = await MethodCallAsync("requestTopic", args).Caf();
            return new RequestTopicResponse(response);
        }        

        public GetMasterUriResponse GetMasterUri()
        {
            Arg[] args = {CallerId};
            object[] response = MethodCall("getMasterUri", args);
            return new GetMasterUriResponse(response);
        }

        public async Task<GetMasterUriResponse> GetMasterUriAsync()
        {
            Arg[] args = {CallerId};
            object[] response = await MethodCallAsync("getMasterUri", args).Caf();
            return new GetMasterUriResponse(response);
        }

        public GetPidResponse GetPid()
        {
            Arg[] args = {CallerId};
            object[] response = MethodCall("getPid", args);
            return new GetPidResponse(response);
        }

        public async Task<GetPidResponse> GetPidAsync()
        {
            Arg[] args = {CallerId};
            object[] response = await MethodCallAsync("getPid", args).Caf();
            return new GetPidResponse(response);
        }

        object[] MethodCall(string function, IEnumerable<Arg> args)
        {
            object tmp = XmlRpcService.MethodCall(Uri, CallerUri, function, args, TimeoutInMs);
            if (tmp is object[] result)
            {
                return result;
            }

            Logger.Log($"Rpc Response: Expected type object[], got {tmp?.GetType().Name}");
            return null;
        }
        
        async Task<object[]> MethodCallAsync(string function, IEnumerable<Arg> args)
        {
            object tmp = await XmlRpcService.MethodCallAsync(Uri, CallerUri, function, args, TimeoutInMs).Caf();
            if (tmp is object[] result)
            {
                return result;
            }

            Logger.Log($"Rpc Response: Expected type object[], got {tmp?.GetType().Name}");
            return null;
        }        
        
        
        public class ProtocolResponse
        {
            public string Type { get; } = "";
            public string Hostname { get; } = "";
            public int Port { get; }

            public ProtocolResponse()
            {
            }

            public ProtocolResponse(string type, string hostname, int port)
            {
                Type = type;
                Hostname = hostname;
                Port = port;
            }
        }

        public class RequestTopicResponse : BaseResponse
        {
            public ProtocolResponse Protocol { get; }

            public RequestTopicResponse(object[] a) : base(a)
            {
                if (!IsValid || !EnsureSize(a, 3))
                {
                    Protocol = new ProtocolResponse();
                    return;
                }

                if (a.Length == 0)
                {
                    Protocol = new ProtocolResponse();
                }
                else
                {
                    if (a[2] is object[] tmp)
                    {
                        a = tmp;
                        if (!EnsureSize(a, 3))
                        {
                            Protocol = new ProtocolResponse();
                            return;
                        }
                    }
                    Protocol = new ProtocolResponse
                    (
                        type: Cast<string>(a[0]),
                        hostname: Cast<string>(a[1]),
                        port: Cast<int>(a[2])
                    );
                }
            }
        }

        public class GetMasterUriResponse : BaseResponse
        {
            public Uri Uri { get; }

            public GetMasterUriResponse(object[] a) : base(a)
            {
                if (hasParseError || Code != StatusCode.Success)
                {
                    return;
                }
                if (Uri.TryCreate((string)a[2], UriKind.Absolute, out Uri uri))
                {
                    Uri = uri;
                }
                else
                {
                    Logger.Log($"RpcNodeClient: Failed to parse GetUriResponse uri: " + a[2]);
                    hasParseError = true;
                    Uri = null;
                }
            }
        }

        public class GetPidResponse : BaseResponse
        {
            public int Pid { get; }

            public GetPidResponse(object[] a) : base(a)
            {
                if (!IsValid || !EnsureSize(a, 3))
                {
                    return;
                }
                Pid = Cast<int>(a[2]);
            }
        }
        
    }
}
