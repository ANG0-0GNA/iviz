﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iviz.Core;
using Iviz.Msgs;
using Iviz.MsgsGen.Dynamic;
using Iviz.Ros;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Iviz.App
{
    public sealed class EchoDialogData : DialogData
    {
        const int MaxMessageLength = 1000;
        const int MaxMessages = 100;

        [NotNull] readonly EchoDialogContents dialog;
        public override IDialogPanelContents Panel => dialog;

        readonly Dictionary<string, Type> topicTypes = new Dictionary<string, Type>();
        readonly Queue<(string, IMessage)> messageQueue = new Queue<(string, IMessage)>();
        readonly List<TopicEntry> entries = new List<TopicEntry>();
        readonly StringBuilder messageBuffer = new StringBuilder();
        IListener listener;
        bool queueIsDirty;

        class TopicEntry : IComparable<TopicEntry>
        {
            [CanBeNull] public string Topic { get; }
            [CanBeNull] public string RosMsgType { get; }
            [NotNull] public Type CsType { get; }
            [NotNull] public string Description { get; }

            public TopicEntry()
            {
                Topic = null;
                RosMsgType = null;
                CsType = typeof(object);
                Description = $"<color=grey>(None)</color>";
            }

            public TopicEntry([NotNull] string topic, [NotNull] string rosMsgType, [NotNull] Type csType)
            {
                Topic = topic;
                RosMsgType = rosMsgType;
                CsType = csType;

                int lastSlash = RosMsgType.LastIndexOf('/');
                string shortType = (lastSlash == -1) ? RosMsgType : RosMsgType.Substring(lastSlash + 1);
                Description = $"{topic} <color=grey>[{shortType}]</color>";
            }

            public int CompareTo(TopicEntry other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return string.Compare(Topic, other.Topic, StringComparison.Ordinal);
            }
        }

        public EchoDialogData()
        {
            dialog = DialogPanelManager.GetPanelByType<EchoDialogContents>(DialogPanelType.Echo);
        }

        bool TryGetType([NotNull] string rosMsgType, out Type type)
        {
            if (topicTypes.TryGetValue(rosMsgType, out type))
            {
                return true;
            }

            type = BuiltIns.TryGetTypeFromMessageName(rosMsgType);
            if (type == null)
            {
                return false;
            }
            
            topicTypes.Add(rosMsgType, type);
            return true;

        }

        void CreateListener(string topicName, string rosMsgType, [NotNull] Type csType)
        {
            if (listener != null)
            {
                if (listener.Topic == topicName && listener.Type == rosMsgType)
                {
                    return;
                }

                listener.Stop();
            }

            if (csType == typeof(DynamicMessage))
            {
                listener = new Listener<DynamicMessage>(topicName, Handler);
            }
            else
            {
                Action<IMessage> handler = Handler;
                Type listenerType = typeof(Listener<>).MakeGenericType(csType);
                listener = (IListener) Activator.CreateInstance(listenerType, topicName, handler);
            }
            
        }

        void CreateTopicList()
        {
            var newTopics = ConnectionManager.Connection.GetSystemTopicTypes();
            entries.Clear();

            entries.Add(new TopicEntry());

            foreach (var entry in newTopics)
            {
                string topic = entry.Topic;
                string msgType = entry.Type;

                Type csType = TryGetType(msgType, out Type newCsType) ? newCsType : typeof(DynamicMessage);
                entries.Add(new TopicEntry(topic, msgType, csType));
            }
            
            entries.Sort();
        }

        void Handler(IMessage msg)
        {
            string time = $"<b>{GameThread.Now.ToString("HH:mm:ss")}</b> ";
            messageQueue.Enqueue((time, msg));
            if (messageQueue.Count > MaxMessages)
            {
                messageQueue.Dequeue();
            }

            queueIsDirty = true;
        }


        public override void SetupPanel()
        {
            dialog.Close.Clicked += Close;
            dialog.Topics.ValueChanged += (i, _) =>
            {
                if (i == 0)
                {
                    listener?.Stop();
                    listener = null;
                    return;
                }

                var entry = entries[i];
                CreateListener(entry.Topic, entry.RosMsgType, entry.CsType);

                messageQueue.Clear();
                queueIsDirty = false;
                messageBuffer.Length = 0;
                dialog.Text.text = "";
            };

            UpdateOptions();
        }

        void UpdateOptions()
        {
            CreateTopicList();
            dialog.Topics.Options = Enumerable.Select(entries, entry => entry.Description);
        }

        public override void UpdatePanel()
        {
            UpdateOptions();
            ProcessMessages();
            if (listener == null)
            {
                dialog.Publishers.text = "---";
                dialog.Messages.text = "---";
                dialog.KBytes.text = "---";
            }
            else
            {
                dialog.Publishers.text = $"{listener.NumPublishers.Active.ToString()}/{listener.NumPublishers.Total.ToString()} publishers";
                dialog.Messages.text = $"{listener.Stats.MessagesPerSecond.ToString()} mps";
                long kBytesPerSecond = listener.Stats.BytesPerSecond / 1000;
                dialog.KBytes.text = $"{kBytesPerSecond.ToString("N0")} kB/s";
            }
        }

        void ProcessMessages()
        {
            if (!queueIsDirty)
            {
                return;
            }

            messageBuffer.Length = 0;

            (string time, IMessage msg)[] messages = messageQueue.ToArray();
            foreach ((string time, IMessage msg) in messages)
            {
                string msgAsText = JsonConvert.SerializeObject(msg, Formatting.Indented,
                    new ClampJsonConverter(MaxMessageLength));
                messageBuffer.Append(time);
                messageBuffer.Append(msgAsText).AppendLine();
            }

            dialog.Text.text = messageBuffer.ToString();
            queueIsDirty = false;
        }
    }
}