﻿using System;
using System.IO;
using System.Net.Sockets;
using Iviz.Msgs;

namespace Iviz.RoslibSharp.XmlRpc
{
    public sealed class HttpRequest : IDisposable
    {
        readonly Uri callerUri;
        readonly Uri uri;
        readonly TcpClient client;

        public HttpRequest(Uri callerUri, Uri uri)
        {
            this.callerUri = callerUri ?? throw new ArgumentNullException(nameof(callerUri));
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            string hostname = uri.Host;
            int port = uri.Port;

            client = new TcpClient(hostname, port);
        }

        public string Request(string msgIn, int timeoutInMs = 2000)
        {
            if (msgIn is null)
            {
                throw new ArgumentNullException(nameof(msgIn));
            }

            StreamWriter writer = new StreamWriter(client.GetStream(), BuiltIns.UTF8)
            {
                NewLine = "\r\n"
            };

            string path = uri.AbsolutePath;
            writer.WriteLine($"POST {path} HTTP/1.0");
            writer.WriteLine($"User-Agent: iviz XML-RPC");
            writer.WriteLine($"Host: {callerUri.Host}");
            writer.WriteLine($"Content-Length: {BuiltIns.UTF8.GetByteCount(msgIn)}");
            writer.WriteLine($"Content-Type: text/xml; charset=utf-8");
            writer.WriteLine();
            writer.Write(msgIn);
            writer.Flush();

            StreamReader reader = new StreamReader(client.GetStream(), BuiltIns.UTF8);
            reader.BaseStream.ReadTimeout = timeoutInMs;

            string response = reader.ReadToEnd();

            int index = response.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
            if (index == -1)
            {
                index = response.IndexOf("\n\n", StringComparison.InvariantCulture);
                if (index == -1)
                {
                    throw new ParseException("Cannot find double line-end in HTTP header");
                }
                index += 2;
            }
            else
            {
                index += 4;
            }

            reader.Close();
            writer.Close();

            return response.Substring(index);
        }

        bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                client.Close();
                disposed = true;
            }
        }
    }
}
