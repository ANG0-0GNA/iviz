using System.Runtime.Serialization;

namespace Iviz.Msgs.StdMsgs
{
    [DataContract (Name = "std_msgs/Int8")]
    public sealed class Int8 : IMessage
    {
        [DataMember (Name = "data")] public sbyte Data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public Int8()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public Int8(sbyte Data)
        {
            this.Data = Data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal Int8(Buffer b)
        {
            Data = b.Deserialize<sbyte>();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new Int8(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.Data);
        }
        
        public void Validate()
        {
        }
    
        public int RosMessageLength => 1;
    
        string IMessage.RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "std_msgs/Int8";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "27ffa0c9c4b8fb8492252bcad9e5c57b";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE8vMK7FQSEksSeTiAgDmSq87CwAAAA==";
                
    }
}
