using System.IO;
using OrientDB.Net.Core.BusinessObjects.Generator.Generator;

namespace OrientDB.Net.Core.BusinessObjects.Generator
{
    public static class BOGenerator
    {
        public static void Execute(DirectoryInfo outputDirectory, Configuration configuration)
        {
            if (!outputDirectory.Exists)
                outputDirectory.Create();
            else
            {
                outputDirectory.Delete(true);
                outputDirectory.Create();
            }

            SolutionGenerator.Execute(outputDirectory, configuration);
            
            foreach (var project in configuration.Projects)
            {
                ProjectGenerator.Execute(outputDirectory, configuration, project);
            }
        }
    }
}