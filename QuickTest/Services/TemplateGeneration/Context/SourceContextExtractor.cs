using Microsoft.VisualStudio.Shell;
using QuickTest.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickTest.Services.TemplateGeneration.Context
{
    internal class SourceContextExtractor
    {
        public TestContext ExtractContext(string originalFilePath, string targetFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(originalFilePath))
                throw new ArgumentNullException(nameof(originalFilePath));
            if (string.IsNullOrEmpty(targetFilePath))
                throw new ArgumentNullException(nameof(targetFilePath));

            return new TestContext
            {
                Namespace = GetTargetNamespace(targetFilePath),
                ClassName = Path.GetFileNameWithoutExtension(originalFilePath),
                Usings = GetUsings(originalFilePath),
                Dependencies = AnalyzeDependencies(originalFilePath)
            };
        }

        private string[] GetUsings(string originalFilePath)
        {
            var usings = new List<string>();

            try
            {
                string[] fileLines = File.ReadAllLines(originalFilePath);
                string originalNamespace = null;

                foreach (string line in fileLines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("using") && trimmedLine.Contains("Xtel"))
                    {
                        string usingStatement = trimmedLine
                            .Replace("using", "")
                            .Replace(";", "")
                            .Trim();

                        usings.Add(usingStatement);
                    }

                    if (trimmedLine.StartsWith("namespace"))
                    {
                        originalNamespace = trimmedLine
                            .Replace("namespace", "")
                            .Replace("{", "")
                            .Trim();

                        usings.Add(originalNamespace);
                    }
                }
            }
            catch (Exception)
            {
                // If we can't read the file, continue with empty usings 
            }

            return usings.Distinct().OrderBy(u => u).ToArray();
        }

        private object[] AnalyzeDependencies(string originalFilePath)
        {
            var dependencies = new List<object>();
            try
            {
                string[] fileLines = File.ReadAllLines(originalFilePath);
                string className = Path.GetFileNameWithoutExtension(originalFilePath);

                var constructorLines = new List<string>();
                bool inConstructor = false;
                bool foundAnyConstructor = false;
                int bracketCount = 0;

                for (int i = 0; i < fileLines.Length; i++)
                {
                    string line = fileLines[i].Trim();

                    if (line.Contains($"public {className}("))
                    {
                        foundAnyConstructor = true;
                        inConstructor = true;
                        bracketCount = line.Count(c => c == '(') - line.Count(c => c == ')');
                        constructorLines.Clear();
                        constructorLines.Add(line);

                        if (bracketCount == 0)
                        {
                            ProcessConstructor(constructorLines[0], dependencies);
                            inConstructor = false;
                        }
                        continue;
                    }

                    if (inConstructor)
                    {
                        constructorLines.Add(line);
                        bracketCount += line.Count(c => c == '(') - line.Count(c => c == ')');

                        if (bracketCount == 0)
                        {
                            ProcessConstructor(string.Join(" ", constructorLines), dependencies);
                            inConstructor = false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If we can't read the file, return empty dependencies
            }

            return dependencies.ToArray();
        }

        private void ProcessConstructor(string constructorText, List<object> dependencies)
        {
            int startIndex = constructorText.IndexOf('(') + 1;
            int endIndex = constructorText.LastIndexOf(')');
            if (startIndex <= 0 || endIndex <= 0 || endIndex <= startIndex)
                return;

            string parameters = constructorText.Substring(startIndex, endIndex - startIndex);
            var paramList = parameters.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in paramList)
            {
                string trimmedParam = param.Trim();
                string[] parts = trimmedParam.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    string paramType = parts[0];
                    string name = parts[1];

                    if (name.Contains("="))
                        name = name.Substring(0, name.IndexOf('=')).Trim();

                    dependencies.Add(new
                    {
                        interfaceType = paramType,
                        name = name,
                        needsSubstitute = paramType.StartsWith("I") && char.IsUpper(paramType[1]),
                        defaultValue = GetDefaultValue(paramType)
                    });
                }
            }
        }

        private string GetDefaultValue(string paramType)
        {
            switch (paramType.ToLower())
            {
                case "string":
                    return "\"test\"";
                case "int":
                case "int32":
                    return "0";
                case "long":
                case "int64":
                    return "0L";
                case "bool":
                case "boolean":
                    return "false";
                case "decimal":
                    return "0m";
                case "double":
                    return "0d";
                case "float":
                    return "0f";
                case "datetime":
                    return "DateTime.Now";
                case "guid":
                    return "Guid.NewGuid()";
                default:
                    return "null";
            }
        }

        private string GetTargetNamespace(string targetFilePath)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(targetFilePath);
                string[] pathParts = directoryPath.Split(Path.DirectorySeparatorChar);

                int projectFolderIndex = -1;
                for (int i = pathParts.Length - 1; i >= 0; i--)
                {
                    if (Directory.GetFiles(string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Take(i + 1)), "*.csproj").Any())
                    {
                        projectFolderIndex = i;
                        break;
                    }
                }

                if (projectFolderIndex == -1)
                    return string.Empty;

                var namespaceParts = pathParts
                    .Skip(projectFolderIndex)
                    .Where(part => !string.IsNullOrEmpty(part));

                return string.Join(".", namespaceParts);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
