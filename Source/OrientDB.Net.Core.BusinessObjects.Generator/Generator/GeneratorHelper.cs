using System;
using System.Collections.Generic;
using System.Linq;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class GeneratorHelper
    {
        internal static string Convert(EType type)
        {
            switch (type)
            {
                case EType.String:
                    return "string";
                case EType.Boolean:
                    return "bool";
                case EType.Integer:
                    return "int";
                case EType.Double:
                    return "double";
                case EType.DateTime:
                    return "DateTime";
                case EType.Guid:
                    return "Guid";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        internal static string Convert(bool propertyRequired)
        {
            return propertyRequired ? "true" : "false";
        }

        internal static IEnumerable<Project> GetReferencedProjects(Configuration configuration, Project project, Type businessObjectType)
        {
            var test = (from proj in configuration.Projects
                        where proj.Name != project.Name
                        from type in proj.BusinessObject.Types
                        from requestedType in (businessObjectType.Children ?? new List<Child>())
                                             .Select(c => c.Type)
                                             .Concat((businessObjectType.ReferenceLists ?? new List<ReferenceList>()).Select(r => r.Type))
                        where type.Name == requestedType
                        select proj).ToList();
            return test;
        }

        internal static string ToCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            return str;
        }


        internal static string ToCamelUpperCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
                return char.ToUpperInvariant(str[0]) + str.Substring(1);
            return str;
        }
    }
}