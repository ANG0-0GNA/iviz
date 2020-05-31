﻿//#define DEBUG__

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Iviz.Msgs;

namespace Iviz.RoslibSharp.XmlRpc
{
    public class FaultException : Exception
    {
        public FaultException() { }
        public FaultException(string message) : base(message) { }

        public FaultException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParseException : Exception
    {
        public ParseException() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public static class Service
    {

        static void Assert(string received, string expected)
        {
            if (received != expected)
            {
                throw new ParseException($"Expected '{expected}' but received '{received}'");
            }
        }

        static object Parse(XmlNode value)
        {
            Assert(value.Name, "value");
            if (!value.HasChildNodes)
            {
                return value.InnerText;
            }

            XmlNode primitive = value.FirstChild;
            if (primitive is XmlText)
            {
                return primitive.InnerText;
            };
            switch (primitive.Name)
            {
                case "double": return double.Parse(primitive.InnerText, BuiltIns.Culture);
                case "i4": return int.Parse(primitive.InnerText, BuiltIns.Culture);
                case "int": return int.Parse(primitive.InnerText, BuiltIns.Culture);
                case "boolean": return primitive.InnerText == "1";
                case "string": return primitive.InnerText;
                case "array":
                    XmlNode data = primitive.FirstChild;
                    Assert(data.Name, "data");
                    object[] children = new object[data.ChildNodes.Count];
                    for (int i = 0; i < data.ChildNodes.Count; i++)
                    {
                        children[i] = Parse(data.ChildNodes[i]);
                    }
                    return children;
                default:
                    return null;
            }
        }

        public static object MethodCall(Uri remoteUri, Uri callerUri, string method, Arg[] args, int timeoutInMs = 2000)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            StringBuilder buffer = new StringBuilder();
            buffer.AppendLine("<?xml version=\"1.0\"?>");
            buffer.AppendLine("<methodCall>");
            buffer.Append("<methodName>").Append(method).AppendLine("</methodName>");
            buffer.AppendLine("<params>");
            foreach (Arg arg in args)
            {
                buffer.AppendLine("<param>");
                buffer.AppendLine(arg);
                buffer.AppendLine("</param>");
            }
            buffer.AppendLine("</params>");
            buffer.AppendLine("</methodCall>");

#if DEBUG__
            Logger.Log("--- MethodCall ---");
            Logger.Log(">> " + buffer);
#endif
            string inData;
            using (HttpRequest request = new HttpRequest(callerUri, remoteUri))
            {
                inData = request.Request(buffer.ToString(), timeoutInMs);
            }

#if DEBUG__
            Logger.Log("<< " + inData);
            Logger.Log("--- End MethodCall ---");
#endif

            XmlDocument document = new XmlDocument();
            document.LoadXml(inData);
            XmlNode root = document.FirstChild;
            while (root.Name != "methodResponse")
            {
                root = root.NextSibling;
            }
            XmlNode child = root.FirstChild;
            if (child.Name == "params")
            {
                if (child.ChildNodes.Count == 0)
                {
                    throw new ParseException("Empty response");
                }
                else if (child.ChildNodes.Count > 1)
                {
                    throw new ParseException("Function call returned too many arguments");
                }
                XmlNode param = child.FirstChild;
                Assert(param.Name, "param");
                return Parse(param.FirstChild);
            }
            else if (child.Name == "fault")
            {
                throw new FaultException(child.FirstChild.InnerXml);
            }
            else
            {
                throw new ParseException($"Expected 'params' or 'fault', but got '{child.Name}'");
            }
        }

        public static void MethodResponse(HttpListenerContext httpContext, Dictionary<string, Func<object[], Arg[]>> methods)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (methods is null)
            {
                throw new ArgumentNullException(nameof(methods));
            }

            string inData = httpContext.GetRequest();

#if DEBUG__
            Logger.Log("--- MethodResponse ---");
            Logger.Log("<< " + inData);
#endif

            XmlDocument document = new XmlDocument();
            document.LoadXml(inData);

            try
            {
                XmlNode root = document.FirstChild;
                while (root != null && root.Name != "methodCall")
                {
                    root = root.NextSibling;
                }
                if (root == null)
                {
                    throw new ParseException("Malformed request: no 'methodCall' found");
                }
                string methodName = null;
                object[] args = null;
                XmlNode child = root.FirstChild;
                do
                {
                    if (child.Name == "params")
                    {
                        args = new object[child.ChildNodes.Count];
                        for (int i = 0; i < child.ChildNodes.Count; i++)
                        {
                            XmlNode param = child.ChildNodes[i];
                            Assert(param.Name, "param");
                            args[i] = Parse(param.FirstChild);

                        }
                    }
                    else if (child.Name == "fault")
                    {
                        throw new FaultException(child.FirstChild.InnerXml);
                    }
                    else if (child.Name == "methodName")
                    {
                        methodName = child.InnerText;
                    }
                    else
                    {
                        throw new ParseException($"Expected 'params', 'fault', or 'methodName', got '{child.Name}'");
                    }
                } while ((child = child.NextSibling) != null);

                StringBuilder buffer = new StringBuilder();
                if (methodName == null ||
                    !methods.TryGetValue(methodName, out Func<object[], Arg[]> method) ||
                    args == null)
                {
                    throw new ParseException($"Unknown function '{methodName}' or invalid arguments");
                }
                else
                {
                    Arg response = new Arg(method(args));

                    buffer.AppendLine("<?xml version=\"1.0\"?>");
                    buffer.AppendLine("<methodResponse>");
                    buffer.AppendLine("<params>");
                    buffer.AppendLine("<param>");
                    buffer.AppendLine(response);
                    buffer.AppendLine("</param>");
                    buffer.AppendLine("</params>");
                    buffer.AppendLine("</methodResponse>");
                    buffer.AppendLine();
                }

#if DEBUG__
                Logger.Log(">> " + buffer);
                Logger.Log("--- End MethodResponse ---");
#endif

                httpContext.Respond(buffer.ToString());
            }
            catch (ParseException e)
            {
                StringBuilder buffer = new StringBuilder();
                buffer.AppendLine("<?xml version=\"1.0\"?>");
                buffer.AppendLine("<methodResponse>");
                buffer.AppendLine("<fault>");
                buffer.AppendLine(new Arg(e.Message));
                buffer.AppendLine("</fault>");
                buffer.AppendLine("</methodResponse>");

                httpContext.Respond(buffer.ToString());
            }
        }
    }
}
