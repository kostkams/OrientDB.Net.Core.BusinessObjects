using System;

namespace OrientDB.Net.Core.BusinessObjects
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReferenceListAttribute : Attribute
    {
        public ReferenceListAttribute(string edgeClassName)
        {
            EdgeClassName = edgeClassName;
        }

        public string EdgeClassName { get; set; }
    }
}