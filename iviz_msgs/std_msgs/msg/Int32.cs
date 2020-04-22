
namespace Iviz.Msgs.std_msgs
{
    public sealed class Int32 : IMessage
    {
        public int data;
    
        /// <summary> Full ROS name of this message. </summary>
        public const string MessageType = "std_msgs/Int32";
    
        public IMessage Create() => new Int32();
    
        public int GetLength() => 4;
    
        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Deserialize(out data, ref ptr, end);
        }
    
        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Serialize(data, ref ptr, end);
        }
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string Md5Sum = "da5909fbe378aeaf85e547e830cc1bb7";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
                "H4sIAAAAAAAACsvMKzE2UkhJLEnk5QIAn4vD4wwAAAA=";
                
    }
}
