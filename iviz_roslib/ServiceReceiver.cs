﻿//#define DEBUG__

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Iviz.Msgs;

namespace Iviz.Roslib
{
    internal sealed class ServiceReceiver : IDisposable
    {
        const int BufferSizeIncrease = 512;

        byte[] readBuffer = new byte[16];
        byte[] writeBuffer = new byte[16];

        readonly BinaryReader reader;
        readonly BinaryWriter writer;
        readonly TcpClient tcpClient = new TcpClient();
        readonly IPEndPoint remoteEndPoint;
        readonly ServiceInfo serviceInfo;
        readonly bool requestNoDelay;
        readonly bool persistent;

        public bool IsAlive => tcpClient.Connected;

        int Port => remoteEndPoint.Port;
        string Hostname => remoteEndPoint.Address.ToString();

        public ServiceReceiver(
            ServiceInfo serviceInfo,
            Uri remoteUri,
            bool requestNoDelay,
            bool persistent)
        {
            this.serviceInfo = serviceInfo;
            this.requestNoDelay = requestNoDelay;
            this.persistent = persistent;

            var remoteHostname = remoteUri.Host;
            var remotePort = remoteUri.Port;

            tcpClient.ReceiveTimeout = 5000;
            tcpClient.SendTimeout = 5000;

            tcpClient.Connect(remoteHostname, remotePort);
            NetworkStream stream = tcpClient.GetStream();
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
            remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
        }

        int SerializeHeader(BinaryWriter writer)
        {
            string[] contents = {
                $"callerid={serviceInfo.CallerId}",
                $"service={serviceInfo.Service}",
                $"md5sum={serviceInfo.Md5Sum}",
                $"type={serviceInfo.Type}",
                $"tcp_nodelay={(requestNoDelay ? "1" : "0")}",
                $"persistent={(persistent ? "1" : "0")}",
            };
            int totalLength = 4 * contents.Length;
            for (int i = 0; i < contents.Length; i++)
            {
                totalLength += contents[i].Length;
            }
            writer.Write(totalLength);
            for (int i = 0; i < contents.Length; i++)
            {
                writer.Write(contents[i].Length);
                writer.Write(BuiltIns.UTF8.GetBytes(contents[i]));

#if DEBUG__
                Logger.Log(">>> " + contents[i]);
#endif

            }
            return totalLength;
        }

        List<string> ParseHeader(int totalLength)
        {
            int numRead = 0;

            List<string> contents = new List<string>();
            while (numRead < totalLength)
            {
                int length = BitConverter.ToInt32(readBuffer, numRead);
                numRead += 4;
                string entry = BuiltIns.UTF8.GetString(readBuffer, numRead, length);
                numRead += length;
                contents.Add(entry);

#if DEBUG__
                Logger.Log("<<< " + contents.Last());
#endif
            }
            return contents;
        }

        public bool Start()
        {
            SerializeHeader(writer);

            int totalLength = ReceivePacket();
            List<string> responses = ParseHeader(totalLength);

            if (responses.Count != 0 && responses[0].HasPrefix("error"))
            {
                int index = responses[0].IndexOf('=');
                if (index != -1)
                {
                    Logger.Log($"{this}: Closing socket! Error:\n{responses[0].Substring(index + 1)}");
                }
                else
                {
                    Logger.Log($"{this}: Closing socket! Error:\n{responses[0]}");
                }
                tcpClient.Close();
                return false;
            }
            return true;
        }

        public void Stop()
        {
            tcpClient.Close();
        }

        int ReceivePacket()
        {
            int numRead = 0;
            while (numRead < 4)
            {
                int readNow = reader.Read(readBuffer, numRead, 4 - numRead);
                if (readNow == 0)
                {
                    return 0;
                }
                numRead += readNow;
            }

            int length = BitConverter.ToInt32(readBuffer, 0);
            if (readBuffer.Length < length)
            {
                readBuffer = new byte[length + BufferSizeIncrease];
            }
            numRead = 0;
            while (numRead < length)
            {
                int readNow = reader.Read(readBuffer, numRead, length - numRead);
                if (readNow == 0)
                {
                    return 0;
                }
                numRead += readNow;
            }
            return length;
        }

        public bool Execute(IService service)
        {
            bool success;
            try
            {
                success = ExecuteImpl(service);
            }
            catch (Exception e)
            {
                service.ErrorMessage = e.Message;
                Logger.Log("ServiceReceiver: Error during service call:" + e);
                success = false;
            }
            if (!persistent)
            {
                Stop();
            }
            return success;
        }

        const byte ErrorByte = 0;

        bool ExecuteImpl(IService service)
        {
            IRequest requestMsg = service.Request;
            int msgLength = requestMsg.RosMessageLength;
            if (writeBuffer.Length < msgLength)
            {
                writeBuffer = new byte[msgLength + BufferSizeIncrease];
            }
            uint sendLength = Msgs.Buffer.Serialize(requestMsg, writeBuffer);
            writer.Write(sendLength);

            writer.Write(writeBuffer, 0, (int)sendLength);


            int rcvLengthH = reader.Read(readBuffer, 0, 1);
            if (rcvLengthH == 0)
            {
                service.ErrorMessage = $"Connection to {Hostname}:{Port} closed remotely.";
                return false;
            }

            byte statusByte = readBuffer[0];
            
            int rcvLength = ReceivePacket();
            if (rcvLength == 0)
            {
                service.ErrorMessage = $"Connection to {Hostname}:{Port} closed remotely.";
                return false;
            }

            if (statusByte == ErrorByte)
            {
                service.ErrorMessage = BuiltIns.UTF8.GetString(readBuffer, 0, rcvLength);
                return false;
            }

            service.ErrorMessage = null;
            service.Response = Msgs.Buffer.Deserialize(service.Response, readBuffer, rcvLength);
            return true;
        }

        public void Dispose()
        {
            Stop();
            reader.Dispose();
            writer.Dispose();
            tcpClient.Dispose();
        }
    }
}
