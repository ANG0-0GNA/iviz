namespace Iviz.Msgs.visualization_msgs
{
    public sealed class MenuEntry : IMessage
    {
        // MenuEntry message.
        
        // Each InteractiveMarker message has an array of MenuEntry messages.
        // A collection of MenuEntries together describe a
        // menu/submenu/subsubmenu/etc tree, though they are stored in a flat
        // array.  The tree structure is represented by giving each menu entry
        // an ID number and a "parent_id" field.  Top-level entries are the
        // ones with parent_id = 0.  Menu entries are ordered within their
        // level the same way they are ordered in the containing array.  Parent
        // entries must appear before their children.
        
        // Example:
        // - id = 3
        //   parent_id = 0
        //   title = "fun"
        // - id = 2
        //   parent_id = 0
        //   title = "robot"
        // - id = 4
        //   parent_id = 2
        //   title = "pr2"
        // - id = 5
        //   parent_id = 2
        //   title = "turtle"
        //
        // Gives a menu tree like this:
        //  - fun
        //  - robot
        //    - pr2
        //    - turtle
        
        // ID is a number for each menu entry.  Must be unique within the
        // control, and should never be 0.
        public uint id;
        
        // ID of the parent of this menu entry, if it is a submenu.  If this
        // menu entry is a top-level entry, set parent_id to 0.
        public uint parent_id;
        
        // menu / entry title
        public string title;
        
        // Arguments to command indicated by command_type (below)
        public string command;
        
        // Command_type stores the type of response desired when this menu
        // entry is clicked.
        // FEEDBACK: send an InteractiveMarkerFeedback message with menu_entry_id set to this entry's id.
        // ROSRUN: execute "rosrun" with arguments given in the command field (above).
        // ROSLAUNCH: execute "roslaunch" with arguments given in the command field (above).
        public const byte FEEDBACK = 0;
        public const byte ROSRUN = 1;
        public const byte ROSLAUNCH = 2;
        public byte command_type;
    
        /// <summary> Constructor for empty message. </summary>
        public MenuEntry()
        {
            title = "";
            command = "";
        }
        
        public unsafe void Deserialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Deserialize(out id, ref ptr, end);
            BuiltIns.Deserialize(out parent_id, ref ptr, end);
            BuiltIns.Deserialize(out title, ref ptr, end);
            BuiltIns.Deserialize(out command, ref ptr, end);
            BuiltIns.Deserialize(out command_type, ref ptr, end);
        }
    
        public unsafe void Serialize(ref byte* ptr, byte* end)
        {
            BuiltIns.Serialize(id, ref ptr, end);
            BuiltIns.Serialize(parent_id, ref ptr, end);
            BuiltIns.Serialize(title, ref ptr, end);
            BuiltIns.Serialize(command, ref ptr, end);
            BuiltIns.Serialize(command_type, ref ptr, end);
        }
    
        public int GetLength()
        {
            int size = 17;
            size += title.Length;
            size += command.Length;
            return size;
        }
    
        public IMessage Create() => new MenuEntry();
    
        /// <summary> Full ROS name of this message. </summary>
        public const string MessageType = "visualization_msgs/MenuEntry";
    
        /// <summary> MD5 hash of a compact representation of the message. </summary>
        public const string Md5Sum = "b90ec63024573de83b57aa93eb39be2d";
    
        /// <summary> Base64 of the GZip'd compression of the concatenated dependencies file. </summary>
        public const string DependenciesBase64 =
                "H4sIAAAAAAAAE51Uy27bMBC8+ysWzqEJkFedFigC+JAmThu0Tou0OQeUtLYIS6RKUk78950lJVtuDgF6" +
                "Epeand3ZBw9ozqadmeA2VLP3asmno9EBzVRe0p0J7FQe9Jrnyq3Y9RAqlSdlSDmnNmQXr0n8KUiuKLdV" +
                "xSCwZojS7CnYJYcSlAX73OmMScGjBuLMt1n/7Y8ccgqO+ZhCadtliQ9vEJ7JB+u4II1kaFGpAJKY1SnR" +
                "75KjEzCuzUMLtPbkuHHsGdIKyja01GttlsSiV0IRiwxhMXR3Q6atMySpTAH+cYOIJjzpYkwLzVUhQWxz" +
                "UvGaq+goyiQrpAcKa2A+61DS1pGmdA6veR+pd7CuYNEhaGiBv3ZgSMywyKua6RnV3irvXRIcpTZBaSNi" +
                "+gL8jFFB00eqWx9INQ0rRxkvbMpUO8pLXRUAp96/qLqp+BLHE4opX+BI+yLiTdChYljjRWvGO/jkLbiz" +
                "mQ0Dhw+vHCb7Do2bDOAf34Kj1zjBA9dfML2ocWpuHIdKr0S39qIQnEg+HWJakQkGQvbHRCelwURoIevG" +
                "AhX8d3KkuVJlDHRr9J+WBy0FgXTJ2eo4TpTHLFcFGTRZ+oHJGLXahIsJZHbRsDXS3KQ1WUhgF+6Y9IJ0" +
                "SFl124IU7hKw26gETZiwN6/w9xwGpQx2kMX2etQTnXVUsdIj7JWMWzJk392yBSzIdkNpXYtIbQqdq27b" +
                "usunsGmYDjOu7PNRT9P9E6LrISxuuI9liDaKALuxxrM8HjquTclmV5pu4qPivNL5igt5jm5ns5vPV9ff" +
                "LqFZ9tm8fuBumYtM5avtQxe3VzifIqOUSAoGfTFavHzn0S8J8PDj18Pj/SXxC+dtYJlz77AZiUVtq4M3" +
                "B+lu9zbVKT4odKgyu+ajju371eP99dd9wkq1Ji//i1Pa+mlbhul5d5HSnr7fmSnudNLdDLs2Gv0F7VRe" +
                "/DMGAAA=";
                
    }
}
