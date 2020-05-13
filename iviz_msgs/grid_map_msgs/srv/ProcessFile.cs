using System.Runtime.Serialization;

namespace Iviz.Msgs.grid_map_msgs
{
    [DataContract]
    public sealed class ProcessFile : IService
    {
        /// <summary> Request message. </summary>
        [DataMember] public ProcessFileRequest Request { get; set; }
        
        /// <summary> Response message. </summary>
        [DataMember] public ProcessFileResponse Response { get; set; }
        
        /// <summary> Empty constructor. </summary>
        public ProcessFile()
        {
            Request = new ProcessFileRequest();
            Response = new ProcessFileResponse();
        }
        
        /// <summary> Setter constructor. </summary>
        public ProcessFile(ProcessFileRequest request)
        {
            Request = request;
            Response = new ProcessFileResponse();
        }
        
        IService IService.Create() => new ProcessFile();
        
        IRequest IService.Request
        {
            get => Request;
            set => Request = (ProcessFileRequest)value;
        }
        
        IResponse IService.Response
        {
            get => Response;
            set => Response = (ProcessFileResponse)value;
        }
        
        public string ErrorMessage { get; set; }
        
        string IService.RosType => RosServiceType;
        
        /// <summary> Full ROS name of this service. </summary>
        [Preserve] public const string RosServiceType = "grid_map_msgs/ProcessFile";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        [Preserve] public const string RosMd5Sum = "03f389710f49a6dd2a8b447bb2850cd6";
    }

    public sealed class ProcessFileRequest : IRequest
    {
        // Absolute file path.
        [DataMember] public string file_path { get; set; }
        
        // For ROS bags: topic name that should be processed (optional).
        [DataMember] public string topic_name { get; set; }
        
    
        /// <summary> Constructor for empty message. </summary>
        public ProcessFileRequest()
        {
            file_path = "";
            topic_name = "";
        }
        
        /// <summary> Explicit constructor. </summary>
        public ProcessFileRequest(string file_path, string topic_name)
        {
            this.file_path = file_path ?? throw new System.ArgumentNullException(nameof(file_path));
            this.topic_name = topic_name ?? throw new System.ArgumentNullException(nameof(topic_name));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ProcessFileRequest(Buffer b)
        {
            this.file_path = b.DeserializeString();
            this.topic_name = b.DeserializeString();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ProcessFileRequest(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.file_path);
            b.Serialize(this.topic_name);
        }
        
        public void Validate()
        {
            if (file_path is null) throw new System.NullReferenceException();
            if (topic_name is null) throw new System.NullReferenceException();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 8;
                size += BuiltIns.UTF8.GetByteCount(file_path);
                size += BuiltIns.UTF8.GetByteCount(topic_name);
                return size;
            }
        }
    }

    public sealed class ProcessFileResponse : IResponse
    {
        
        // True if file processing was successful.
        [DataMember] public bool success { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public ProcessFileResponse()
        {
        }
        
        /// <summary> Explicit constructor. </summary>
        public ProcessFileResponse(bool success)
        {
            this.success = success;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal ProcessFileResponse(Buffer b)
        {
            this.success = b.Deserialize<bool>();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new ProcessFileResponse(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.success);
        }
        
        public void Validate()
        {
        }
    
        public int RosMessageLength => 1;
    }
}
