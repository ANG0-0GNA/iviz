using System.Xml;

namespace Iviz.Urdf
{
    public class Parent
    {
        public string Link { get; }

        internal Parent(XmlNode node)
        {
            Link = Utils.ParseString(node.Attributes["link"]);
        }
    }
}