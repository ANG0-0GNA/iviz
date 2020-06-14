using System.Runtime.Serialization;

namespace Iviz.Msgs.Rosapi
{
    [DataContract (Name = "rosapi/MessageDetails")]
    public sealed class MessageDetails : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public MessageDetailsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public MessageDetailsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public MessageDetails()
        {
            Request = new MessageDetailsRequest();
            Response = new MessageDetailsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public MessageDetails(MessageDetailsRequest request)
        {
            Request = request;
            Response = new MessageDetailsResponse();
        }
        
        IService IService.Create() => new MessageDetails();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (MessageDetailsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (MessageDetailsResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/MessageDetails";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "f9c88144f6f6bd888dd99d4e0411905d";
    }

    public sealed class MessageDetailsRequest : IRequest
    {
        [DataMember (Name = "type")] public string Type { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public MessageDetailsRequest()
        {
            Type = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public MessageDetailsRequest(string Type)
        {
            this.Type = Type;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal MessageDetailsRequest(Buffer b)
        {
            Type = b.DeserializeString();
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new MessageDetailsRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(Type);
        }
        
        public void RosValidate()
        {
            if (Type is null) throw new System.NullReferenceException();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(Type);
                return size;
            }
        }
    }

    public sealed class MessageDetailsResponse : IResponse
    {
        [DataMember (Name = "typedefs")] public TypeDef[] Typedefs { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public MessageDetailsResponse()
        {
            Typedefs = System.Array.Empty<TypeDef>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public MessageDetailsResponse(TypeDef[] Typedefs)
        {
            this.Typedefs = Typedefs;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal MessageDetailsResponse(Buffer b)
        {
            Typedefs = b.DeserializeArray<TypeDef>();
            for (int i = 0; i < this.Typedefs.Length; i++)
            {
                Typedefs[i] = new TypeDef(b);
            }
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new MessageDetailsResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(Typedefs, 0);
        }
        
        public void RosValidate()
        {
            if (Typedefs is null) throw new System.NullReferenceException();
            for (int i = 0; i < Typedefs.Length; i++)
            {
                if (Typedefs[i] is null) throw new System.NullReferenceException();
                Typedefs[i].RosValidate();
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                for (int i = 0; i < Typedefs.Length; i++)
                {
                    size += Typedefs[i].RosMessageLength;
                }
                return size;
            }
        }
    }
}
