using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Iviz.Msgs.geometry_msgs
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Quaternion : IMessage
    {
        // This represents an orientation in free space in quaternion form.
        
        public double x { get; }
        public double y { get; }
        public double z { get; }
        public double w { get; }
    
        /// <summary> Explicit constructor. </summary>
        public Quaternion(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal Quaternion(Buffer b)
        {
            BuiltIns.DeserializeStruct(out this, b);
        }
        
        public IMessage Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new Quaternion(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            BuiltIns.SerializeStruct(this, b);
        }
        
        public void Validate()
        {
        }
    
        [IgnoreDataMember]
        public int RosMessageLength => 32;
    
        [IgnoreDataMember]
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve]
        public const string RosMessageType = "geometry_msgs/Quaternion";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve]
        public const string RosMd5Sum = "a779879fadf0160734f906b8c19c7004";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve]
        public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAEz3JTQqAQAhA4b2nENq3ik7SBSQcEkonNfo5fbWZ3fd4HU6LBDpX52DNQFI0l4+UYoqi" +
                "WJwZo9LMf+0HJbv+r5hvPUBZjXIc8Gq6m56mE+AFLI5yL20AAAA=";
                
    }
}
