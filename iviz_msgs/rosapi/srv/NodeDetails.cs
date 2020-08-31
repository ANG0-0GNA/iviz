using System.Runtime.Serialization;

namespace Iviz.Msgs.Rosapi
{
    [DataContract (Name = "rosapi/NodeDetails")]
    public sealed class NodeDetails : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public NodeDetailsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public NodeDetailsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public NodeDetails()
        {
            Request = new NodeDetailsRequest();
            Response = new NodeDetailsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public NodeDetails(NodeDetailsRequest request)
        {
            Request = request;
            Response = new NodeDetailsResponse();
        }
        
        IService IService.Create() => new NodeDetails();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (NodeDetailsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (NodeDetailsResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/NodeDetails";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "e1d0ced5ab8d5edb5fc09c98eb1d46f6";
    }

    public sealed class NodeDetailsRequest : IRequest
    {
        [DataMember (Name = "node")] public string Node { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public NodeDetailsRequest()
        {
            Node = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public NodeDetailsRequest(string Node)
        {
            this.Node = Node;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal NodeDetailsRequest(Buffer b)
        {
            Node = b.DeserializeString();
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new NodeDetailsRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(Node);
        }
        
        public void RosValidate()
        {
            if (Node is null) throw new System.NullReferenceException(nameof(Node));
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += BuiltIns.UTF8.GetByteCount(Node);
                return size;
            }
        }
    }

    public sealed class NodeDetailsResponse : IResponse
    {
        [DataMember (Name = "subscribing")] public string[] Subscribing { get; set; }
        [DataMember (Name = "publishing")] public string[] Publishing { get; set; }
        [DataMember (Name = "services")] public string[] Services { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public NodeDetailsResponse()
        {
            Subscribing = System.Array.Empty<string>();
            Publishing = System.Array.Empty<string>();
            Services = System.Array.Empty<string>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public NodeDetailsResponse(string[] Subscribing, string[] Publishing, string[] Services)
        {
            this.Subscribing = Subscribing;
            this.Publishing = Publishing;
            this.Services = Services;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal NodeDetailsResponse(Buffer b)
        {
            Subscribing = b.DeserializeStringArray();
            Publishing = b.DeserializeStringArray();
            Services = b.DeserializeStringArray();
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new NodeDetailsResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(Subscribing, 0);
            b.SerializeArray(Publishing, 0);
            b.SerializeArray(Services, 0);
        }
        
        public void RosValidate()
        {
            if (Subscribing is null) throw new System.NullReferenceException(nameof(Subscribing));
            for (int i = 0; i < Subscribing.Length; i++)
            {
                if (Subscribing[i] is null) throw new System.NullReferenceException($"{nameof(Subscribing)}[{i}]");
            }
            if (Publishing is null) throw new System.NullReferenceException(nameof(Publishing));
            for (int i = 0; i < Publishing.Length; i++)
            {
                if (Publishing[i] is null) throw new System.NullReferenceException($"{nameof(Publishing)}[{i}]");
            }
            if (Services is null) throw new System.NullReferenceException(nameof(Services));
            for (int i = 0; i < Services.Length; i++)
            {
                if (Services[i] is null) throw new System.NullReferenceException($"{nameof(Services)}[{i}]");
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 12;
                size += 4 * Subscribing.Length;
                for (int i = 0; i < Subscribing.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(Subscribing[i]);
                }
                size += 4 * Publishing.Length;
                for (int i = 0; i < Publishing.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(Publishing[i]);
                }
                size += 4 * Services.Length;
                for (int i = 0; i < Services.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(Services[i]);
                }
                return size;
            }
        }
    }
}
