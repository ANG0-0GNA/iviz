using System.Runtime.Serialization;

namespace Iviz.Msgs.Rosapi
{
    [DataContract (Name = "rosapi/Publishers")]
    public sealed class Publishers : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public PublishersRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public PublishersResponse Response { get; set; }
        
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
        
        IService IService.Create() => new Publishers();
        
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
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/Publishers";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "cb37f09944e7ba1fc08ee38f7a94291d";
    }

    public sealed class PublishersRequest : IRequest
    {
        [DataMember (Name = "topic")] public string Topic { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public PublishersRequest()
        {
            Topic = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public PublishersRequest(string Topic)
        {
            this.Topic = Topic;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal PublishersRequest(Buffer b)
        {
            Topic = b.DeserializeString();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new PublishersRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.Topic);
        }
        
        public void Validate()
        {
            if (Topic is null) throw new System.NullReferenceException();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(Topic);
                return size;
            }
        }
    }

    public sealed class PublishersResponse : IResponse
    {
        [DataMember (Name = "publishers")] public string[] Publishers_ { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public PublishersResponse()
        {
            Publishers_ = System.Array.Empty<string>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public PublishersResponse(string[] Publishers_)
        {
            this.Publishers_ = Publishers_;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal PublishersResponse(Buffer b)
        {
            Publishers_ = b.DeserializeStringArray();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new PublishersResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(Publishers_, 0);
        }
        
        public void Validate()
        {
            if (Publishers_ is null) throw new System.NullReferenceException();
            for (int i = 0; i < Publishers_.Length; i++)
            {
                if (Publishers_[i] is null) throw new System.NullReferenceException();
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += 4 * Publishers_.Length;
                for (int i = 0; i < Publishers_.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(Publishers_[i]);
                }
                return size;
            }
        }
    }
}
