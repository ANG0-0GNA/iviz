﻿using System;
using Iviz.Msgs;

namespace Iviz.Roslib
{
    /// <summary>
    /// Full info about a ROS topic and its message type, including dependencies.
    /// </summary>
    class TopicInfo
    {
        /// <summary>
        /// Concatenated dependencies file.
        /// </summary>
        public string MessageDefinition { get; }

        /// <summary>
        /// ROS name of this node.
        /// </summary>
        public string CallerId { get; }

        /// <summary>
        /// Name of this topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        /// MD5 hash of the compact representation of the message.
        /// </summary>
        public string Md5Sum { get; }

        /// <summary>
        /// Full ROS message type.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Instance of the message used to generate others of the same type.
        /// </summary>
        public IMessage Generator { get; }

        public TopicInfo(string messageDefinition, string callerId, string topic, string md5Sum, string type, IMessage generator)
        {
            MessageDefinition = messageDefinition;
            CallerId = callerId;
            Topic = topic;
            Md5Sum = md5Sum;
            Type = type;
            Generator = generator;
        }

        public TopicInfo(string callerId, string topic, Type type, IMessage generator = null)
        : this(
                BuiltIns.DecompressDependency(type),
                callerId, topic,
                BuiltIns.GetMd5Sum(type),
                BuiltIns.GetMessageType(type),
                generator
                )
        {
        }
    }
}
