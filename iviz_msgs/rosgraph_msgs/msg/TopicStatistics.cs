/* This file was created automatically, do not edit! */

using System.Runtime.Serialization;

namespace Iviz.Msgs.RosgraphMsgs
{
    [DataContract (Name = "rosgraph_msgs/TopicStatistics")]
    public sealed class TopicStatistics : IDeserializable<TopicStatistics>, IMessage
    {
        // name of the topic
        [DataMember (Name = "topic")] public string Topic { get; set; }
        // node id of the publisher
        [DataMember (Name = "node_pub")] public string NodePub { get; set; }
        // node id of the subscriber
        [DataMember (Name = "node_sub")] public string NodeSub { get; set; }
        // the statistics apply to this time window
        [DataMember (Name = "window_start")] public time WindowStart { get; set; }
        [DataMember (Name = "window_stop")] public time WindowStop { get; set; }
        // number of messages delivered during the window
        [DataMember (Name = "delivered_msgs")] public int DeliveredMsgs { get; set; }
        // numbers of messages dropped during the window
        [DataMember (Name = "dropped_msgs")] public int DroppedMsgs { get; set; }
        // traffic during the window, in bytes
        [DataMember (Name = "traffic")] public int Traffic { get; set; }
        // mean/stddev/max period between two messages
        [DataMember (Name = "period_mean")] public duration PeriodMean { get; set; }
        [DataMember (Name = "period_stddev")] public duration PeriodStddev { get; set; }
        [DataMember (Name = "period_max")] public duration PeriodMax { get; set; }
        // mean/stddev/max age of the message based on the
        // timestamp in the message header. In case the
        // message does not have a header, it will be 0.
        [DataMember (Name = "stamp_age_mean")] public duration StampAgeMean { get; set; }
        [DataMember (Name = "stamp_age_stddev")] public duration StampAgeStddev { get; set; }
        [DataMember (Name = "stamp_age_max")] public duration StampAgeMax { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TopicStatistics()
        {
            Topic = "";
            NodePub = "";
            NodeSub = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public TopicStatistics(string Topic, string NodePub, string NodeSub, time WindowStart, time WindowStop, int DeliveredMsgs, int DroppedMsgs, int Traffic, duration PeriodMean, duration PeriodStddev, duration PeriodMax, duration StampAgeMean, duration StampAgeStddev, duration StampAgeMax)
        {
            this.Topic = Topic;
            this.NodePub = NodePub;
            this.NodeSub = NodeSub;
            this.WindowStart = WindowStart;
            this.WindowStop = WindowStop;
            this.DeliveredMsgs = DeliveredMsgs;
            this.DroppedMsgs = DroppedMsgs;
            this.Traffic = Traffic;
            this.PeriodMean = PeriodMean;
            this.PeriodStddev = PeriodStddev;
            this.PeriodMax = PeriodMax;
            this.StampAgeMean = StampAgeMean;
            this.StampAgeStddev = StampAgeStddev;
            this.StampAgeMax = StampAgeMax;
        }
        
        /// <summary> Constructor with buffer. </summary>
        public TopicStatistics(ref Buffer b)
        {
            Topic = b.DeserializeString();
            NodePub = b.DeserializeString();
            NodeSub = b.DeserializeString();
            WindowStart = b.Deserialize<time>();
            WindowStop = b.Deserialize<time>();
            DeliveredMsgs = b.Deserialize<int>();
            DroppedMsgs = b.Deserialize<int>();
            Traffic = b.Deserialize<int>();
            PeriodMean = b.Deserialize<duration>();
            PeriodStddev = b.Deserialize<duration>();
            PeriodMax = b.Deserialize<duration>();
            StampAgeMean = b.Deserialize<duration>();
            StampAgeStddev = b.Deserialize<duration>();
            StampAgeMax = b.Deserialize<duration>();
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new TopicStatistics(ref b);
        }
        
        TopicStatistics IDeserializable<TopicStatistics>.RosDeserialize(ref Buffer b)
        {
            return new TopicStatistics(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            b.Serialize(Topic);
            b.Serialize(NodePub);
            b.Serialize(NodeSub);
            b.Serialize(WindowStart);
            b.Serialize(WindowStop);
            b.Serialize(DeliveredMsgs);
            b.Serialize(DroppedMsgs);
            b.Serialize(Traffic);
            b.Serialize(PeriodMean);
            b.Serialize(PeriodStddev);
            b.Serialize(PeriodMax);
            b.Serialize(StampAgeMean);
            b.Serialize(StampAgeStddev);
            b.Serialize(StampAgeMax);
        }
        
        public void RosValidate()
        {
            if (Topic is null) throw new System.NullReferenceException(nameof(Topic));
            if (NodePub is null) throw new System.NullReferenceException(nameof(NodePub));
            if (NodeSub is null) throw new System.NullReferenceException(nameof(NodeSub));
        }
    
        public int RosMessageLength
        {
            get {
                int size = 88;
                size += BuiltIns.UTF8.GetByteCount(Topic);
                size += BuiltIns.UTF8.GetByteCount(NodePub);
                size += BuiltIns.UTF8.GetByteCount(NodeSub);
                return size;
            }
        }
    
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "rosgraph_msgs/TopicStatistics";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "10152ed868c5097a5e2e4a89d7daa710";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAACn2RW26DQAxF/5HYg6X8VknVrqKrQAPjBEvMQ2MDye5rHhNogvo5nnOvfe0TeOMQwhWk" +
                "RZAQqSkLlkT+ll9lcQIfLALZzMW+7ohbTE92AiotH+Lc19wkql95zvwMiRFioYbBxNg9tL3WiUFIBxzJ" +
                "2zCWxe5RqSLJaynEdYTeab9pAofM5oYMFjsaMKEF2y8B282YvHx/bUjl+MabD/81SiHGf20WYDWZEyZz" +
                "vVLzLvkA8lA/BDmLV3TROTT+wmItDhdn7hAxUbBQo4yIHmQMz7HKQs11h8GvVDWJ36s8ux3Q5n7cU83z" +
                "JddeUBvW/KrV4hxPb6DncHFKswdbNBbTGX48NKrJfP62Qdfpg0BrBgSz4roT0fV0neaEz/Nu1LlHpcrX" +
                "bNvHW7ydZkn4C0WtpQn0AgAA";
                
    }
}
