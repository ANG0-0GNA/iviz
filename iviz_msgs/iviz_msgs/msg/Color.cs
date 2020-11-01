/* This file was created automatically, do not edit! */

using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Iviz.Msgs.IvizMsgs
{
    [DataContract (Name = "iviz_msgs/Color")]
    [StructLayout(LayoutKind.Sequential)]
    public struct Color : IMessage, System.IEquatable<Color>, IDeserializable<Color>
    {
        [DataMember (Name = "r")] public byte R { get; set; }
        [DataMember (Name = "g")] public byte G { get; set; }
        [DataMember (Name = "b")] public byte B { get; set; }
        [DataMember (Name = "a")] public byte A { get; set; }
    
        /// <summary> Explicit constructor. </summary>
        public Color(byte R, byte G, byte B, byte A)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal Color(ref Buffer b)
        {
            b.Deserialize(out this);
        }
        
        public readonly ISerializable RosDeserialize(ref Buffer b)
        {
            return new Color(ref b);
        }
        
        readonly Color IDeserializable<Color>.RosDeserialize(ref Buffer b)
        {
            return new Color(ref b);
        }
        
        public override readonly int GetHashCode() => (R, G, B, A).GetHashCode();
        
        public override readonly bool Equals(object? o) => o is Color s && Equals(s);
        
        public readonly bool Equals(Color o) => (R, G, B, A) == (o.R, o.G, o.B, o.A);
        
        public static bool operator==(in Color a, in Color b) => a.Equals(b);
        
        public static bool operator!=(in Color a, in Color b) => !a.Equals(b);
    
        public readonly void RosSerialize(ref Buffer b)
        {
            b.Serialize(this);
        }
        
        public readonly void RosValidate()
        {
        }
    
        public readonly int RosMessageLength => 4;
    
        public readonly string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "iviz_msgs/Color";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "3a89b17adab5bedef0b554f03235d9b3";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAEyvNzCuxUCjiKgXT6VA6CUoncnEBACHBa7shAAAA";
                
    }
}
