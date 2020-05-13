using System.Runtime.Serialization;

namespace Iviz.Msgs.geometry_msgs
{
    [DataContract]
    public sealed class AccelWithCovarianceStamped : IMessage
    {
        // This represents an estimated accel with reference coordinate frame and timestamp.
        [DataMember] public std_msgs.Header header { get; set; }
        [DataMember] public AccelWithCovariance accel { get; set; }
    
        /// <summary> Constructor for empty message. </summary>
        public AccelWithCovarianceStamped()
        {
            header = new std_msgs.Header();
            accel = new AccelWithCovariance();
        }
        
        /// <summary> Explicit constructor. </summary>
        public AccelWithCovarianceStamped(std_msgs.Header header, AccelWithCovariance accel)
        {
            this.header = header ?? throw new System.ArgumentNullException(nameof(header));
            this.accel = accel ?? throw new System.ArgumentNullException(nameof(accel));
        }
        
        /// <summary> Constructor with buffer. </summary>
        internal AccelWithCovarianceStamped(Buffer b)
        {
            this.header = new std_msgs.Header(b);
            this.accel = new AccelWithCovariance(b);
        }
        
        ISerializable ISerializable.Deserialize(Buffer b)
        {
            return new AccelWithCovarianceStamped(b ?? throw new System.ArgumentNullException(nameof(b)));
        }
    
        void ISerializable.Serialize(Buffer b)
        {
            if (b is null) throw new System.ArgumentNullException(nameof(b));
            b.Serialize(this.header);
            b.Serialize(this.accel);
        }
        
        public void Validate()
        {
            if (header is null) throw new System.NullReferenceException();
            header.Validate();
            if (accel is null) throw new System.NullReferenceException();
            accel.Validate();
        }
    
        public int RosMessageLength
        {
            get {
                int size = 336;
                size += header.RosMessageLength;
                return size;
            }
        }
    
        string IMessage.RosType => RosMessageType;
    
        /// <summary> Full ROS name of this message. </summary>
        [Preserve] public const string RosMessageType = "geometry_msgs/AccelWithCovarianceStamped";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        [Preserve] public const string RosMd5Sum = "96adb295225031ec8d57fb4251b0a886";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        [Preserve] public const string RosDependenciesBase64 =
                "H4sIAAAAAAAAE71VTY/TQAy951dY6mFb1BaJRXtYiQMCAT0gIUB8Cq3cxEkGkpngmWyb/fW8mbShaDmA" +
                "BFtFajKxn+3nZ2dGb2vjSaVT8WKDJ7YkPpiWgxTEeS4N7UyoYVKKis2Fcue0MBYGVCq3ApeC4AE3brt1" +
                "9kK4EKU6/WWPI8R7IDxx16yGI0KCzbJH//iXvXzz/JJ8KK5aX/n7Yx7ZjN4EZMhaUCuBCw5MpUN+pqpF" +
                "V41co8SUOgpOb8PQiV/DMXGDqxIryk0zUO9hFBw4aNvemjySMJV+9IenscTUsQaT9w3rLc4iOi4v3/vE" +
                "6ebpJWysl7wPBgkNQMhV2Btb4SVlvbHh/EF0yGZvd26FR6nA8hScQs0hJiv72MuYJ/tLxLg3FrcGNsgR" +
                "RCk8zdPZFR79ghAEKUjn8prmyPzVEGpnASiUWrZtJALnYACoZ9HpbHGCbBO0ZeuO8CPizxh/Amsn3FjT" +
                "qkbPmli97ysQCMNO3bUpYLodEkjeGCiWGrNV1iGLXmPIbPYs6TLE9qWO4J+9d7lJqo56znzQiJ66cWWK" +
                "/6XGShxUp8Moyd8Mw1Fmx7b5cTqgt2DAFngrVVBYx2A0TWIPLw0MBQzrbJyv40TN6LXbrVr+Cn1PMz0C" +
                "uTKRdrG/gNCmScSYq9mnHIScmskc2gUxQdRHzUPMpdlLseL96bZIplHKG+Arhm2ZYpz4skrU4Hy/pGFJ" +
                "N0tSdwjAW9cH+kAR8dbxx98ff0rHi6xsHIeLh5/PL76cFHOHLfyrpm3VfZN4iL1hsGGhaoGi49ZkW6X1" +
                "EDcFNs47yYPTczqY/Hw+2N1NhYeoxxpPvw10nd79WuA6brJN2j3OYnO1whhLFDt5wrEwCtcolyg1fEyc" +
                "yhJ0UOHAnnUBGC1/A6RgEURv7jqAYRsrW9+MxCYGaS7rar2kXQ1Wk1Uc5LR206I2OampTDF6IlA7OTMd" +
                "ioNQywcYp6YZcx6DQbwAOYpusaZNSYPraRcLwo0evg+OtjLllfZYcG6ZBmWE+JXQVw69By3ec4WVZ33A" +
                "lwmje5Ax7ae7Ybq7yX4AgIYMhpsHAAA=";
                
    }
}
