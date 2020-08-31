using System.Runtime.Serialization;

namespace Iviz.Msgs.MeshMsgs
{
    [DataContract (Name = "mesh_msgs/GetTexture")]
    public sealed class GetTexture : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public GetTextureRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public GetTextureResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public GetTexture()
        {
            Request = new GetTextureRequest();
            Response = new GetTextureResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public GetTexture(GetTextureRequest request)
        {
            Request = request;
            Response = new GetTextureResponse();
        }
        
        IService IService.Create() => new GetTexture();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (GetTextureRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (GetTextureResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "mesh_msgs/GetTexture";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "48823554c65f6c317f12f79207ce78ac";
    }

    public sealed class GetTextureRequest : IRequest
    {
        [DataMember (Name = "uuid")] public string Uuid { get; set; }
        [DataMember (Name = "texture_index")] public uint TextureIndex { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public GetTextureRequest()
        {
            Uuid = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public GetTextureRequest(string Uuid, uint TextureIndex)
        {
            this.Uuid = Uuid;
            this.TextureIndex = TextureIndex;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal GetTextureRequest(Buffer b)
        {
            Uuid = b.DeserializeString();
            TextureIndex = b.Deserialize<uint>();
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new GetTextureRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(Uuid);
            b.Serialize(TextureIndex);
        }
        
        public void RosValidate()
        {
            if (Uuid is null) throw new System.NullReferenceException(nameof(Uuid));
        }
    
        public int RosMessageLength
        {
            get {
                int size = 8;
                size += BuiltIns.UTF8.GetByteCount(Uuid);
                return size;
            }
        }
    }

    public sealed class GetTextureResponse : IResponse
    {
        [DataMember (Name = "texture")] public MeshMsgs.MeshTexture Texture { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public GetTextureResponse()
        {
            Texture = new MeshMsgs.MeshTexture();
        }
        
        /// <summary> Explicit constructor. </summary>
        public GetTextureResponse(MeshMsgs.MeshTexture Texture)
        {
            this.Texture = Texture;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal GetTextureResponse(Buffer b)
        {
            Texture = new MeshMsgs.MeshTexture(b);
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new GetTextureResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            Texture.RosSerialize(b);
        }
        
        public void RosValidate()
        {
            if (Texture is null) throw new System.NullReferenceException(nameof(Texture));
            Texture.RosValidate();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 0;
                size += Texture.RosMessageLength;
                return size;
            }
        }
    }
}
