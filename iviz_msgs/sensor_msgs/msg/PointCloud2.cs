using System.Runtime.Serialization;

namespace Iviz.Msgs.sensor_msgs
{
    public sealed class PointCloud2 : IMessage
    {
        // This message holds a collection of N-dimensional points, which may
        // contain additional information such as normals, intensity, etc. The
        // point data is stored as a binary blob, its layout described by the
        // contents of the "fields" array.
        
        // The point cloud data may be organized 2d (image-like) or 1d
        // (unordered). Point clouds organized as 2d images may be produced by
        // camera depth sensors such as stereo or time-of-flight.
        
        // Time of sensor data acquisition, and the coordinate frame ID (for 3d
        // points).
        public std_msgs.Header header { get; set; }
        
        // 2D structure of the point cloud. If the cloud is unordered, height is
        // 1 and width is the length of the point cloud.
        public uint height { get; set; }
        public uint width { get; set; }
        
        // Describes the channels and their layout in the binary data blob.
        public PointField[] fields { get; set; }
        
        public bool is_bigendian { get; set; } // Is this data bigendian?
        public uint point_step { get; set; } // Length of a point in bytes
        public uint row_step { get; set; } // Length of a row in bytes
        public byte[] data { get; set; } // Actual point data, size is (row_step*height)
        
        public bool is_dense { get; set; } // True if there are no invalid points
    
        /// <summary> Constructor for empty message. </summary>
        public PointCloud2()
        {
            header = new std_msgs.Header();
            fields = System.Array.Empty<PointField>();
            data = System.Array.Empty<byte>();
        }
        
        /// <summary> Explicit constructor. </summary>
        public PointCloud2(std_msgs.Header header, uint height, uint width, PointField[] fields, bool is_bigendian, uint point_step, uint row_step, byte[] data, bool is_dense)
        {
            this.header = header ?? throw new System.ArgumentNullException(nameof(header));
            this.height = height;
            this.width = width;
            this.fields = fields ?? throw new System.ArgumentNullException(nameof(fields));
            this.is_bigendian = is_bigendian;
            this.point_step = point_step;
            this.row_step = row_step;
            this.data = data ?? throw new System.ArgumentNullException(nameof(data));
            this.is_dense = is_dense;
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal PointCloud2(Buffer b)
        {
            this.header = new std_msgs.Header(b);
            this.height = BuiltIns.DeserializeStruct<uint>(b);
            this.width = BuiltIns.DeserializeStruct<uint>(b);
            this.fields = BuiltIns.DeserializeArray<PointField>(b, 0);
            this.is_bigendian = BuiltIns.DeserializeStruct<bool>(b);
            this.point_step = BuiltIns.DeserializeStruct<uint>(b);
            this.row_step = BuiltIns.DeserializeStruct<uint>(b);
            this.data = BuiltIns.DeserializeStructArray<byte>(b, 0);
            this.is_dense = BuiltIns.DeserializeStruct<bool>(b);
        }
        
        public IMessage Deserialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            return new PointCloud2(b);
        }
    
        public void Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            this.header.Serialize(b);
            BuiltIns.Serialize(this.height, b);
            BuiltIns.Serialize(this.width, b);
            BuiltIns.SerializeArray(this.fields, b, 0);
            BuiltIns.Serialize(this.is_bigendian, b);
            BuiltIns.Serialize(this.point_step, b);
            BuiltIns.Serialize(this.row_step, b);
            BuiltIns.Serialize(this.data, b, 0);
            BuiltIns.Serialize(this.is_dense, b);
        }
        
        public void Validate()
        {
            if (header is null) throw new System.NullReferenceException();
            if (fields is null) throw new System.NullReferenceException();
            if (data is null) throw new System.NullReferenceException();
        }
    
        [IgnoreDataMember]
        public int RosMessageLength
        {
            get {
                int size = 26;
                size += header.RosMessageLength;
                for (int i = 0; i < fields.Length; i++)
                {
                    size += fields[i].RosMessageLength;
                }
                size += 1 * data.Length;
                return size;
            }
        }
    
        [IgnoreDataMember]
        public string RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve]
        public const string RosMessageType = "sensor_msgs/PointCloud2";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve]
        public const string RosMd5Sum = "1158d486dd51d683ce2f1be655c3c181";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve]
        public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE7VV32/bNhB+119xqB9qF7GHpF0WBDCGYkHWAF1aoNnTMASUeLaJUaRLUsm0v37fkaLt" +
                "bHvYwybYkHS6++73xxk97EyknmNUW6adtzqSos5by10y3pHf0P1Sm55dxKuytPfGpXhGzzvT7ahXYzOD" +
                "vkvKOFJam1TUjNv40KuMEQdoqkhOJBa2QBC8NJ4Rp26FIBgoGZm0SooQU0w+sBYzRa1xKozUWt/COEWy" +
                "avQDdDl2wbRQa0dKGURCYQQogUNCrzaGkdQrUiGocdVABd4mX531gy4ekQi1TD5slTN/APFC09z0qMrS" +
                "mt94gS90rmE9H5CGZsS2WNHnI0w8sUXQMM/WsSLvg9dDl0OVMFXPQSGBfdpRRDF8iIc6xQR4Lx4TCr/0" +
                "m+XGmu0uleghkuSKUQledV8HE3Ppz0g5nTPvPOJE4RLTJsAd3d3QHE2ht7oWOy5WzQdWyIZ2+SYOLm4Q" +
                "QBi6NASuVTwp14ruiqwUD506FOQMKBInhMA5z5E8G40MoSUmlt0Wb/8A2gx4fnsxAdS3bCwx3UyNLjDd" +
                "TjnHNtZUTagDgRkUhWlecm1kaFZNbtStjMIvv1IZiaZpvbeEy8TH1mzZaaMczehOvCDiYl4/fF+DKnE/" +
                "okl72M7o4yEpNaWEKNoxcTxYBP9c9f9qgU8v9a8QYPZcrxm9Ry/q5uVvZxQxZ1LVeYV+Uyq3mLJCShoD" +
                "wkeQhzDAIlcefVX4Ow/PT8oaPU1D06z/46v56cuP1xgn/djHbfymzBr6+SWhdSpoME9SOVsZzB0y4LC0" +
                "/MQWRqrf87SdadxzXOXdRdL4oSnYH2tHGiKUkse49/3gTCfzLmvzwh6WQk+0VyGZbrAq/G09BB2/yF8H" +
                "dp1sy7WQSeRuSAYBjUDoAqto3FZWqTYXBs3s4dkvhdS2HI7OUWolu0D8+z6AYTMxXMPHm5LcCtgoDsML" +
                "2GOeZY94jQu0V0LgvQchzBH55zHtfJntJxWMam3ufocKAPW1GL1enCC7DO2U8xW+IB59/BtYd8DNPISt" +
                "01ayj8MWBYQiOO3J6AP9YpcNuJesaQP2rxGr4rKZ3WYKOu4V7ipG3xk0QFgCew7WEfTcjUej/7dpzMRZ" +
                "BvJIC3W2Xh6FklQ5ZPb1NPSuMhdSDeNEOTDPWD8Im10cUMoZWNjtiu7uH65kGdd0Pkl+nkRrujjqnF9m" +
                "ydsTHRGt6d1RR1gFkm9PdES0pstJcvvx03sRrem7U8nlO0iumlprJ02ZCOJelYMls2Odbr/ZRE5F4VN5" +
                "3gTfS1dDEu1SinJgTI5yg2VjxeimPrMb5MgrZ1RkUFDrn7j66fzg0hTIB1Bir9xIbLnPZ/lE6yWy5k8H" +
                "yKIrtQgAAA==";
                
    }
}
