using System;

namespace OrientDB.Net.Core.BusinessObjects
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DocumentPropertyAttribute : Attribute
    {
        public DocumentPropertyAttribute(string key, bool required)
        {
            Key = key;
            Required = required;
        }

        public DocumentPropertyAttribute(string key)
            : this(key, false)
        {
        }

        public string Key { get; set; }
        public bool Required { get; set; }
    }
}