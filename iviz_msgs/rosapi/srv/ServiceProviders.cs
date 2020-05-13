using System.Runtime.Serialization;

namespace Iviz.Msgs.rosapi
{
    [DataContract]
    public sealed class ServiceProviders : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public ServiceProvidersRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public ServiceProvidersResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public ServiceProviders()
        {
            Request = new ServiceProvidersRequest();
            Response = new ServiceProvidersResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public ServiceProviders(ServiceProvidersRequest request)
        {
            Request = request;
            Response = new ServiceProvidersResponse();
        }
        
        IService IService.Create() => new ServiceProviders();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (ServiceProvidersRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (ServiceProvidersResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/ServiceProviders";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "f30b41d5e347454ae5483ee95eef5cc6";
    }

    public sealed class ServiceProvidersRequest : IRequest
    {
        [DataMember] public string service { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ServiceProvidersRequest()
        {
            service = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public ServiceProvidersRequest(string service)
        {
            this.service = service ?? throw new System.ArgumentNullException(nameof(service));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ServiceProvidersRequest(Buffer b)
        {
            this.service = b.DeserializeString();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ServiceProvidersRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.service);
        }
        
        public void Validate()
        {
            if (service is null) throw new System.NullReferenceException();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(service);
                return size;
            }
        }
    }

    public sealed class ServiceProvidersResponse : IResponse
    {
        [DataMember] public string[] providers { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ServiceProvidersResponse()
        {
            providers = System.Array.Empty<string>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public ServiceProvidersResponse(string[] providers)
        {
            this.providers = providers ?? throw new System.ArgumentNullException(nameof(providers));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ServiceProvidersResponse(Buffer b)
        {
            this.providers = b.DeserializeStringArray();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ServiceProvidersResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(this.providers, 0);
        }
        
        public void Validate()
        {
            if (providers is null) throw new System.NullReferenceException();
            for (int i = 0; i < providers.Length; i++)
            {
                if (providers[i] is null) throw new System.NullReferenceException();
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += 4 * providers.Length;
                for (int i = 0; i < providers.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(providers[i]);
                }
                return size;
            }
        }
    }
}
