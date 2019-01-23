﻿using System.IO;
using System.Linq;
using System.Text;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class BOGeneratorClass
    {
        public static void Execute(DirectoryInfo outputDirectory, Configuration configuration, Project project, Type businessObjectType)
        {
            var sb = new StringBuilder();
            if (businessObjectType.Properties.Any(p => p.Type == EType.Guid || p.Type == EType.DateTime))
                sb.AppendLine("using System;");
            if (businessObjectType.ReferenceLists != null)
                sb.AppendLine("using System.Collections.Generic;");
            foreach (var projectConfiguration in GeneratorHelper.GetReferencedProjects(configuration, project, businessObjectType))
            {
                sb.AppendLine($"using {projectConfiguration.Name};");
            }
            sb.AppendLine("using OrientDB.Net.Core.BusinessObjects;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}{(string.IsNullOrEmpty(configuration.Namespace) ? "" : $".{configuration.Namespace}")}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {businessObjectType.Name}BO : BusinessObject, I{businessObjectType.Name}");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {businessObjectType.Name}BO()");
            sb.AppendLine("        {");
            sb.AppendLine($"            ClassName = \"{businessObjectType.ClassName}\";");
            if (businessObjectType.Children != null)
                foreach (var child in businessObjectType.Children)
                {
                    sb.AppendLine($"            {child.Name.ToCamelUpperCase()} = new {child.Type.ToCamelUpperCase()}BO();");
                }
            if (businessObjectType.ReferenceLists != null)
                foreach (var referenceList in businessObjectType.ReferenceLists)
                {
                    sb.AppendLine($"            {referenceList.Name.ToCamelUpperCase()} = new ReferenceList<I{referenceList.Type.ToCamelUpperCase()}>();");
                }
            sb.AppendLine("        }");
            sb.AppendLine("");
            foreach (var property in businessObjectType.Properties)
            {
                sb.AppendLine($"        [DocumentProperty(\"{property.DocumentPropertyName}\", {GeneratorHelper.Convert(property.Required)})]");
                sb.AppendLine($"        public {GeneratorHelper.Convert(property.Type)}{(property.Nullable ? "?" : "")} {property.Name.ToCamelUpperCase()} {{ get; set; }}");
            }

            if (businessObjectType.Children != null)
                foreach (var child in businessObjectType.Children)
                {
                    sb.AppendLine($"        [Child(\"{child.EdgeClassName}\")]");
                    sb.AppendLine($"        public I{child.Type} {child.Name.ToCamelUpperCase()} {{ get; set; }}");
                }
            if(businessObjectType.ReferenceLists != null)
                foreach (var referenceList in businessObjectType.ReferenceLists)
                {
                    sb.AppendLine($"        [ReferenceList(\"{referenceList.EdgeClassName}\")]");
                    sb.AppendLine($"        public IList<I{referenceList.Type.ToCamelUpperCase()}> {referenceList.Name.ToCamelUpperCase()} {{ get; }}");
                }
            sb.AppendLine("    }");
            if (businessObjectType.IsRoot)
            {
                sb.AppendLine();
                sb.AppendLine($"    public static class {businessObjectType.Name.ToCamelUpperCase()}BOExtension");
                sb.AppendLine("    {");
                sb.AppendLine($"        public static I{businessObjectType.Name.ToCamelUpperCase()} Create{businessObjectType.Name.ToCamelUpperCase()}(this ITransaction transaction)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var {businessObjectType.Name.ToCamelCase()} = new {businessObjectType.Name}BO();");
                sb.AppendLine($"            transaction.Create({businessObjectType.Name.ToCamelCase()});");
                sb.AppendLine($"            return {businessObjectType.Name.ToCamelCase()};");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputDirectory.FullName, $"{businessObjectType.Name}.cs"), sb.ToString());
        }
    }
}