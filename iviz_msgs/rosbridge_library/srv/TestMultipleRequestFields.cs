using System.Runtime.Serialization;

namespace Iviz.Msgs.rosbridge_library
{
    public sealed class TestMultipleRequestFields : IService
    {
        /// <summary> Request message. </summary>
        public TestMultipleRequestFieldsRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        public TestMultipleRequestFieldsResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public TestMultipleRequestFields()
        {
            Request = new TestMultipleRequestFieldsRequest();
            Response = new TestMultipleRequestFieldsResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public TestMultipleRequestFields(TestMultipleRequestFieldsRequest request)
        {
            Request = request;
            Response = new TestMultipleRequestFieldsResponse();
        }
        
        public IService Create() => new TestMultipleRequestFields();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (TestMultipleRequestFieldsRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (TestMultipleRequestFieldsResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        [IgnoreDataMember]
        public string RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve]
        public const string RosServiceType = "rosbridge_library/TestMultipleRequestFields";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve]
        public const string RosMd5Sum = "6cce9fb727dd0f31d504d7d198a1f4ef";
    }

    public sealed class TestMultipleRequestFieldsRequest : IRequest
    {
        public int @int { get; set; }
        public float @float { get; set; }
        public string @string { get; set; }
        public bool @bool { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TestMultipleRequestFieldsRequest()
        {
            @string = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public TestMultipleRequestFieldsRequest(int @int, float @float, string @string, bool @bool)
        {
            this.@int = @int;
            this.@float = @float;
            this.@string = @string ?? throw new System.ArgumentNullException(nameof(@string));
            this.@bool = @bool;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TestMultipleRequestFieldsRequest(Buffer b)
        {
            this.@int = b.Deserialize<int>();
            this.@float = b.Deserialize<float>();
            this.@string = b.DeserializeString();
            this.@bool = b.Deserialize<bool>();
        }
        
        public IRequest Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new TestMultipleRequestFieldsRequest(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.@int);
            b.Serialize(this.@float);
            b.Serialize(this.@string);
            b.Serialize(this.@bool);
        }
        
        public void Validate()
        {
            if (@string is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 13;
                size += BuiltIns.UTF8.GetByteCount(@string);
                return size;
            }
        }
    }

    public sealed class TestMultipleRequestFieldsResponse : Internal.EmptyResponse
    {
    }
}
