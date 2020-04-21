
namespace Iviz.Msgs.visualization_msgs
{
    public sealed class InteractiveMarkerFeedback : IMessage 
    {
        // Time/frame info.
        public std_msgs.Header header;
        
        // Identifying string. Must be unique in the topic namespace.
        public string client_id;
        
        // Feedback message sent back from the GUI, e.g.
        // when the status of an interactive marker was modified by the user.
        
        // Specifies which interactive marker and control this message refers to
        public string marker_name;
        public string control_name;
        
        // Type of the event
        // KEEP_ALIVE: sent while dragging to keep up control of the marker
        // MENU_SELECT: a menu entry has been selected
        // BUTTON_CLICK: a button control has been clicked
        // POSE_UPDATE: the pose has been changed using one of the controls
        public const byte KEEP_ALIVE = 0;
        public const byte POSE_UPDATE = 1;
        public const byte MENU_SELECT = 2;
        public const byte BUTTON_CLICK = 3;
        
        public const byte MOUSE_DOWN = 4;
        public const byte MOUSE_UP = 5;
        
        public byte event_type;
        
        // Current pose of the marker
        // Note: Has to be valid for all feedback types.
        public geometry_msgs.Pose pose;
        
        // Contains the ID of the selected menu entry
        // Only valid for MENU_SELECT events.
        public uint menu_entry_id;
        
        // If event_type is BUTTON_CLICK, MOUSE_DOWN, or MOUSE_UP, mouse_point
        // may contain the 3 dimensional position of the event on the
        // control.  If it does, mouse_point_valid will be true.  mouse_point
        // will be relative to the frame listed in the header.
        public geometry_msgs.Point mouse_point;
        public bool mouse_point_valid;

        /// <summary> Full ROS name of this message. </summary>
        public const string MessageType = "visualization_msgs/InteractiveMarkerFeedback";

        public IMessage Create() => new InteractiveMarkerFeedback();

        public int GetLength()
        {
            int size = 98;
            size += header.GetLength();
            size += client_id.Length;
            size += marker_name.Length;
            size += control_name.Length;
            return size;
        }

        /// <summary> Constructor for empty message. </summary>
        public InteractiveMarkerFeedback()
        {
            header = new std_msgs.Header();
            client_id = "";
            marker_name = "";
            control_name = "";
        }
        
        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            header.Deserialize(ref ptr, end);
            BuiltIns.Deserialize(out client_id, ref ptr, end);
            BuiltIns.Deserialize(out marker_name, ref ptr, end);
            BuiltIns.Deserialize(out control_name, ref ptr, end);
            BuiltIns.Deserialize(out event_type, ref ptr, end);
            pose.Deserialize(ref ptr, end);
            BuiltIns.Deserialize(out menu_entry_id, ref ptr, end);
            mouse_point.Deserialize(ref ptr, end);
            BuiltIns.Deserialize(out mouse_point_valid, ref ptr, end);
        }

        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            header.Serialize(ref ptr, end);
            BuiltIns.Serialize(client_id, ref ptr, end);
            BuiltIns.Serialize(marker_name, ref ptr, end);
            BuiltIns.Serialize(control_name, ref ptr, end);
            BuiltIns.Serialize(event_type, ref ptr, end);
            pose.Serialize(ref ptr, end);
            BuiltIns.Serialize(menu_entry_id, ref ptr, end);
            mouse_point.Serialize(ref ptr, end);
            BuiltIns.Serialize(mouse_point_valid, ref ptr, end);
        }

        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string Md5Sum = "ab0f1eee058667e28c19ff3ffc3f4b78";

        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
            "H4sIAAAAAAAAE71W227bRhB951cM4Ickhay0cRoEBvLg2koixBcVltpHYkUOqYXJXWZ3aUX9+p5ZXkzV" +
            "RdGHNoYBUcuZM3POXFYntNY1vy6cqpm0Kew8+cwqZ0e7+JEkJ7TM2QRdHLQpyQeHjzndtD7Qlqk1+msr" +
            "nhR2TME2OiMDLN+ojOdJZ05ZpQGR6lzgPjLnW5U9EKy8Kpk83lE8KZytI9CnzXJGPC/nsN/vuEP3QYXW" +
            "ky1IGUQM7FQW9CNTrdwDMt4rT7XNdaE5p+0h+rSe3Vyi3jecyRsPPJ3t/s5fmZwya4KzFXy1HxN0XLDz" +
            "YDfw6RxSITpS7By7M8RbHxqWVCUJfgRDnH1ZLFbpxfXyt8V5RxqpVEy5U2UpGMHSA3NDbTPm0SN0AQFx" +
            "s7jdpPeL68Xl+pwUMjQtAckdaAf2W4ZUnivOAuew/mWzXt/dppfXy8svYr5tQ7BmBB9dUJ/sIXqs7u4X" +
            "6WZ1dbFGjhK5sZ4nhjtlSqjbesnXmpFiD+mTFsK+nzClD/RjfzjBxulP/emEEU7f9KfTzHF8lgzWdxuA" +
            "XN39fovTt0eHmxWOfh4Mo+ZpQBWkGpetcyJ4ZPNXTW9t4HP6rKTC0tOPqtI5FRYdUVVUDO0qWH6elGxr" +
            "huBp7Uv/eiWAghqjQASljY/wy6sh0FCQSbVgfGeqwyTUVIaYO0IJk7M30S2Nbv0ELYsJPUKjTtWaTTSa" +
            "kSD36swwHBiHtLE6dmOtDrFsqh/eM8qxCozX1qhKOOmAx6MeRsXlC5z7es9JktGBcsv+KEDaUdtrSAhN" +
            "g2sZxscZDC8dVyoOIvSXWN02qrQX0frsunX0XH4gHaFuLRr7WR5J8uE//ktu7j9hikPeJdItTdkzAWtE" +
            "OSl2ULkKKlZ3p8sdu9MKKlayx+oGzOLbvquwMGTj4L9kg8VUoTnAIRdJMlvX2LOZClAIJTryhycUUtQo" +
            "F3TWVsrB3rpcGzGPSgo6/j1jU5tMOvNc6uc5a0V1RNImc6ziTKNth76DQ3Ky3ttT2ZUlFuQYHBVRQZLl" +
            "b43DkkQyyp8jxg8duTmwZcUhSu7pZTxL8dW/IgRBCtxY7OCXyHx1CLuuqzAMTqttFTs6gwJAfSFOL15N" +
            "kE2ENsrYAb5DfIrxb2DNiCucTrHV8ipeb20JAWHYOPuo86d7pLvB0JNbpzC+4tWFTE4+xm6NF0asCD6V" +
            "9zbTStp3r8NuuCNiNdL/rxufryYQvMB4SZGQvhoGOq5BqFQ4Bg25qmfSZXKc9++74Zcr0To9+M4p6SZu" +
            "MEh+bcHSmYj7ZPe9CHZrJE5ONt290+WF0YgpH9FNisqq8O4tfRufDuPTH98n/SfpBg5jobz8wpnoeZy8" +
            "fPv6pDv2S42fOP/MaHjaJ8mfRXnjYvMJAAA=";

    }
}
