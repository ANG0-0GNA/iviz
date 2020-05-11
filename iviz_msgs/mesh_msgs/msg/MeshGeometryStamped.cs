using System.Runtime.Serialization;

namespace Iviz.Msgs.mesh_msgs
{
    public sealed class MeshGeometryStamped : IMessage
    {
        // Mesh Geometry Message
        public std_msgs.Header header { get; set; }
        public string uuid { get; set; }
        public mesh_msgs.MeshGeometry mesh_geometry { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public MeshGeometryStamped()
        {
            header = new std_msgs.Header();
            uuid = "";
            mesh_geometry = new mesh_msgs.MeshGeometry();
        }
        
        /// <summary> Explicit constructor. </summary>
        public MeshGeometryStamped(std_msgs.Header header, string uuid, mesh_msgs.MeshGeometry mesh_geometry)
        {
            this.header = header ?? throw new System.ArgumentNullException(nameof(header));
            this.uuid = uuid ?? throw new System.ArgumentNullException(nameof(uuid));
            this.mesh_geometry = mesh_geometry ?? throw new System.ArgumentNullException(nameof(mesh_geometry));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal MeshGeometryStamped(Buffer b)
        {
            this.header = new std_msgs.Header(b);
            this.uuid = b.DeserializeString();
            this.mesh_geometry = new mesh_msgs.MeshGeometry(b);
        }
        
        public IMessage Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new MeshGeometryStamped(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            this.header.Serialize(b);
            b.Serialize(this.uuid);
            this.mesh_geometry.Serialize(b);
        }
        
        public void Validate()
        {
            if (header is null) throw new System.NullReferenceException();
            if (uuid is null) throw new System.NullReferenceException();
            if (mesh_geometry is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += header.RosMessageLength;
                size += BuiltIns.UTF8.GetByteCount(uuid);
                size += mesh_geometry.RosMessageLength;
                return size;
            }
        }
    
        [IgnoreDataMember]
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve]
        public const string RosMessageType = "mesh_msgs/MeshGeometryStamped";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve]
        public const string RosMd5Sum = "2d62dc21b3d9b8f528e4ee7f76a77fb7";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve]
        public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE7VTTWvbQBC9768Y8MFJwSk0pQdDb6apD4FAfAvFjLUjaWG1q+6uHKu/vm8lWzHBhB5q" +
                "IZBWeu/Nx5uZ0aPEmh7EN5JCn0+RK1Ex6W0Tq/j5p7CWQPXwwOdgXEVdZ7RqQBwxWWJSGD5Xx5NS3//z" +
                "pR6fH5b0Lj01o+fETnPQiJ9Yc2IqPdI2VS1hYWUvFiRuWtE0/E19K/EOxE1tIuGuxElga3vqIkDJU+Gb" +
                "pnOm4CSUDOo654NpHDG1HJIpOssBeB+0cRleBm4kq+OO8rsTVwitV0tgXJSiSwYJ9VAognDMLV2vSHXG" +
                "pfsvmaBmm1e/wFEqNH8KTqnmlJOVQxvgFJLhuESMT2Nxd9BGcwRRdKSb4dsWx3hLCIIUpPVFTTfI/KlP" +
                "tXcQFNpzMLyzkoULdACq80ya354pu0HasfMn+VHxLca/yLpJN9e0qOGZzdXHrkIDAWyD3xsN6K4fRApr" +
                "xCWyZhcY45RZY0g1+5F7DBBYgyN4coy+MDBA06tJ9WlcBze2GNkrTePlTUCRl3frtBwj5cnD5pdftJc8" +
                "SBI/+C2HrfOhYRvPlm+DLrvKytrpTAe05CxzpVovZHdaIoxFYuPiYFzro0kGo+DLvCUZlxemDAIDW2So" +
                "Sus5fftKh+mtn97+XN+qd31DESspjTtLOh0R8/hmzriiL/eTIeZIV38BEc3WNkoFAAA=";
                
    }
}
