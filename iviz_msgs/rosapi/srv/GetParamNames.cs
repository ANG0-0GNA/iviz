using System.Runtime.Serialization;

namespace Iviz.Msgs.Rosapi
{
    [DataContract (Name = "rosapi/GetParamNames")]
    public sealed class GetParamNames : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public GetParamNamesRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public GetParamNamesResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public GetParamNames()
        {
            Request = new GetParamNamesRequest();
            Response = new GetParamNamesResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public GetParamNames(GetParamNamesRequest request)
        {
            Request = request;
            Response = new GetParamNamesResponse();
        }
        
        IService IService.Create() => new GetParamNames();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (GetParamNamesRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (GetParamNamesResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosapi/GetParamNames";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "dc7ae3609524b18034e49294a4ce670e";
    }

    public sealed class GetParamNamesRequest : Internal.EmptyRequest
    {
    }

    public sealed class GetParamNamesResponse : IResponse
    {
        [DataMember (Name = "names")] public string[] Names { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public GetParamNamesResponse()
        {
            Names = System.Array.Empty<string>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public GetParamNamesResponse(string[] Names)
        {
            this.Names = Names;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal GetParamNamesResponse(Buffer b)
        {
            Names = b.DeserializeStringArray();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new GetParamNamesResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.SerializeArray(Names, 0);
        }
        
        public void Validate()
        {
            if (Names is null) throw new System.NullReferenceException();
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] is null) throw new System.NullReferenceException();
            }
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += 4 * Names.Length;
                for (int i = 0; i < Names.Length; i++)
                {
                    size += BuiltIns.UTF8.GetByteCount(Names[i]);
                }
                return size;
            }
        }
    }
}
