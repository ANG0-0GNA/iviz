using System.Runtime.Serialization;

namespace Iviz.Msgs.tf2_msgs
{
    public sealed class FrameGraph : IService
    {
        /// <summary> Request message. </summary>
        public FrameGraphRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        public FrameGraphResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public FrameGraph()
        {
            Request = new FrameGraphRequest();
            Response = new FrameGraphResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public FrameGraph(FrameGraphRequest request)
        {
            Request = request;
            Response = new FrameGraphResponse();
        }
        
        public IService Create() => new FrameGraph();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (FrameGraphRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (FrameGraphResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        [IgnoreDataMember]
        public string RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve]
        public const string RosServiceType = "tf2_msgs/FrameGraph";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve]
        public const string RosMd5Sum = "437ea58e9463815a0d511c7326b686b0";
    }

    public sealed class FrameGraphRequest : Internal.EmptyRequest
    {
    }

    public sealed class FrameGraphResponse : IResponse
    {
        public string frame_yaml { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public FrameGraphResponse()
        {
            frame_yaml = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public FrameGraphResponse(string frame_yaml)
        {
            this.frame_yaml = frame_yaml ?? throw new System.ArgumentNullException(nameof(frame_yaml));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal FrameGraphResponse(Buffer b)
        {
            this.frame_yaml = BuiltIns.DeserializeString(b);
        }
        
        public IResponse Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new FrameGraphResponse(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            BuiltIns.Serialize(this.frame_yaml, b);
        }
        
        public void Validate()
        {
            if (frame_yaml is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(frame_yaml);
                return size;
            }
        }
    }
}
