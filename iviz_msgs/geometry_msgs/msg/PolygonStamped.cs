using System.Runtime.Serialization;

namespace Iviz.Msgs.geometry_msgs
{
    [DataContract]
    public sealed class PolygonStamped : IMessage
    {
        // This represents a Polygon with reference coordinate frame and timestamp
        [DataMember] public std_msgs.Header header { get; set; }
        [DataMember] public Polygon polygon { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public PolygonStamped()
        {
            header = new std_msgs.Header();
            polygon = new Polygon();
        }
        
        /// <summary> Explicit constructor. </summary>
        public PolygonStamped(std_msgs.Header header, Polygon polygon)
        {
            this.header = header ?? throw new System.ArgumentNullException(nameof(header));
            this.polygon = polygon ?? throw new System.ArgumentNullException(nameof(polygon));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal PolygonStamped(Buffer b)
        {
            this.header = new std_msgs.Header(b);
            this.polygon = new Polygon(b);
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new PolygonStamped(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.header);
            b.Serialize(this.polygon);
        }
        
        public void Validate()
        {
            if (header is null) throw new System.NullReferenceException();
            header.Validate();
            if (polygon is null) throw new System.NullReferenceException();
            polygon.Validate();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 0;
                size += header.RosMessageLength;
                size += polygon.RosMessageLength;
                return size;
            }
        }
    
        string IMessage.RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "geometry_msgs/PolygonStamped";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "c6be8f7dc3bee7fe9e8d296070f53340";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE71UTW/UMBC9+1eMtIe2iC0S3CpxQCCgB6RK7Q2hymtPkhGOHWxn2/DreZ7sLpV64QCs" +
                "VsqHZ97Mm3kvG7obpFDmKXPhWAtZuklh6VOkB6kDTjrOHB2TSyl7ibYyddmOTDZ6qjJyqXaczGe2njMN" +
                "ejFHjGm9GvP2L//Ml9tPV1Sqvx9LX16t1c2GbivastnTyNV6Wy11CV1JP3DeBt5zIO2XPelpXSYul0jU" +
                "OeDfc+RsQ1hoLgiqCcTHcY7iGvMT32M+MiViaJPNVdwcbH42qIaOf+Efsw7y+sMVYmJhN1dBQwsQXGZb" +
                "JPY4JDNLrG9etwSzuXtIWzxyj9meilMdbG3N8mPbW+vTlivUeLGSuwQ2hsOo4gud67t7PJYLQhG0wFNy" +
                "A52j85ulDthTHZj2NovdBW7ADhMA6llLOrt4ghwVOtqYjvAr4u8afwIbT7iN03bAzkJjX+YeA0TglNNe" +
                "PEJ3i4K4IFAnBdllmxfTstaSZvNRxVjb+nQjuNpSkhMswKuITam5oes27sX/KzX2nKC6vKySPFjAbN5R" +
                "mdhJ1xQkGErqml6OJoMuWSl2kktVUwWLmymJ2hGnoDOPqxZ3zYcxsgM3mEyF8vXbIfh/8dKqR8ugnWol" +
                "FuUwpSJPOSKy2aPLjHVN1vG5flQg7p2AHKKgXycFKRfNJteqaryC5divlGFD0prrrPYwAsoUUUnFUuH8" +
                "BnRo65Jotdvhu6ZI+CJoV3gDQGhrTLUlV85pgtt3EqQumnrMhNOK7VW0nov0cW2m2u9M80QBxyuj1lWE" +
                "1yIs3yM7pAOxw/4qJfjjJZbYJqFatmCkA9Ke34c0+1bbdCHZZvzH091yuvtpfgHG0nZTqwUAAA==";
                
    }
}
