﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Iviz.Msgs;
using Iviz.RoslibSharp;
using UnityEngine;

namespace Iviz.App
{
    [DataContract]
    public class RosListenerStats : JsonToString
    {
        [DataMember] public int TotalMessages { get; }
        [DataMember] public float JitterMin { get; }
        [DataMember] public float JitterMax { get; }
        [DataMember] public float JitterMean { get; }
        [DataMember] public int MessagesPerSecond { get; }
        [DataMember] public int BytesPerSecond { get; }
        [DataMember] public int MessagesInQueue { get; }

        public RosListenerStats() { }

        public RosListenerStats(int totalMessages, float jitterMin, float jitterMax,
            float jitterMean, int messagesPerSecond, int bytesPerSecond, int messagesInQueue)
        {
            TotalMessages = totalMessages;
            JitterMin = jitterMin;
            JitterMax = jitterMax;
            JitterMean = jitterMean;
            MessagesPerSecond = messagesPerSecond;
            BytesPerSecond = bytesPerSecond;
            MessagesInQueue = messagesInQueue;
        }
    }

    public abstract class RosListener
    {
        public string Topic { get; }
        public string Type { get; }
        public RosListenerStats Stats { get; protected set; } = new RosListenerStats();

        public int NumPublishers => ConnectionManager.Connection.GetNumPublishers(Topic);

        public int TotalMsgCounter { get; protected set; }
        public int MsgsInQueue { get; protected set; }
        public int MaxQueueSize { get; set; } = 50;
        public int TotalMsgBytes { get; protected set; }
        protected readonly List<float> timesOfArrival = new List<float>();

        protected RosListener(string topic, string type)
        {
            Logger.Internal($"Subscribing to <b>{topic}</b> <i>[{type}]</i>.");

            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            Type = type ?? throw new ArgumentNullException(nameof(type));

            GameThread.EverySecond += UpdateStats;
        }

        public virtual void Stop()
        {
            GameThread.EverySecond -= UpdateStats;
        }

        void UpdateStats()
        {
            if (!timesOfArrival.Any())
            {
                Stats = new RosListenerStats();
                return;
            }
            else
            {
                float jitterMin = float.MaxValue;
                float jitterMax = float.MinValue;

                for (int i = 0; i < timesOfArrival.Count() - 1; i++)
                {
                    float jitter = timesOfArrival[i + 1] - timesOfArrival[i];
                    if (jitter < jitterMin) jitterMin = jitter;
                    if (jitter > jitterMax) jitterMax = jitter;
                }

                Stats = new RosListenerStats(
                    TotalMsgCounter,
                    jitterMin,
                    jitterMax,
                    timesOfArrival.Count == 0 ? 0 : (timesOfArrival.Last() - timesOfArrival.First()) / timesOfArrival.Count(),
                    timesOfArrival.Count,
                    TotalMsgBytes,
                    MsgsInQueue
                );
                TotalMsgBytes = 0;
                timesOfArrival.Clear();
            }
        }
    }

    public sealed class RosListener<T> : RosListener where T : IMessage, new()
    {
        readonly Action<T> subscriptionHandler;

        public RosListener(string topic, Action<T> handler) :
            base(topic, BuiltIns.GetMessageType(typeof(T)))
        {
            subscriptionHandler = handler;

            ConnectionManager.Subscribe(this);
        }

        public void EnqueueMessage(T t)
        {
            if (MsgsInQueue >= MaxQueueSize)
            {
                return;
            }
            MsgsInQueue++;
            GameThread.RunOnce(() =>
            {
                TotalMsgCounter++;
                MsgsInQueue--;
                TotalMsgBytes += t.RosMessageLength;
                timesOfArrival.Add(Time.time);
                try
                {
                    subscriptionHandler(t);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            });
        }

        public override void Stop()
        {
            base.Stop();
            Logger.Internal($"Unsubscribing from {Topic}.");
            ConnectionManager.Unsubscribe(this);
        }
    }
}



