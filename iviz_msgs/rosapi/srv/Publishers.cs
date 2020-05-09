using System.Runtime.Serialization;

namespace Iviz.Msgs.rosapi
{
    public sealed class Publishers : IService
    {
        /// <summary> Request message. </summary>
        public PublishersRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        public PublishersResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public Publishers()
        {
            Request = new PublishersRequest();
            Response = new PublishersResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public Publishers(PublishersRequest request)
        {
            Request = request;
            Response = new PublishersResponse();
        }
        
        public IService Create() => new Publishers();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (PublishersRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (PublishersResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        [IgnoreDataMember]
        public string RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve]
        public const string RosServiceType = "rosapi/Publishers";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve]
        public const string RosMd5Sum = "cb37f09944e7ba1fc08ee38f7a94291d";
    }

    public sealed class PublishersRequest : IRequest
    {
        public string topic { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public PublishersRequest()
        {
            topic = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public PublishersRequest(string topic)
        {
            this.topic = topic ?? throw new System.ArgumentNullException(nameof(topic));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal PublishersRequest(Buffer b)
        {
            this.topic = BuiltIns.DeserializeString(b);
        }
        
        public IRequest Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new PublishersRequest(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            BuiltIns.Serialize(this.topic, b);
        }
        
        public void Validate()
        {
            if (topic is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(topic);
                return size;
            }
        }
    }

    public sealed class PublishersResponse : IResponse
    {
        public string[] publishers { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public PublishersResponse()
        {
            publishers = System.Array.Empty<string>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public PublishersResponse(string[] publishers)
        {
            this.publishers = publishers ?? throw new System.ArgumentNullException(nameof(publishers));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal PublishersResponse(Buffer b)
        {
            this.publishers = BuiltIns.DeserializeStringArray(b, 0);
        }
        
        public IResponse Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new PublishersResponse(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            BuiltIns.Serialize(this.publishers, b, 0);
        }
        
        public void Validate()
        {
            if (publishers is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += 4 * publishers.Length;
                for (int i = 0; i < publishers.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(publishers[i]);
                }
                return size;
            }
        }
    }
}
