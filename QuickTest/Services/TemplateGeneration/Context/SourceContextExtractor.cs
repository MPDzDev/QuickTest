using Microsoft.VisualStudio.Shell;
using QuickTest.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

            string sourceCode = string.Empty;
            try
            {
                sourceCode = File.ReadAllText(originalFilePath);
            }
            catch (Exception)
            {
                // If we can't read the file, continue with empty source
            }

            string className = Path.GetFileNameWithoutExtension(originalFilePath);
            
            return new TestContext
            {
                Namespace = GetTargetNamespace(targetFilePath),
                ClassName = className,
                Usings = GetUsings(originalFilePath),
                Dependencies = AnalyzeDependencies(originalFilePath),
                Methods = ExtractPublicMethods(sourceCode),
                Properties = ExtractProperties(sourceCode),
                ClassType = DetermineClassType(sourceCode, className),
                ClassAttributes = ExtractAttributes(sourceCode, className)
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

                    if (trimmedLine.StartsWith("using") && trimmedLine.EndsWith(";"))
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
                    
                    // Remove any remaining characters that aren't part of the parameter name
                    name = new string(name.TakeWhile(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

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

        private Method[] ExtractPublicMethods(string sourceCode)
        {
            var methods = new List<Method>();
            
            if (string.IsNullOrEmpty(sourceCode))
                return methods.ToArray();

            try
            {
                // Use more sophisticated regex pattern to handle more complex method signatures
                var regex = new Regex(@"(?:public|internal)\s+(?:virtual\s+)?(?:override\s+)?(?:abstract\s+)?(?:async\s+)?(?:[a-zA-Z0-9_.<>]+\s+)?([\w<>\.]+)\s+([\w]+)\s*\(([^)]*)\)(?:\s*=>\s*[^;]+)?(?:\s*{|;)", 
                    RegexOptions.Compiled | RegexOptions.Multiline);
                
                var matches = regex.Matches(sourceCode);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        string returnType = match.Groups[1].Value.Trim();
                        string methodName = match.Groups[2].Value.Trim();
                        string parameters = match.Groups[3].Value.Trim();
                        
                        // Skip constructors and properties
                        if (!methodName.Equals(Path.GetFileNameWithoutExtension(sourceCode), StringComparison.OrdinalIgnoreCase) 
                            && !IsProbablyPropertyAccessor(methodName))
                        {
                            var method = new Method
                            {
                                Name = methodName,
                                ReturnType = NormalizeReturnType(returnType),
                                Parameters = parameters,
                                IsAsync = sourceCode.Contains("async") && sourceCode.Contains(methodName),
                                ParameterList = ParseParameters(parameters),
                                Attributes = ExtractAttributes(sourceCode, methodName),
                                BodyPatterns = AnalyzeMethodBody(sourceCode, methodName)
                            };
                            
                            // Generate suggested test methods based on method signature
                            method.SuggestedTestMethods = GenerateSuggestedTestMethods(method);
                            
                            methods.Add(method);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log exception and return empty methods array
            }

            return methods.ToArray();
        }

        private bool IsProbablyPropertyAccessor(string methodName)
        {
            return methodName.StartsWith("get_") || methodName.StartsWith("set_");
        }

        private string NormalizeReturnType(string returnType)
        {
            // Handle nullable types properly
            if (returnType.EndsWith("?"))
                return returnType;
                
            // Handle generic return types
            if (returnType.Contains("<") && returnType.Contains(">"))
                return returnType;
                
            // Handle Task<T> for async methods
            if (returnType.StartsWith("Task<") && returnType.EndsWith(">"))
                return returnType.Substring(5, returnType.Length - 6);  // Extract T from Task<T>
                
            // Handle plain Task (void async)
            if (returnType == "Task")
                return "void";
                
            return returnType;
        }

        private MethodParameter[] ParseParameters(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
                return new MethodParameter[0];
                
            var result = new List<MethodParameter>();
            var paramArray = parameters.Split(',');
            
            foreach (var param in paramArray)
            {
                string trimmedParam = param.Trim();
                if (string.IsNullOrWhiteSpace(trimmedParam))
                    continue;
                    
                var parts = trimmedParam.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string paramType = parts[0];
                    string paramName = parts[parts.Length - 1];
                    
                    // Handle out, ref, params keywords
                    bool isOut = false;
                    bool isRef = false;
                    bool isParams = false;
                    
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        if (parts[i] == "out")
                            isOut = true;
                        else if (parts[i] == "ref")
                            isRef = true;
                        else if (parts[i] == "params")
                            isParams = true;
                    }
                    
                    // Clean up parameter name
                    paramName = new string(paramName.TakeWhile(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
                    
                    result.Add(new MethodParameter
                    {
                        Type = paramType,
                        Name = paramName,
                        IsOut = isOut,
                        IsRef = isRef,
                        IsParams = isParams
                    });
                }
            }
            
            return result.ToArray();
        }

        private string[] GenerateSuggestedTestMethods(Method method)
        {
            var suggestions = new List<string>();
            string methodName = method.Name;
            string returnType = method.ReturnType;
            
            // For Get methods
            if (methodName.StartsWith("Get") || methodName.StartsWith("Find") || methodName.StartsWith("Retrieve"))
            {
                suggestions.Add($"{methodName}_WhenValidInput_ReturnsExpectedResult");
                suggestions.Add($"{methodName}_WhenInvalidInput_ReturnsNull");
                
                if (returnType != "void" && returnType != "Task")
                {
                    suggestions.Add($"{methodName}_WhenNotFound_ThrowsNotFoundException");
                }
            }
            
            // For validation methods
            else if (methodName.StartsWith("Validate") || methodName.StartsWith("Is") || methodName.StartsWith("Can") || methodName.StartsWith("Has"))
            {
                suggestions.Add($"{methodName}_WhenValid_ReturnsTrue");
                suggestions.Add($"{methodName}_WhenInvalid_ReturnsFalse");
            }
            
            // For Create/Update methods
            else if (methodName.StartsWith("Create") || methodName.StartsWith("Update") || methodName.StartsWith("Save"))
            {
                suggestions.Add($"{methodName}_WhenValidInput_Succeeds");
                suggestions.Add($"{methodName}_WhenInvalidInput_ThrowsException");
                
                if (returnType != "void" && returnType != "Task")
                {
                    suggestions.Add($"{methodName}_WhenSuccessful_ReturnsCreatedEntity");
                }
            }
            
            // For Delete methods
            else if (methodName.StartsWith("Delete") || methodName.StartsWith("Remove"))
            {
                suggestions.Add($"{methodName}_WhenValidId_Succeeds");
                suggestions.Add($"{methodName}_WhenInvalidId_ThrowsException");
                suggestions.Add($"{methodName}_WhenEntityNotFound_ThrowsNotFoundException");
            }
            
            // For Process methods
            else if (methodName.StartsWith("Process") || methodName.StartsWith("Handle") || methodName.StartsWith("Execute"))
            {
                suggestions.Add($"{methodName}_WhenValidInput_CompletesSuccessfully");
                suggestions.Add($"{methodName}_WhenInvalidInput_ThrowsException");
                
                if (returnType != "void" && returnType != "Task")
                {
                    suggestions.Add($"{methodName}_WhenProcessed_ReturnsExpectedResult");
                }
            }
            
            // Add at least one generic test method if no specific suggestions
            if (suggestions.Count == 0)
            {
                suggestions.Add($"{methodName}_WhenCalled_PerformsExpectedAction");
                
                if (returnType != "void" && returnType != "Task")
                {
                    suggestions.Add($"{methodName}_WhenCalled_ReturnsExpectedResult");
                }
            }
            
            return suggestions.ToArray();
        }

        private ClassType DetermineClassType(string sourceCode, string className)
        {
            // Default to unknown
            ClassType classType = ClassType.Unknown;
            
            // Look for interfaces implemented
            var interfaceRegex = new Regex($"class\\s+{className}\\s*:\\s*([^{{]+)");
            var interfaceMatch = interfaceRegex.Match(sourceCode);
            
            if (interfaceMatch.Success)
            {
                string implementsList = interfaceMatch.Groups[1].Value;
                
                if (implementsList.Contains("Controller") || implementsList.Contains("ControllerBase"))
                    return ClassType.Controller;
                    
                if (implementsList.Contains("IRepository") || implementsList.Contains("Repository"))
                    return ClassType.Repository;
                    
                if (implementsList.Contains("IService") || implementsList.Contains("Service"))
                    return ClassType.Service;
                    
                if (implementsList.Contains("IValidator") || implementsList.Contains("Validator"))
                    return ClassType.Validator;
            }
            
            // If not determined by interfaces, use naming convention
            if (className.EndsWith("Controller"))
                classType = ClassType.Controller;
            else if (className.EndsWith("Repository"))
                classType = ClassType.Repository;
            else if (className.EndsWith("Service"))
                classType = ClassType.Service;
            else if (className.EndsWith("Validator"))
                classType = ClassType.Validator;
            else if (className.EndsWith("Factory"))
                classType = ClassType.Factory;
            else if (className.EndsWith("Provider"))
                classType = ClassType.Provider;
            else if (className.EndsWith("Manager"))
                classType = ClassType.Manager;
            else if (className.EndsWith("Handler"))
                classType = ClassType.Handler;
            
            return classType;
        }

        private MethodBodyPatterns AnalyzeMethodBody(string sourceCode, string methodName)
        {
            MethodBodyPatterns patterns = new MethodBodyPatterns();
            
            // Extract method body for further analysis
            var methodBodyRegex = new Regex($"{methodName}\\s*\\([^)]*\\)\\s*{{([^}}]*)}}", RegexOptions.Singleline);
            var methodBodyMatch = methodBodyRegex.Match(sourceCode);
            
            if (methodBodyMatch.Success)
            {
                string methodBody = methodBodyMatch.Groups[1].Value;
                
                // Check if method uses database operations
                patterns.UsesDatabaseOperations = methodBody.Contains("ExecuteQuery") || 
                                                methodBody.Contains(".Connection") ||
                                                methodBody.Contains("DbContext") ||
                                                methodBody.Contains("Repository.");
                
                // Check if method performs validations
                patterns.PerformsValidation = methodBody.Contains("if (") && 
                                            (methodBody.Contains("throw new") || 
                                             methodBody.Contains("return false") ||
                                             methodBody.Contains("return null"));
                
                // Check if method has external dependencies
                patterns.HasExternalDependencies = methodBody.Contains("HttpClient") ||
                                                 methodBody.Contains("WebClient") ||
                                                 methodBody.Contains("ApiClient");
                
                // Check if method does file operations
                patterns.UsesFileOperations = methodBody.Contains("File.") ||
                                            methodBody.Contains("Directory.") ||
                                            methodBody.Contains("Stream");
            }
            
            return patterns;
        }

        private Property[] ExtractProperties(string sourceCode)
        {
            var properties = new List<Property>();
            
            // Regex for properties
            var regex = new Regex(@"public\s+(?:virtual\s+)?(?:override\s+)?([a-zA-Z0-9_.<>]+)\s+([a-zA-Z0-9_]+)\s*{\s*get;\s*(?:private\s+)?(?:set;)?", 
                                RegexOptions.Compiled | RegexOptions.Multiline);
            
            var matches = regex.Matches(sourceCode);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    string propType = match.Groups[1].Value.Trim();
                    string propName = match.Groups[2].Value.Trim();
                    
                    properties.Add(new Property
                    {
                        Name = propName,
                        Type = propType,
                        IsCollection = propType.Contains("IEnumerable") || 
                                     propType.Contains("ICollection") || 
                                     propType.Contains("IList") || 
                                     propType.Contains("List<") ||
                                     propType.Contains("[]")
                    });
                }
            }
            
            return properties.ToArray();
        }

        private string[] ExtractAttributes(string sourceCode, string elementName)
        {
            var attributes = new List<string>();
            
            // Look for attributes before the element definition
            var attributeRegex = new Regex($@"\[([^\]]+)\](?:\s*)*(?:public|internal)\s+(?:.*{elementName}\s*\(|\s+{elementName}\s*{{)", 
                                        RegexOptions.Compiled | RegexOptions.Multiline);
            
            var matches = attributeRegex.Matches(sourceCode);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 2)
                {
                    string attribute = match.Groups[1].Value.Trim();
                    attributes.Add(attribute);
                }
            }
            
            return attributes.ToArray();
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
    }
}