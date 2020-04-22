
namespace Iviz.Msgs.geometry_msgs
{
    public struct Point : IMessage
    {
        // This contains the position of a point in free space
        public double x;
        public double y;
        public double z;
    
        /// <summary> Full ROS name of this message. </summary>
        public const string MessageType = "geometry_msgs/Point";
    
        public IMessage Create() => new Point();
    
        public int GetLength() => 24;
    
        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            BuiltIns.DeserializeStruct(out this, ref ptr, end);
        }
    
        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            BuiltIns.SerializeStruct(this, ref ptr, end);
        }
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string Md5Sum = "4a842b65f413084dc2b10fb484ea7f17";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
                "H4sIAAAAAAAACj3HwQmAMAwF0HvAHT64gjiJC4SS0IAkxeSgTq+n3t5bcXRLtPBi80R1wYi0snCEgv+Z" +
                "F8yhlwhycBPSM7j2DffUM/XSQh9Q4wl6VgAAAA==";
                
    }
}
