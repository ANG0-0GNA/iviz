using System.Runtime.Serialization;

namespace Iviz.Msgs.RosbridgeLibrary
{
    [DataContract (Name = "rosbridge_library/TestNestedService")]
    public sealed class TestNestedService : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public TestNestedServiceRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public TestNestedServiceResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public TestNestedService()
        {
            Request = new TestNestedServiceRequest();
            Response = new TestNestedServiceResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public TestNestedService(TestNestedServiceRequest request)
        {
            Request = request;
            Response = new TestNestedServiceResponse();
        }
        
        IService IService.Create() => new TestNestedService();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (TestNestedServiceRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (TestNestedServiceResponse)value;
        }
        
        /// <summary>
        /// An error message in case the call fails.
        /// If the provider sets this to non-null, the ok byte is set to false, and the error message is sent instead of the response.
        /// </summary>
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosbridge_library/TestNestedService";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "063d2b71e58b5225a457d4ee09dab6f6";
    }

    public sealed class TestNestedServiceRequest : IRequest
    {
        //request definition
        [DataMember (Name = "pose")] public GeometryMsgs.Pose Pose { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TestNestedServiceRequest()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public TestNestedServiceRequest(in GeometryMsgs.Pose Pose)
        {
            this.Pose = Pose;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TestNestedServiceRequest(ref Buffer b)
        {
            Pose = new GeometryMsgs.Pose(ref b);
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new TestNestedServiceRequest(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            Pose.RosSerialize(ref b);
        }
        
        public void RosValidate()
        {
        }
    
        public int RosMessageLength => 56;
    }

    public sealed class TestNestedServiceResponse : IResponse
    {
        //response definition
        [DataMember (Name = "data")] public StdMsgs.Float64 Data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TestNestedServiceResponse()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public TestNestedServiceResponse(in StdMsgs.Float64 Data)
        {
            this.Data = Data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TestNestedServiceResponse(ref Buffer b)
        {
            Data = new StdMsgs.Float64(ref b);
        }
        
        public ISerializable RosDeserialize(ref Buffer b)
        {
            return new TestNestedServiceResponse(ref b);
        }
    
        public void RosSerialize(ref Buffer b)
        {
            Data.RosSerialize(ref b);
        }
        
        public void RosValidate()
        {
        }
    
        public int RosMessageLength => 8;
    }
}
