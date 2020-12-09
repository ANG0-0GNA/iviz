﻿using System;
using Iviz.Msgs;

namespace Iviz.Roslib
{
    /// <summary>
    ///     Full info about a ROS topic and its message type, including dependencies.
    /// </summary>
    internal sealed class TopicInfo<T> where T : IMessage
    {
        readonly IDeserializable<T>? generator;

        TopicInfo(string messageDefinition, string callerId, string topic, string md5Sum, string type,
            IDeserializable<T>? generator)
        {
            MessageDefinition = messageDefinition;
            CallerId = callerId;
            Topic = topic;
            Md5Sum = md5Sum;
            Type = type;
            this.generator = generator;
        }

        public TopicInfo(string callerId, string topic, IDeserializable<T>? generator = null)
            : this(
                BuiltIns.DecompressDependency(typeof(T)),
                callerId, topic,
                BuiltIns.GetMd5Sum(typeof(T)),
                BuiltIns.GetMessageType(typeof(T)),
                generator
            )
        {
        }

        /// <summary>
        ///     Concatenated dependencies file.
        /// </summary>
        public string MessageDefinition { get; }

        /// <summary>
        ///     ROS name of this node.
        /// </summary>
        public string CallerId { get; }

        /// <summary>
        ///     Name of this topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     MD5 hash of the compact representation of the message.
        /// </summary>
        public string Md5Sum { get; }

        /// <summary>
        ///     Full ROS message type.
        /// </summary>
        public string Type { get; }

        /// <summary>
        ///     Instance of the message used to generate others of the same type.
        /// </summary>
        public IDeserializable<T> Generator =>
            generator ?? throw new InvalidOperationException("This type does not have a generator!");
    }
}