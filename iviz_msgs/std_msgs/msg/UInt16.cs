/* This file was created automatically, do not edit! */

using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Iviz.Msgs.StdMsgs
{
    [DataContract (Name = "std_msgs/UInt16")]
    [StructLayout(LayoutKind.Sequential)]
    public struct UInt16 : IMessage, System.IEquatable<UInt16>, IDeserializable<UInt16>
    {
        [DataMember (Name = "data")] public ushort Data { get; set; }
    
        /// <summary> Explicit constructor. </summary>
        public UInt16(ushort Data)
        {
            this.Data = Data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        public UInt16(ref Buffer b)
        {
            b.Deserialize(out this);
        }
        
        public readonly ISerializable RosDeserialize(ref Buffer b)
        {
            return new UInt16(ref b);
        }
        
        readonly UInt16 IDeserializable<UInt16>.RosDeserialize(ref Buffer b)
        {
            return new UInt16(ref b);
        }
        
        public override readonly int GetHashCode() => (Data).GetHashCode();
        
        public override readonly bool Equals(object? o) => o is UInt16 s && Equals(s);
        
        public readonly bool Equals(UInt16 o) => (Data) == (o.Data);
        
        public static bool operator==(in UInt16 a, in UInt16 b) => a.Equals(b);
        
        public static bool operator!=(in UInt16 a, in UInt16 b) => !a.Equals(b);
    
        public readonly void RosSerialize(ref Buffer b)
        {
            b.Serialize(this);
        }
        
        public readonly void RosValidate()
        {
        }
    
        /// <summary> Constant size of this message. </summary>
        public const int RosFixedMessageLength = 2;
        
        public readonly int RosMessageLength => RosFixedMessageLength;
    
        public readonly string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "std_msgs/UInt16";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "1df79edf208b629fe6b81923a544552d";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAACivNzCsxNFNISSxJ5OXi5QIArq0l+g8AAAA=";
                
    }
}
