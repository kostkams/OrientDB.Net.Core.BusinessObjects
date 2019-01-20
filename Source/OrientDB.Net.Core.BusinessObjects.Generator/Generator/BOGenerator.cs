using System.IO;

namespace OrientDB.Net.Core.BusinessObjects.Generator.Generator
{
    internal static class BOGenerator
    {
        public static void Execute(DirectoryInfo outputDirectory, Configuration configuration, Project project)
        {
            foreach (var businessObjectType in project.BusinessObject.Types)
            {
                BOGeneratorInterface.Execute(outputDirectory, configuration, project, businessObjectType);
                BOGeneratorClass.Execute(outputDirectory, configuration, project, businessObjectType);
            }
        }
    }
}