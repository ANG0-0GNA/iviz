using System.Runtime.Serialization;

namespace Iviz.Msgs.std_msgs
{
    public sealed class UInt32 : IMessage
    {
        public uint data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public UInt32()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public UInt32(uint data)
        {
            this.data = data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal UInt32(Buffer b)
        {
            this.data = BuiltIns.DeserializeStruct<uint>(b);
        }
        
        public IMessage Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new UInt32(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            BuiltIns.Serialize(this.data, b);
        }
        
        public void Validate()
        {
        }
    
        [IgnoreDataMember]
        public int RosMessageLength => 4;
    
        [IgnoreDataMember]
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve]
        public const string RosMessageType = "std_msgs/UInt32";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve]
        public const string RosMd5Sum = "304a39449588c7f8ce2df6e8001c5fce";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve]
        public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAEyvNzCsxNlJISSxJ5AIAYOk1nQwAAAA=";
                
    }
}
