using System.Runtime.Serialization;

namespace Iviz.Msgs.rosbridge_library
{
    [DataContract]
    public sealed class TestRequestAndResponse : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public TestRequestAndResponseRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public TestRequestAndResponseResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public TestRequestAndResponse()
        {
            Request = new TestRequestAndResponseRequest();
            Response = new TestRequestAndResponseResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public TestRequestAndResponse(TestRequestAndResponseRequest request)
        {
            Request = request;
            Response = new TestRequestAndResponseResponse();
        }
        
        IService IService.Create() => new TestRequestAndResponse();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (TestRequestAndResponseRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (TestRequestAndResponseResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "rosbridge_library/TestRequestAndResponse";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "491d316f183df11876531749005b31d1";
    }

    public sealed class TestRequestAndResponseRequest : IRequest
    {
        [DataMember] public int data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TestRequestAndResponseRequest()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public TestRequestAndResponseRequest(int data)
        {
            this.data = data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TestRequestAndResponseRequest(Buffer b)
        {
            this.data = b.Deserialize<int>();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new TestRequestAndResponseRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.data);
        }
        
        public void Validate()
        {
        }
    
        public int RosMessageLength => 4;
    }

    public sealed class TestRequestAndResponseResponse : IResponse
    {
        [DataMember] public int data { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TestRequestAndResponseResponse()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public TestRequestAndResponseResponse(int data)
        {
            this.data = data;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TestRequestAndResponseResponse(Buffer b)
        {
            this.data = b.Deserialize<int>();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new TestRequestAndResponseResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.data);
        }
        
        public void Validate()
        {
        }
    
        public int RosMessageLength => 4;
    }
}
