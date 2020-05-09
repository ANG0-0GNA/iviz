using System.Runtime.Serialization;

namespace Iviz.Msgs.std_msgs
{
    public sealed class UInt32MultiArray : IMessage
    {
        // Please look at the MultiArrayLayout message definition for
        // documentation on all multiarrays.
        
        public MultiArrayLayout layout { get; set; } // specification of data layout
        public uint[] data { get; set; } // array of data
        
    
        /// <summary> Constructor for empty message. </summary>
        public UInt32MultiArray()
        {
            layout = new MultiArrayLayout();
            data = System.Array.Empty<uint>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public UInt32MultiArray(MultiArrayLayout layout, uint[] data)
        {
            this.layout = layout ?? throw new System.ArgumentNullException(nameof(layout));
            this.data = data ?? throw new System.ArgumentNullException(nameof(data));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal UInt32MultiArray(Buffer b)
        {
            this.layout = new MultiArrayLayout(b);
            this.data = BuiltIns.DeserializeStructArray<uint>(b, 0);
        }
        
        public IMessage Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new UInt32MultiArray(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            this.layout.Serialize(b);
            BuiltIns.Serialize(this.data, b, 0);
        }
        
        public void Validate()
        {
            if (layout is null) throw new System.NullReferenceException();
            if (data is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += layout.RosMessageLength;
                size += 4 * data.Length;
                return size;
            }
        }
    
        [IgnoreDataMember]
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve]
        public const string RosMessageType = "std_msgs/UInt32MultiArray";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve]
        public const string RosMd5Sum = "4d6a180abc9be191b96a7eda6c8a233d";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve]
        public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE71U32vbMBB+919xJC9tlmb5Ucpa6ENgsJcWBh2MEkJQrXOsRJaCJDfr/vp9sh3bafc4" +
                "ZgyW73R33/fpTkP6rll4Jm3tnkSgkDM9ljqopXPi7UG82TJQwd6LLZPkTBkVlDWUWZcMSdq0LNgEUdnw" +
                "Cq2piOEihvtJknxIRrr51s+Q/IFTlam0SZKRFEE0u5JSmbCYr9bUPpWXuvCq0iksSZL7f/wkj0/f7sgH" +
                "uSn81n9+zwcq/IBmHWmolGrh2JOgLRt2Kq29V1JBKw+SQneoBRIchAsqLRFVswtvB54QfT3tRyrHZJ1k" +
                "x5IyZwtCZXZUWB8BBEvKmOb/TPM2BRREeci1bOU6uejg7IGBgH0jd4ViY7PMc++cDkJKZbbEmuOZ+9gu" +
                "wGJCJz7SpymaxTpPPrellrR8+Ll8fqIXpqNTIbABVAL2wp+D8MEpycggjDy1BMhWPK8ir97eTLnIc0h4" +
                "O+Ev1Hg33l/SfQVm1efwKQZv6hKr2Xqkzi3z9WgHy36dDCMFYAEI4eSYFldpLiCtppvr6a/rL1NSRZyE" +
                "owo5iAAbxucVOFOrraNms0eWY8UetDsuwt9VBVB5NV1PtHhBXsAd5Ky2eRh0Lq9+M0UXKvasFVpYFyOg" +
                "GUU093Q7n91Mp0QXxgZudjZikvK0K6FclQ5qV9gvm4SzPoKjkiEfdJ4WAAr1rGcA8J3dzk/ueT9do8Og" +
                "87UJFz1bm66S5eNJOs4YnYT2jtdSlNzZ45h2WEDvsjDjqlv28b+uOPmP89/OVhKJYDAa/hiVegXFt+oV" +
                "Hd927mm+GjXi5dcczbuNdBGnBNcAlbhw/WUbWEsWA+vVX2r8AfgORpHUBQAA";
                
    }
}
