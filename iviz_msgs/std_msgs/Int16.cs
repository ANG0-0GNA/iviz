
namespace Iviz.Msgs.std_msgs
{
    public sealed class Int16 : IMessage 
    {
        public short data;

        /// <summary> Full ROS name of this message. </summary>
        public const string MessageType = "std_msgs/Int16";

        public IMessage Create() => new Int16();

        public int GetLength() => 2;

        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Deserialize(out data, ref ptr, end);
        }

        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Serialize(data, ref ptr, end);
        }

        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string Md5Sum = "8524586e34fbd7cb1c08c5f5f1ca0e57";

        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
            "H4sIAAAAAAAAE8vMKzE0U0hJLEnk4gIAJDs+BgwAAAA=";

    }
}
