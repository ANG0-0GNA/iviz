using System.Runtime.Serialization;

namespace Iviz.Msgs.rosapi
{
    public sealed class Topics : IService
    {
        /// <summary> Request message. </summary>
        public TopicsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        public TopicsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public Topics()
        {
            Request = new TopicsRequest();
            Response = new TopicsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public Topics(TopicsRequest request)
        {
            Request = request;
            Response = new TopicsResponse();
        }
        
        public IService Create() => new Topics();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (TopicsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (TopicsResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        [IgnoreDataMember]
        public string RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve]
        public const string RosServiceType = "rosapi/Topics";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve]
        public const string RosMd5Sum = "d966d98fc333fa1f3135af765eac1ba8";
    }

    public sealed class TopicsRequest : IRequest
    {
        
    
        /// <summary> Constructor for empty message. </summary>
        public TopicsRequest()
        {
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TopicsRequest(Buffer b)
        {
        }
        
        public IRequest Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new TopicsRequest(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
        }
        
        public void Validate()
        {
        }
    
        [IgnoreDataMember]
        public int RosMessageLength => 0;
    }

    public sealed class TopicsResponse : IResponse
    {
        public string[] topics { get; set; }
        public string[] types { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TopicsResponse()
        {
            topics = System.Array.Empty<string>();
            types = System.Array.Empty<string>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public TopicsResponse(string[] topics, string[] types)
        {
            this.topics = topics ?? throw new System.ArgumentNullException(nameof(topics));
            this.types = types ?? throw new System.ArgumentNullException(nameof(types));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TopicsResponse(Buffer b)
        {
            this.topics = b.DeserializeStringArray(0);
            this.types = b.DeserializeStringArray(0);
        }
        
        public IResponse Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new TopicsResponse(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(this.topics, 0);
            b.SerializeArray(this.types, 0);
        }
        
        public void Validate()
        {
            if (topics is null) throw new System.NullReferenceException();
            if (types is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 8;
                size += 4 * topics.Length;
                for (int i = 0; i < topics.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(topics[i]);
                }
                size += 4 * types.Length;
                for (int i = 0; i < types.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(types[i]);
                }
                return size;
            }
        }
    }
}
