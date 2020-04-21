
namespace Iviz.Msgs.geometry_msgs
{
    public sealed class AccelStamped : IMessage 
    {
        // An accel with reference coordinate frame and timestamp
        public std_msgs.Header header;
        public Accel accel;

        /// <summary> Full ROS name of this message. </summary>
        public const string MessageType = "geometry_msgs/AccelStamped";

        public IMessage Create() => new AccelStamped();

        public int GetLength()
        {
            int size = 48;
            size += header.GetLength();
            return size;
        }

        /// <summary> Constructor for empty message. </summary>
        public AccelStamped()
        {
            header = new std_msgs.Header();
            accel = new Accel();
        }
        
        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            header.Deserialize(ref ptr, end);
            accel.Deserialize(ref ptr, end);
        }

        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            header.Serialize(ref ptr, end);
            accel.Serialize(ref ptr, end);
        }

        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string Md5Sum = "d8a98a5d81351b6eb0578c78557e7659";

        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
            "H4sIAAAAAAAAE71UTWvbQBC961cM+JCk2CokpYdAD4HSNodCIKHXMN4dS0ukXXV3ZEf99X27spWGXnpo" +
            "awTWx8ybeW/e7IpuPLEx0tHBaUtRdhLFGyETQrTOswrtIvdC7C2p6yUp90P1RdhKpLb8VTcFoeBU1Ye/" +
            "/Ku+3n++pqT2sU9NejtXrlZ0r2iJo6VelC0r0y6gI9e0Ejed7NFR6VUsla86DZJqJD60LhGuRrxE7rqJ" +
            "xoQgDSDd96N3JrNeuJ7ykekgFg0c1Zmx4/ibSBkdV5LvYxHx9uM1YnwSM6pDQxMQTBROzjf4SNXovF5d" +
            "5oRq9XAIGzxKA12X4qQta25WnocoKffJ6Ro13szkamBDHEEVm+i8vHvEY7ogFEELMgTT0jk6v5u0DR6A" +
            "QnuOjredZGADBYB6lpPOLn5B9gXasw8n+BnxpcafwPoFN3PatJhZl9mnsYGACBxi2DuL0O1UQEznxCt1" +
            "bhs5TlXOmktWq0/FiJrHVyaCf04pGIcB2GLgKmnM6GUaj87+Kzc2EuC6OM2WLPY/Ges0qDTvAxymDvpA" +
            "qV0UUBkYGm5jeJL8EqZzmsDWC+TIO8a+Kd7KNoNdv4nREK/oGPLyfIz7PwyPVU8co2SOGBNI0r58e02w" +
            "zmtwW4wbPGzfC2OmILtkItG6iFSIUwMVxw7WV9aQg2yAej4oMHp+AqTARTmbhwFgWOXIPnWzsEVBOpe6" +
            "qdd0aKFqicouKDtbttwZiq5xds5EoX5JZjqSW5PuLuGirpt7novBkgCJQUvCRU23O5rCSIdMCDfxeLgE" +
            "2srSV1kCDWGdT5YjxGtB7wJmD1lS4gb74pPiWKuratcF1vfv6Hm5m5a7H9VP4NsfYa8FAAA=";

    }
}
