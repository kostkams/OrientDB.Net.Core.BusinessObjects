using System.IO;
using System.Text;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class ProjectGenerator
    {
        internal static void Execute(DirectoryInfo outputDirectory, Configuration configuration, Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine("");
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <TargetFramework>netstandard2.0</TargetFramework>");
            sb.AppendLine("    <SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("");
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("     <PackageReference Include=\"OrientDB.Net.Core.BusinessObjects\" Version=\"1.0.2\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("");
            sb.AppendLine("</Project>");
            sb.AppendLine("");

            var outputDir = new DirectoryInfo(Path.Combine(outputDirectory.FullName, project.Name));
            outputDir.Create();
            File.WriteAllText(Path.Combine(outputDir.FullName, $"{project.Name}.BusinessObjects.csproj"), sb.ToString());

            BOGenerator.Execute(outputDir, configuration, project);
        }
    }
}