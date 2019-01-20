using System;

namespace OrientDB.Net.Core.BusinessObjects
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChildAttribute : Attribute
    {
        public ChildAttribute(string edgeClassName)
        {
            EdgeClassName = edgeClassName;
        }

        public string EdgeClassName { get; set; }
    }
}