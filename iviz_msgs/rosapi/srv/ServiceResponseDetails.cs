using System.Runtime.Serialization;

namespace Iviz.Msgs.Rosapi
{
    [DataContract (Name = "rosapi/ServiceResponseDetails")]
    public sealed class ServiceResponseDetails : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public ServiceResponseDetailsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public ServiceResponseDetailsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public ServiceResponseDetails()
        {
            Request = new ServiceResponseDetailsRequest();
            Response = new ServiceResponseDetailsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public ServiceResponseDetails(ServiceResponseDetailsRequest request)
        {
            Request = request;
            Response = new ServiceResponseDetailsResponse();
        }
        
        IService IService.Create() => new ServiceResponseDetails();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (ServiceResponseDetailsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (ServiceResponseDetailsResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/ServiceResponseDetails";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "f9c88144f6f6bd888dd99d4e0411905d";
    }

    public sealed class ServiceResponseDetailsRequest : IRequest
    {
        [DataMember (Name = "type")] public string Type { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ServiceResponseDetailsRequest()
        {
            Type = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public ServiceResponseDetailsRequest(string Type)
        {
            this.Type = Type;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ServiceResponseDetailsRequest(Buffer b)
        {
            Type = b.DeserializeString();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ServiceResponseDetailsRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.Type);
        }
        
        public void Validate()
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

    public sealed class ServiceResponseDetailsResponse : IResponse
    {
        [DataMember (Name = "typedefs")] public TypeDef[] Typedefs { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ServiceResponseDetailsResponse()
        {
            Typedefs = System.Array.Empty<TypeDef>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public ServiceResponseDetailsResponse(TypeDef[] Typedefs)
        {
            this.Typedefs = Typedefs;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ServiceResponseDetailsResponse(Buffer b)
        {
            Typedefs = b.DeserializeArray<TypeDef>();
            for (int i = 0; i < this.Typedefs.Length; i++)
            {
                Typedefs[i] = new TypeDef(b);
            }
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ServiceResponseDetailsResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(Typedefs, 0);
        }
        
        public void Validate()
        {
            if (Typedefs is null) throw new System.NullReferenceException();
            for (int i = 0; i < Typedefs.Length; i++)
            {
                if (Typedefs[i] is null) throw new System.NullReferenceException();
                Typedefs[i].Validate();
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
