/* This file was created automatically, do not edit! */

using System.Runtime.Serialization;

namespace Iviz.Msgs.IvizMsgs
{
    [DataContract (Name = "iviz_msgs/Texture")]
    public sealed class Texture : IDeserializable<Texture>, IMessage
    {
        [DataMember (Name = "path")] public string Path { get; set; }
        [DataMember (Name = "width")] public uint Width { get; set; }
        [DataMember (Name = "height")] public uint Height { get; set; }
        [DataMember (Name = "bpp")] public uint Bpp { get; set; }
        [DataMember (Name = "data")] public byte[] Data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public Texture()
        {
            Path = "";
            Data = System.Array.Empty<byte>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public Texture(string Path, uint Width, uint Height, uint Bpp, byte[] Data)
        {
            this.Path = Path;
            this.Width = Width;
            this.Height = Height;
            this.Bpp = Bpp;
            this.Data = Data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        public Texture(ref Buffer b)
        {
            Path = b.DeserializeString();
            Width = b.Deserialize<uint>();
            Height = b.Deserialize<uint>();
            Bpp = b.Deserialize<uint>();
            Data = b.DeserializeStructArray<byte>();
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new Texture(ref b);
        }
        
        Texture IDeserializable<Texture>.RosDeserialize(ref Buffer b)
        {
            return new Texture(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            b.Serialize(Path);
            b.Serialize(Width);
            b.Serialize(Height);
            b.Serialize(Bpp);
            b.SerializeStructArray(Data, 0);
        }
        
        public void RosValidate()
        {
            if (Path is null) throw new System.NullReferenceException(nameof(Path));
            if (Data is null) throw new System.NullReferenceException(nameof(Data));
        }
    
        public int RosMessageLength
        {
            get {
                int size = 20;
                size += BuiltIns.UTF8.GetByteCount(Path);
                size += 1 * Data.Length;
                return size;
            }
        }
    
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "iviz_msgs/Texture";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "15648750e71eb8bc15207f2746db0398";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAACisuKcrMS1coSCzJ4OUqzcwrMTZSKM9MQeJlpGamZ5TAuUkFBRC2RXSsQkpiSSIvFy8X" +
                "AFE01cxGAAAA";
                
    }
}
