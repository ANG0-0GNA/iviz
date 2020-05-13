using System.Runtime.Serialization;

namespace Iviz.Msgs.rosapi
{
    [DataContract]
    public sealed class ServiceRequestDetails : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public ServiceRequestDetailsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public ServiceRequestDetailsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public ServiceRequestDetails()
        {
            Request = new ServiceRequestDetailsRequest();
            Response = new ServiceRequestDetailsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public ServiceRequestDetails(ServiceRequestDetailsRequest request)
        {
            Request = request;
            Response = new ServiceRequestDetailsResponse();
        }
        
        IService IService.Create() => new ServiceRequestDetails();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (ServiceRequestDetailsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (ServiceRequestDetailsResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/ServiceRequestDetails";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "f9c88144f6f6bd888dd99d4e0411905d";
    }

    public sealed class ServiceRequestDetailsRequest : IRequest
    {
        [DataMember] public string type { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ServiceRequestDetailsRequest()
        {
            type = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public ServiceRequestDetailsRequest(string type)
        {
            this.type = type ?? throw new System.ArgumentNullException(nameof(type));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ServiceRequestDetailsRequest(Buffer b)
        {
            this.type = b.DeserializeString();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ServiceRequestDetailsRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.type);
        }
        
        public void Validate()
        {
            if (type is null) throw new System.NullReferenceException();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(type);
                return size;
            }
        }
    }

    public sealed class ServiceRequestDetailsResponse : IResponse
    {
        [DataMember] public TypeDef[] typedefs { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ServiceRequestDetailsResponse()
        {
            typedefs = System.Array.Empty<TypeDef>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public ServiceRequestDetailsResponse(TypeDef[] typedefs)
        {
            this.typedefs = typedefs ?? throw new System.ArgumentNullException(nameof(typedefs));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ServiceRequestDetailsResponse(Buffer b)
        {
            this.typedefs = b.DeserializeArray<TypeDef>();
            for (int i = 0; i < this.typedefs.Length; i++)
            {
                this.typedefs[i] = new TypeDef(b);
            }
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ServiceRequestDetailsResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(this.typedefs, 0);
        }
        
        public void Validate()
        {
            if (typedefs is null) throw new System.NullReferenceException();
            for (int i = 0; i < typedefs.Length; i++)
            {
                typedefs[i].Validate();
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                for (int i = 0; i < typedefs.Length; i++)
                {
                    size += typedefs[i].RosMessageLength;
                }
                return size;
            }
        }
    }
}
