using System.Runtime.Serialization;

namespace Iviz.Msgs.std_msgs
{
    [DataContract]
    public sealed class UInt16MultiArray : IMessage
    {
        // Please look at the MultiArrayLayout message definition for
        // documentation on all multiarrays.
        
        [DataMember] public MultiArrayLayout layout { get; set; } // specification of data layout
        [DataMember] public ushort[] data { get; set; } // array of data
        
    
        /// <summary> Constructor for empty message. </summary>
        public UInt16MultiArray()
        {
            layout = new MultiArrayLayout();
            data = System.Array.Empty<ushort>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public UInt16MultiArray(MultiArrayLayout layout, ushort[] data)
        {
            this.layout = layout ?? throw new System.ArgumentNullException(nameof(layout));
            this.data = data ?? throw new System.ArgumentNullException(nameof(data));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal UInt16MultiArray(Buffer b)
        {
            this.layout = new MultiArrayLayout(b);
            this.data = b.DeserializeStructArray<ushort>();
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new UInt16MultiArray(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.layout);
            b.SerializeStructArray(this.data, 0);
        }
        
        public void Validate()
        {
            if (layout is null) throw new System.NullReferenceException();
            layout.Validate();
            if (data is null) throw new System.NullReferenceException();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 4;
                size += layout.RosMessageLength;
                size += 2 * data.Length;
                return size;
            }
        }
    
        string IMessage.RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "std_msgs/UInt16MultiArray";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "52f264f1c973c4b73790d384c6cb4484";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE71U32vbMBB+919xJC9tlmb5Ucpa6ENgsJcWBh2MEkJQrXOsRLaCJDfr/vp9kh3bafc4" +
                "ZgyW73R33/fpTkP6rlk4Jm3MnoQnnzM9VtqrpbXi7UG8mcpTwc6JLZPkTJXKK1NSZmwyJGnSquDSi2jD" +
                "K7SmIoSLEO4mSfIhGenmWz9DcgdOVabSJklGUnjR7EoqVfrZzWpNvSf62/BY6RSWJMn9P36Sx6dvd+S8" +
                "3BRu6z6/5wMVfkCzjjRUSrWw7EjQlku2Kq29V1JBKweSQneoBRIchPUqrRBVc/NvB54QfT3tRyrLZKxk" +
                "y5IyawpCZbZUGBcAeEOqLJv/M83bFFAQ5SHXspXr5KKDNQcGAnZR7sU8otiYLHPcO6eDkFKVW2LN4cxd" +
                "aBdgKX0nPtKnKZrFWEcuN5WWtHz4uXx+ohemo1XecwmoBOyFOwfhvFWSkUGU8tQSIBt5XgVevb2ZsoHn" +
                "kPB2wl+o8W68v6T7CGbV5/ApBG/qEqvZeqTOLfP1aAfLfp0MAwVgAQhh5ZgWV2kuIK2mm+vpr+svU1JF" +
                "mISj8jmIABvG5xU4U6ONpWazQ5ZjZA/aHRfh7mIBVF5N1xMtXpAXcAc5q23uB53Lqd9MwYWKPWtEC+ti" +
                "BDSjgOaebuezm+mU6KI0npudjZikHO0qKBfTQe2I/bJJOOsjOCrp80HnaQGgUM96BgDf2e385J730zU6" +
                "DDpfm3DRs7XpoiwfT9JyxugktHe4loLk1hzHtMMCeldFOY7dsg//dcXJf5z/draSQASD0fDHqNQrKL5V" +
                "r+j4tnNP89WoES6/5mjebaSLMCW4BqjChesu28BashBYr/5S4w9Lviw21AUAAA==";
                
    }
}
