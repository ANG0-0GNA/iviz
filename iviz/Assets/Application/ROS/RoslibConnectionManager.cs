﻿using Iviz.Msgs;
using Iviz.RoslibSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Iviz.App
{
    public sealed class RoslibConnection : RosConnection
    {
        RosClient client;

        abstract class AdvertisedTopic
        {
            public RosPublisher Publisher { get; protected set; }
            public virtual int Id { get; set; }

            public abstract void Add(RosSender subscriber);
            public abstract void Remove(RosSender subscriber);
            public abstract int Count { get; }
            public abstract void Advertise(RosClient client, string topic);

            public void Invalidate()
            {
                Id = -1;
                Publisher = null;
            }
        }

        class AdvertisedTopic<T> : AdvertisedTopic where T : IMessage
        {
            readonly HashSet<RosSender<T>> senders = new HashSet<RosSender<T>>();

            public override int Id
            {
                get => base.Id;
                set
                {
                    base.Id = value;
                    foreach (RosSender<T> sender in senders)
                    {
                        sender.Id = value;
                    }
                }
            }

            public override void Add(RosSender publisher)
            {
                senders.Add((RosSender<T>)publisher);
            }

            public override void Remove(RosSender publisher)
            {
                senders.Remove((RosSender<T>)publisher);
            }

            public override int Count => senders.Count;

            public override void Advertise(RosClient client, string topic)
            {
                RosPublisher publisher = null;
                client?.Advertise<T>(topic, out publisher);
                Publisher = publisher;
            }
        }

        abstract class SubscribedTopic
        {
            public RosSubscriber Subscriber { get; protected set; }

            public abstract void Add(RosListener subscriber);
            public abstract void Remove(RosListener subscriber);
            public abstract int Count { get; }
            public abstract void Subscribe(RosClient client, string topic);

            public void Invalidate()
            {
                Subscriber = null;
            }
        }

        class SubscribedTopic<T> : SubscribedTopic where T : IMessage, new()
        {
            readonly HashSet<RosListener<T>> listeners = new HashSet<RosListener<T>>();

            public override void Add(RosListener subscriber)
            {
                listeners.Add((RosListener<T>)subscriber);
            }

            public override void Remove(RosListener subscriber)
            {
                listeners.Remove((RosListener<T>)subscriber);
            }

            public void Callback(T msg)
            {
                foreach (RosListener<T> listener in listeners)
                {
                    listener.EnqueueMessage(msg);
                }
            }

            public override void Subscribe(RosClient client, string topic)
            {
                RosSubscriber subscriber = null;
                client?.Subscribe<T>(topic, Callback, out subscriber);
                Subscriber = subscriber;
            }

            public override int Count => listeners.Count;

        }

        readonly Dictionary<string, AdvertisedTopic> publishersByTopic = new Dictionary<string, AdvertisedTopic>();
        readonly Dictionary<string, SubscribedTopic> subscribersByTopic = new Dictionary<string, SubscribedTopic>();
        readonly List<RosPublisher> publishers = new List<RosPublisher>();

        public override Uri MasterUri
        {
            get => base.MasterUri;
            set
            {
                base.MasterUri = value;
                Disconnect();
            }
        }

        //public override string Id => "/Iviz";
        public override string MyId
        {
            get => base.MyId;
            set
            {
                base.MyId = value;
                Disconnect();
            }
        }

        public override Uri MyUri
        {
            get => base.MyUri;
            set
            {
                base.MyUri = value;
                Disconnect();
            }
        }

        protected override bool Connect()
        {
            //Debug.Log("Connecting! '" + Uri + "'");
            if (MasterUri == null ||
                MasterUri.Scheme != "http" ||
                MyId == null ||
                MyUri == null ||
                MyUri.Scheme != "http")
            {
                return false;
            }

            //Debug.Log("Valid");

            try
            {

                RoslibSharp.Logger.LogDebug = x => Logger.Debug(x);
                RoslibSharp.Logger.LogError = x => Logger.Error(x);
                RoslibSharp.Logger.Log = x => Logger.Info(x);

                client = new RosClient(MasterUri, MyId, MyUri);

                foreach (var entry in publishersByTopic)
                {
                    //Logger.Debug("Late advertisement for " + entry.Key);
                    entry.Value.Advertise(client, entry.Key);
                    entry.Value.Id = publishers.Count;
                    publishers.Add(entry.Value.Publisher);
                }
                foreach (var entry in subscribersByTopic)
                {
                    //Logger.Debug("Late subscription for " + entry.Key);
                    entry.Value.Subscribe(client, entry.Key);
                }

                return true;
            }
            catch (Exception e) when (e is ArgumentException)
            {
                Logger.Debug(e);
                client = null;
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                client = null;
                return false;
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();

            if (client == null)
            {
                return;
            }

            Debug.Log("RosLibConnection: Disconnecting...");
            client.Close();
            client = null;
            foreach (var entry in publishersByTopic)
            {
                entry.Value.Invalidate();
            }
            foreach (var entry in subscribersByTopic)
            {
                entry.Value.Invalidate();
            }
            publishers.Clear();
            Debug.Log("RosLibConnection: Disconnection finished.");
        }

        public override void Advertise<T>(RosSender<T> advertiser)
        {
            AddTask(() =>
            {
                try
                {
                    AdvertiseImpl(advertiser);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Disconnect();
                }
            });
        }

        void AdvertiseImpl<T>(RosSender<T> advertiser) where T : IMessage
        {
            if (!publishersByTopic.TryGetValue(advertiser.Topic, out AdvertisedTopic advertisedTopic))
            {
                RosPublisher publisher = null;
                AdvertisedTopic<T> newAdvertisedTopic = new AdvertisedTopic<T>();

                int id;
                if (client != null)
                {
                    newAdvertisedTopic.Advertise(client, advertiser.Topic);
                    publisher = newAdvertisedTopic.Publisher;
                    //Logger.Debug("Direct advertisement for " + advertiser.Topic);

                    //client?.Advertise<T>(advertiser.Topic, out publisher);
                    id = publishers.FindIndex(x => x is null);
                    if (id == -1)
                    {
                        id = publishers.Count;
                        publishers.Add(publisher);
                        //Logger.Debug("Id is " + id);
                    }
                    else
                    {
                        publishers[id] = publisher;
                        //Logger.Debug("Id is " + id);
                    }
                    PublishedTopics = client.PublishedTopics;
                }
                else
                {
                    id = -1;
                }

                advertisedTopic = newAdvertisedTopic;
                advertisedTopic.Id = id;
                publishersByTopic.Add(advertiser.Topic, advertisedTopic);
            }
            advertisedTopic.Add(advertiser);
            advertiser.Id = advertisedTopic.Id;
        }

        public override void Publish(RosSender advertiser, IMessage msg)
        {
            AddTask(() =>
            {
                try
                {
                    PublishImpl(advertiser, msg);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Disconnect();
                }
            });
        }


        void PublishImpl(RosSender advertiser, IMessage msg)
        {
            if (advertiser.Id == -1)
            {
                return;
            }
            //Debug.Log("Trying to publish: " + advertiser.Id + " -> " + publishers[advertiser.Id]);
            publishers[advertiser.Id]?.Publish(msg);
        }

        public override void Subscribe<T>(RosListener<T> listener)
        {
            AddTask(() =>
            {
                try
                {
                    SubscribeImpl(listener);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Disconnect();
                }
            });
        }

        void SubscribeImpl<T>(RosListener<T> listener) where T : IMessage, new()
        {
            if (!subscribersByTopic.TryGetValue(listener.Topic, out SubscribedTopic subscribedTopic))
            {
                SubscribedTopic<T> newSubscribedTopic = new SubscribedTopic<T>();

                newSubscribedTopic.Subscribe(client, listener.Topic);
                //client?.Subscribe<T>(listener.Topic, newSubscribedTopic.Callback, out subscriber);

                subscribedTopic = newSubscribedTopic;
                subscribersByTopic.Add(listener.Topic, subscribedTopic);
            }
            subscribedTopic.Add(listener);
        }

        public override void Unadvertise(RosSender advertiser)
        {
            AddTask(() =>
            {
                try
                {
                    UnadvertiseImpl(advertiser);

                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Disconnect();
                }
            });
        }

        void UnadvertiseImpl(RosSender advertiser)
        {
            if (publishersByTopic.TryGetValue(advertiser.Topic, out AdvertisedTopic advertisedTopic))
            {
                advertisedTopic.Remove(advertiser);
                if (advertisedTopic.Count == 0)
                {
                    publishersByTopic.Remove(advertiser.Topic);
                    publishers[advertiser.Id] = null;

                    if (client != null)
                    {
                        advertisedTopic.Publisher.Unadvertise(advertiser.Topic);
                        PublishedTopics = client.PublishedTopics;
                    }
                }
            }
        }

        public override void Unsubscribe(RosListener subscriber)
        {
            AddTask(() =>
            {
                try
                {
                    UnsubscribeImpl(subscriber);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Disconnect();
                }
            });
        }


        void UnsubscribeImpl(RosListener subscriber)
        {
            if (subscribersByTopic.TryGetValue(subscriber.Topic, out SubscribedTopic subscribedTopic))
            {
                subscribedTopic.Remove(subscriber);
                if (subscribedTopic.Count == 0)
                {
                    subscribersByTopic.Remove(subscriber.Topic);
                    subscribedTopic.Subscriber?.Unsubscribe(subscriber.Topic);
                }
            }
        }

        public override ReadOnlyCollection<BriefTopicInfo> GetSystemPublishedTopics() =>
            client?.GetSystemPublishedTopics() ?? EmptyTopics;

        protected override void Update()
        {
            AddTask(() =>
            {
                //Debug.Log("Pre");
                client?.CheckListenerHack();
                //Debug.Log("Post");
            });
        }

        public override int GetNumPublishers(string topic)
        {
            subscribersByTopic.TryGetValue(topic, out SubscribedTopic subscribedTopic);
            return subscribedTopic?.Subscriber?.NumPublishers ?? 0;
        }

        public override int GetNumSubscribers(string topic)
        {
            publishersByTopic.TryGetValue(topic, out AdvertisedTopic advertisedTopic);
            return advertisedTopic?.Publisher?.NumSubscribers ?? 0;
        }

        public override void Stop()
        {
            base.Stop();
            client?.Close();
            client = null;
        }
    }
}