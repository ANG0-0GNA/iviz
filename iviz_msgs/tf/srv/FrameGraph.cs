using System.Runtime.Serialization;

namespace Iviz.Msgs.Tf
{
    [DataContract (Name = "tf/FrameGraph")]
    public sealed class FrameGraph : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public FrameGraphRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public FrameGraphResponse Response { get; set; }
        
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
        
        IService IService.Create() => new FrameGraph();
        
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
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "tf/FrameGraph";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "c4af9ac907e58e906eb0b6e3c58478c0";
    }

    [DataContract]
    public sealed class FrameGraphRequest : Internal.EmptyRequest
    {
    }

    [DataContract]
    public sealed class FrameGraphResponse : IResponse, IDeserializable<FrameGraphResponse>
    {
        [DataMember (Name = "dot_graph")] public string DotGraph { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public FrameGraphResponse()
        {
            DotGraph = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public FrameGraphResponse(string DotGraph)
        {
            this.DotGraph = DotGraph;
        }
        
        /// <summary> Constructor with buffer. </summary>
        public FrameGraphResponse(ref Buffer b)
        {
            DotGraph = b.DeserializeString();
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new FrameGraphResponse(ref b);
        }
        
        FrameGraphResponse IDeserializable<FrameGraphResponse>.RosDeserialize(ref Buffer b)
        {
            return new FrameGraphResponse(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            b.Serialize(DotGraph);
        }
        
        public void RosValidate()
        {
            if (DotGraph is null) throw new System.NullReferenceException(nameof(DotGraph));
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(DotGraph);
                return size;
            }
        }
    }
}
