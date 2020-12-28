/* This file was created automatically, do not edit! */

using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Iviz.Msgs.StdMsgs
{
    [DataContract (Name = "std_msgs/Int16")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Int16 : IMessage, System.IEquatable<Int16>, IDeserializable<Int16>
    {
        [DataMember (Name = "data")] public short Data { get; }
    
        /// <summary> Explicit constructor. </summary>
        public Int16(short Data)
        {
            this.Data = Data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        public Int16(ref Buffer b)
        {
            b.Deserialize(out this);
        }
        
        public readonly ISerializable RosDeserialize(ref Buffer b)
        {
            return new Int16(ref b);
        }
        
        readonly Int16 IDeserializable<Int16>.RosDeserialize(ref Buffer b)
        {
            return new Int16(ref b);
        }
        
        public override readonly int GetHashCode() => (Data).GetHashCode();
        
        public override readonly bool Equals(object? o) => o is Int16 s && Equals(s);
        
        public readonly bool Equals(Int16 o) => (Data) == (o.Data);
        
        public static bool operator==(in Int16 a, in Int16 b) => a.Equals(b);
        
        public static bool operator!=(in Int16 a, in Int16 b) => !a.Equals(b);
    
        public readonly void RosSerialize(ref Buffer b)
        {
            b.Serialize(this);
        }
        
        public readonly void RosValidate()
        {
        }
    
        /// <summary> Constant size of this message. </summary>
        [Preserve] public const int RosFixedMessageLength = 2;
        
        public readonly int RosMessageLength => RosFixedMessageLength;
    
        public readonly string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "std_msgs/Int16";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "8524586e34fbd7cb1c08c5f5f1ca0e57";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE8vMKzE0U0hJLEnk4gIAJDs+BgwAAAA=";
                
    }
}
