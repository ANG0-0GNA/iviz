/* This file was created automatically, do not edit! */

using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Iviz.Msgs.GeometryMsgs
{
    [DataContract (Name = "geometry_msgs/Wrench")]
    [StructLayout(LayoutKind.Sequential)]
    public struct Wrench : IMessage, System.IEquatable<Wrench>, IDeserializable<Wrench>
    {
        // This represents force in free space, separated into
        // its linear and angular parts.
        [DataMember (Name = "force")] public Vector3 Force { get; set; }
        [DataMember (Name = "torque")] public Vector3 Torque { get; set; }
    
        /// <summary> Explicit constructor. </summary>
        public Wrench(in Vector3 Force, in Vector3 Torque)
        {
            this.Force = Force;
            this.Torque = Torque;
        }
        
        /// <summary> Constructor with buffer. </summary>
        public Wrench(ref Buffer b)
        {
            b.Deserialize(out this);
        }
        
        public readonly ISerializable RosDeserialize(ref Buffer b)
        {
            return new Wrench(ref b);
        }
        
        readonly Wrench IDeserializable<Wrench>.RosDeserialize(ref Buffer b)
        {
            return new Wrench(ref b);
        }
        
        public override readonly int GetHashCode() => (Force, Torque).GetHashCode();
        
        public override readonly bool Equals(object? o) => o is Wrench s && Equals(s);
        
        public readonly bool Equals(Wrench o) => (Force, Torque) == (o.Force, o.Torque);
        
        public static bool operator==(in Wrench a, in Wrench b) => a.Equals(b);
        
        public static bool operator!=(in Wrench a, in Wrench b) => !a.Equals(b);
    
        public readonly void RosSerialize(ref Buffer b)
        {
            b.Serialize(this);
        }
        
        public readonly void RosValidate()
        {
        }
    
        /// <summary> Constant size of this message. </summary>
        public const int RosFixedMessageLength = 48;
        
        public readonly int RosMessageLength => RosFixedMessageLength;
    
        public readonly string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "geometry_msgs/Wrench";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "4f539cf138b23283b520fd271b567936";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAACq1SwUrEMBC9F/oPD/aiUCqoeBA8yx4EQfEqs800G0yTmkxd69c72S6u69lCyyR57817" +
                "06zwvHUZicfEmYNk9DF1DBfQJ2bkkTpukHmkRMJGDyTW1QpOod4FpgQKRl87ea0VJrmtqxfuJKYrLHK/" +
                "1vp9n3Sjru7++amrh6f7W1iOA0uaX4ds88Whb3H8NyjhY394mrVFwa4FCo7BzxiYgqjtI1WZxiXluhha" +
                "leXEmlKn5AQmckaIUkQGelNRDpkLncZR1QiSKGRPhVy2lXPGrW0b7LYcFpQLVoFFwnLg5DokZ51ZqNpq" +
                "+GETDgEbSH+JnfN+cb10k61OeoUUZc84b7HuMccJu5JJiwRDop4iNmry4Iw2vjiODaZifdE4Hetj1Hug" +
                "o8mZbLksWZiM/va66n0kubnG57Gcj+VXXX0DuHC833ICAAA=";
                
    }
}
