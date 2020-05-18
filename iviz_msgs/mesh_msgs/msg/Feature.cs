using System.Runtime.Serialization;

namespace Iviz.Msgs.MeshMsgs
{
    [DataContract (Name = "mesh_msgs/Feature")]
    public sealed class Feature : IMessage
    {
        [DataMember (Name = "location")] public GeometryMsgs.Point Location { get; set; }
        [DataMember (Name = "descriptor")] public StdMsgs.Float32[] Descriptor { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public Feature()
        {
            Descriptor = System.Array.Empty<StdMsgs.Float32>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public Feature(GeometryMsgs.Point Location, StdMsgs.Float32[] Descriptor)
        {
            this.Location = Location;
            this.Descriptor = Descriptor;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal Feature(Buffer b)
        {
            Location = new GeometryMsgs.Point(b);
            Descriptor = b.DeserializeArray<StdMsgs.Float32>();
            for (int i = 0; i < this.Descriptor.Length; i++)
            {
                Descriptor[i] = new StdMsgs.Float32(b);
            }
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new Feature(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(Location);
            b.SerializeArray(Descriptor, 0);
        }
        
        public void Validate()
        {
            if (Descriptor is null) throw new System.NullReferenceException();
            for (int i = 0; i < Descriptor.Length; i++)
            {
                if (Descriptor[i] is null) throw new System.NullReferenceException();
                Descriptor[i].Validate();
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 28;
                size += 4 * Descriptor.Length;
                return size;
            }
        }
    
        string IMessage.RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "mesh_msgs/Feature";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "ac711cf3ef6eb8582240a7afe5b9a573";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE7XQsQrCQAwG4D1PEfABhFYcBFedBMFuIhKuaRtoL8clg/XptRQ66KqZ/vxLPtKyDux5" +
                "vA/W2vqsEh17DeSiEczruT/0Sl4W1xvWbCFLcs0A+x8PnC7HHbbfIlhh1Ylh0Ogk0dA7xqQmkxK1QXpv" +
                "k1wiNpkZLVFgaCb1doOPJY1Lev6L//mz+WJZYE1O8AKMerHtbwEAAA==";
                
    }
}
