using System.Runtime.Serialization;

namespace Iviz.Msgs.RosbridgeLibrary
{
    [DataContract (Name = "rosbridge_library/TestHeader")]
    public sealed class TestHeader : IMessage
    {
        [DataMember (Name = "header")] public StdMsgs.Header Header { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public TestHeader()
        {
            Header = new StdMsgs.Header();
        }
        
        /// <summary> Explicit constructor. </summary>
        public TestHeader(StdMsgs.Header Header)
        {
            this.Header = Header;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal TestHeader(Buffer b)
        {
            Header = new StdMsgs.Header(b);
        }
        
        public ISerializable RosDeserialize(Buffer b)
        {
            return new TestHeader(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        public void RosSerialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            Header.RosSerialize(b);
        }
        
        public void RosValidate()
        {
            if (Header is null) throw new System.NullReferenceException();
            Header.RosValidate();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 0;
                size += Header.RosMessageLength;
                return size;
            }
        }
    
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "rosbridge_library/TestHeader";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "d7be0bb39af8fb9129d5a76e6b63a290";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE62RQWscMQyF7/4Vgj0kKWwKzW2ht9Kmh0IhuS9aW5kReOyJpdl0/n2eZ2na3HroYDAe" +
                "v/c9WTJPx8kG+3gvnKTRuG3h83/+wo+Hbwey92FhRw/OJXFLNIlzYmd6qihCh1HaPstZMkw8zZJou/V1" +
                "FruF8XFUI6xBijTOeaXFIPJKsU7TUjSyC7lO8s4PpxZimrm5xiVzg762pKXLnxpP0ulYJs+LlCj0/csB" +
                "mmISF1cUtIIQm7BpGXBJYdHid5+6IeweX+oeRxnQyrdw8pG9Fyu/5ibW62Q7IOPD5XG3YKM5gpRkdL39" +
                "O+JoN4QQlCBzjSNdo/Kfq4+1ACh05qZ8ytLBER0A9aqbrm7+IpcNXbjU3/gL8U/Gv2DLG7e/aT9iZrm/" +
                "3pYBDYRwbvWsCdLTukFiVilOWU+N2xq66xIZdl97jyGCa5sIdjarUTGARC/qYzBvnb5N46gphFe/0Y83" +
                "pQIAAA==";
                
    }
}
