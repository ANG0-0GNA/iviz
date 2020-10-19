using System.Runtime.Serialization;

namespace Iviz.Msgs.RosbridgeLibrary
{
    [DataContract (Name = "rosbridge_library/AddTwoInts")]
    public sealed class AddTwoInts : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public AddTwoIntsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public AddTwoIntsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public AddTwoInts()
        {
            Request = new AddTwoIntsRequest();
            Response = new AddTwoIntsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public AddTwoInts(AddTwoIntsRequest request)
        {
            Request = request;
            Response = new AddTwoIntsResponse();
        }
        
        IService IService.Create() => new AddTwoInts();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (AddTwoIntsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (AddTwoIntsResponse)value;
        }
        
        /// <summary>
        /// An error message in case the call fails.
        /// If the provider sets this to non-null, the ok byte is set to false, and the error message is sent instead of the response.
        /// </summary>
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosbridge_library/AddTwoInts";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "6a2e34150c00229791cc89ff309fff21";
    }

    public sealed class AddTwoIntsRequest : IRequest
    {
        [DataMember (Name = "a")] public long A { get; set; }
        [DataMember (Name = "b")] public long B { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public AddTwoIntsRequest()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public AddTwoIntsRequest(long A, long B)
        {
            this.A = A;
            this.B = B;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal AddTwoIntsRequest(ref Buffer b)
        {
            A = b.Deserialize<long>();
            B = b.Deserialize<long>();
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new AddTwoIntsRequest(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            b.Serialize(A);
            b.Serialize(B);
        }
        
        public void RosValidate()
        {
        }
    
        public int RosMessageLength => 16;
    }

    public sealed class AddTwoIntsResponse : IResponse
    {
        [DataMember (Name = "sum")] public long Sum { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public AddTwoIntsResponse()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public AddTwoIntsResponse(long Sum)
        {
            this.Sum = Sum;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal AddTwoIntsResponse(ref Buffer b)
        {
            Sum = b.Deserialize<long>();
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new AddTwoIntsResponse(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            b.Serialize(Sum);
        }
        
        public void RosValidate()
        {
        }
    
        public int RosMessageLength => 8;
    }
}
