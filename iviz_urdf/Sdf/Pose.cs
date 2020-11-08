using System;
using System.Xml;
using Iviz.Urdf;

namespace Iviz.Sdf
{
    public sealed class Pose
    {
        public string? RelativeTo { get;}
        public Vector3f Position { get; } = Vector3f.Zero;
        public Vector3f Orientation { get; } = Vector3f.Zero;

        Pose() {}

        internal Pose(XmlNode node)
        {
            RelativeTo = node.Attributes?["relative_to"]?.Value;

            if (node.InnerText is null)
            {
                throw new MalformedSdfException();
            }            
            
            string[] elems = node.InnerText.Split(Vector3f.Separator, StringSplitOptions.RemoveEmptyEntries);
            if (elems.Length != 6)
            {
                throw new MalformedSdfException(node);
            }

            double x = double.Parse(elems[0], Utils.Culture);
            double y = double.Parse(elems[1], Utils.Culture);
            double z = double.Parse(elems[2], Utils.Culture);
            double rr = double.Parse(elems[3], Utils.Culture);
            double rp = double.Parse(elems[4], Utils.Culture);
            double ry = double.Parse(elems[5], Utils.Culture);

            Position = new Vector3f(x, y, z);
            Orientation = new Vector3f(rr, rp, ry);
        }
        
        public static readonly Pose Identity = new Pose();        
    }
}