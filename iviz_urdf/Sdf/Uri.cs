using System;
using System.Xml;

namespace Iviz.Sdf
{
    public sealed class Uri
    {
        public string Value { get; }

        public System.Uri ToUri() => new System.Uri(Value);
        
        internal Uri(XmlNode node)
        {
            Value = node.InnerText ?? throw new MalformedSdfException();
        }        
    }
}