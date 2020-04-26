using System.Text;
using System.Runtime.Serialization;

namespace Iviz.Msgs.std_msgs
{
    public sealed class MultiArrayDimension : IMessage
    {
        public string label; // label of given dimension
        public uint size; // size of given dimension (in type units)
        public uint stride; // stride of given dimension
    
        /// <summary> Constructor for empty message. </summary>
        public MultiArrayDimension()
        {
            label = "";
        }
        
        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Deserialize(out label, ref ptr, end);
            BuiltIns.Deserialize(out size, ref ptr, end);
            BuiltIns.Deserialize(out stride, ref ptr, end);
        }
    
        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Serialize(label, ref ptr, end);
            BuiltIns.Serialize(size, ref ptr, end);
            BuiltIns.Serialize(stride, ref ptr, end);
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 12;
                size += Encoding.UTF8.GetByteCount(label);
                return size;
            }
        }
    
        public IMessage Create() => new MultiArrayDimension();
    
        [IgnoreDataMember]
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        public const string RosMessageType = "std_msgs/MultiArrayDimension";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string RosMd5Sum = "4cd0c83a8683deae40ecdac60e53bfa8";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE22NMQqAMBAEe1+xYKOtvkjJGRbiRbxE0NcrUWy0m2KGsbRSPcIwSgBQPxQneG6icJxF" +
                "jVGrTE19B+MhKGahr4iGirQvgqxM1r7hdXJSwpt+HicAFGWdjgAAAA==";
                
    }
}
