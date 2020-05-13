using System.Runtime.Serialization;

namespace Iviz.Msgs.std_msgs
{
    [DataContract]
    public sealed class UInt16 : IMessage
    {
        [DataMember] public ushort data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public UInt16()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public UInt16(ushort data)
        {
            this.data = data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal UInt16(Buffer b)
        {
            this.data = b.Deserialize<ushort>();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new UInt16(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.data);
        }
        
        public void Validate()
        {
        }
    
        public int RosMessageLength => 2;
    
        string IMessage.RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "std_msgs/UInt16";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "1df79edf208b629fe6b81923a544552d";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAEyvNzCsxNFNISSxJ5OICAF50RNUNAAAA";
                
    }
}
