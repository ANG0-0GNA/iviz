namespace Iviz.Msgs.diagnostic_msgs
{
    public class SelfTest : IService
    {
        public sealed class Request : IRequest
        {
        
            public int GetLength() => 0;
        
            public unsafe void Deserialize(ref byte* ptr, byte* end)
            {
            }
        
            public unsafe void Serialize(ref byte* ptr, byte* end)
            {
            }
        
            public Response Call(IServiceCaller caller)
            {
                SelfTest s = new SelfTest(this);
                caller.Call(s);
                return s.response;
            }
        }

        public sealed class Response : IResponse
        {
            public string id;
            public byte passed;
            public DiagnosticStatus[] status;
        
            public int GetLength()
            {
                int size = 9;
                size += id.Length;
                for (int i = 0; i < status.Length; i++)
                {
                    size += status[i].GetLength();
                }
                return size;
            }
        
            /// <summary> Constructor for empty message. </summary>
            public Response()
            {
                id = "";
                status = System.Array.Empty<DiagnosticStatus>();
            }
            
            public unsafe void Deserialize(ref byte* ptr, byte* end)
            {
                BuiltIns.Deserialize(out id, ref ptr, end);
                BuiltIns.Deserialize(out passed, ref ptr, end);
                BuiltIns.DeserializeArray(out status, ref ptr, end, 0);
            }
        
            public unsafe void Serialize(ref byte* ptr, byte* end)
            {
                BuiltIns.Serialize(id, ref ptr, end);
                BuiltIns.Serialize(passed, ref ptr, end);
                BuiltIns.SerializeArray(status, ref ptr, end, 0);
            }
        }
        
        /// <summary> Full ROS name of this service. </summary>
        public const string ServiceType = "diagnostic_msgs/SelfTest";
        
        /// <summary> MD5 hash of a compact representation of the service. </summary>
        public const string Md5Sum = "ac21b1bab7ab17546986536c22eb34e9";
        
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
            "H4sIAAAAAAAACr1SS0vEQAy+D+x/CHhe18dN6GFB8eCTXdGDiKSduA22M3WSdtl/70wfugjexMIwSfN9" +
            "mS+P+XxuRAO7DbA1+U4JGhQha84ZN86LcrFW1FaeX0B6w8xM9sffzNysL8/Afj35WstGFj8lzMwBPJQs" +
            "UJMIbghKX1kBLWmUBv4N0AE7yx3bFisofN14R05TKAGDz70exkQmnnsvwnlFUFFHVU/3DQVU9k6Gbtxd" +
            "ZUeD9bRc3WbHg32xWt2tspPBWT8sry+yUzN4fSo4GO/9jECurZNNFjD3HcHUeoc1RQqCJSkCNz16FKwk" +
            "uvguI1Djg0bSxJ168Qt9nNkILjHYLQZ6ZdsTJh9axx9tQieYuaLdI1YtxZl36ZYEdoAh4C4lHn/GRfEF" +
            "9/VsWcv99/5vSSats6nGd9pFudsSFdRDhXkcg6at6VXHADnomLZ7PRwiqSGDFXkasHiHOKQAyjXFej4B" +
            "fTUV7isDAAA=";
            
        
        /// <summary> Request message. </summary>
        public readonly Request request;
        
        /// <summary> Response message. </summary>
        public Response response;
        
        /// <summary> Empty constructor. </summary>
        public SelfTest()
        {
            request = new Request();
        }
        
        /// <summary> Setter constructor. </summary>
        public SelfTest(Request request)
        {
            this.request = request;
        }
        
        public IResponse CreateResponse() => new Response();
        
        public IRequest GetRequest() => request;
        
        public void SetResponse(IResponse response)
        {
            this.response = (Response)response;
        }
    }

}
