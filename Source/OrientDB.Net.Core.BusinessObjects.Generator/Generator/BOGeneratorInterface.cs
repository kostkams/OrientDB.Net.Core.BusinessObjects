using System.IO;
using System.Text;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class BOGeneratorInterface
    {
        public static void Execute(DirectoryInfo outputDirectory, Configuration configuration, Project project, Type businessObjectType)
        {
            var sb = new StringBuilder();
            if (businessObjectType.ReferenceLists != null)
                sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using OrientDB.Net.Core.BusinessObjects;");
            sb.AppendLine();
            sb.AppendLine($"namespace {project.Name}{(string.IsNullOrEmpty(configuration.Namespace) ? "" : $".{configuration.Namespace}")}");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface I{businessObjectType.Name} : IBusinessObject");
            sb.AppendLine("    {");
            foreach (var property in businessObjectType.Properties)
                sb.AppendLine($"        {GeneratorHelper.Convert(property.Type)}{(property.Nullable ? "?" : "")} {property.Name.ToCamelUpperCase()} {{ get; set; }}");
            if (businessObjectType.Children != null)
                foreach (var child in businessObjectType.Children)
                    sb.AppendLine($"        I{child.Type} {child.Name.ToCamelUpperCase()} {{ get; }}");
            if(businessObjectType.ReferenceLists != null)
                foreach (var referenceList in businessObjectType.ReferenceLists)
                {
                    sb.AppendLine($"        IList<I{referenceList.Type.ToCamelUpperCase()}> {referenceList.Name.ToCamelUpperCase()} {{ get; }}");
                }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputDirectory.FullName, $"I{businessObjectType.Name}.cs"), sb.ToString());
        }
    }
}