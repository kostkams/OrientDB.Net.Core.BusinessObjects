using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class SolutionGenerator
    {
        internal static void Execute(DirectoryInfo outputDirectory, Configuration configuration)
        {
            var projects = new List<Tuple<string, Guid>>();

            var sb = new StringBuilder();
            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio Version 16");
            sb.AppendLine("VisualStudioVersion = 16.0.28407.52");
            sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
            foreach (var project in configuration.Projects)
            {
                var guid = Guid.NewGuid();
                var projectGuid = Guid.NewGuid();
                sb.AppendLine($"Project(\"{{{guid}}}\") = \"{project.Name}\", \"{project.Name}\\{project.Name}.BusinessObjects.csproj\", \"{{{projectGuid}}}\"");
                projects.Add(new Tuple<string, Guid>(project.Name, projectGuid));
            }
            sb.AppendLine("Global");
            sb.AppendLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("		Debug|Any CPU = Debug|Any CPU");
            sb.AppendLine("		Release|Any CPU = Release|Any CPU");
            sb.AppendLine("	EndGlobalSection");
            sb.AppendLine("	GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var project in projects)
            {
                sb.AppendLine($"		{{{project.Item2}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                sb.AppendLine($"		{{{project.Item2}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                sb.AppendLine($"		{{{project.Item2}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                sb.AppendLine($"		{{{project.Item2}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            }
            sb.AppendLine("	EndGlobalSection");
            sb.AppendLine("	GlobalSection(SolutionProperties) = preSolution");
            sb.AppendLine("		HideSolutionNode = FALSE");
            sb.AppendLine("	EndGlobalSection");
            sb.AppendLine("	GlobalSection(ExtensibilityGlobals) = postSolution");
            sb.AppendLine($"		SolutionGuid = {{{Guid.NewGuid()}}}");
            sb.AppendLine("	EndGlobalSection");
            sb.AppendLine("EndGlobal");
            
            File.WriteAllText(Path.Combine(outputDirectory.FullName, $"{configuration.Name}.sln"), sb.ToString());
        }
    }
}