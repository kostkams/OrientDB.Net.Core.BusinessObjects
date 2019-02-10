using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class ProjectGenerator
    {
        internal static void Execute(DirectoryInfo outputDirectory, Configuration configuration, Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine();
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <TargetFramework>netstandard2.0</TargetFramework>");
            sb.AppendLine("    <SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("     <PackageReference Include=\"OrientDB.Net.Core.BusinessObjects\" Version=\"1.0.3\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            var projects = (from type in project.BusinessObject.Types
                            from proj in GeneratorHelper.GetReferencedProjects(configuration, project, type)
                            select proj).Distinct(new ProjectComparer()).ToList();
            foreach (var configurationProject in projects)
            {
                sb.AppendLine($"     <ProjectReference Include=\"..\\{configurationProject.Name}\\{configurationProject.Name}.BusinessObjects.csproj\" />");
            }
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("</Project>");
            sb.AppendLine();

            var outputDir = new DirectoryInfo(Path.Combine(outputDirectory.FullName, project.Name));
            outputDir.Create();
            File.WriteAllText(Path.Combine(outputDir.FullName, $"{project.Name}.BusinessObjects.csproj"), sb.ToString());

            BOGenerator.Execute(outputDir, configuration, project);
        }

        private class ProjectComparer:IEqualityComparer<Project>
        {
            public bool Equals(Project x, Project y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode(Project obj)
            {
                return -1;
            }
        }
    }


}