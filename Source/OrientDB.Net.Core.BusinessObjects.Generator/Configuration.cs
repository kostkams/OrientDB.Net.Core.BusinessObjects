using System.Collections.Generic;

namespace OrientDB.Net.Core.BusinessObjects.Generator
{
    public class Configuration
    {
        public string Name { get; set; }
        public List<Project> Projects { get; set; }
        public string Namespace { get; set; }
    }

    public class Project
    {
        public string Name { get; set; }
        public BusinessObject BusinessObject { get; set; }
    }

    public class BusinessObject
    {
        public List<Type> Types { get; set; }
    }

    public class ReferenceList
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string EdgeClassName { get; set; }
    }

    public class Type
    {
        public bool IsRoot { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public List<Property> Properties { get; set; }
        public List<Child> Children { get; set; }
        public List<ReferenceList> ReferenceLists { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public EType Type { get; set; }
        public bool Required { get; set; }
        public bool Nullable { get; set; }
        public string DocumentPropertyName { get; set; }
    }

    public class Child
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string EdgeClassName { get; set; }
    }

    public enum EType
    {
        String,
        Boolean,
        Integer,
        Double,
        DateTime,
        Guid,
    }
}