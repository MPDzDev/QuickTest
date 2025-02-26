using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QuickTest.Models;

namespace QuickTest.Services.TemplateGeneration.Generators.Base
{
    internal abstract class TestTemplateGenerator
    {
        protected readonly string TemplatesDirectory;

        protected TestTemplateGenerator()
        {
            TemplatesDirectory = Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location),
                "Templates"
            );
        }

        public abstract string GenerateContent(TestContext context);

        protected string RenderTemplate(string templateName, Dictionary<string, object> context)
        {
            string templatePath = Path.Combine(TemplatesDirectory, templateName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Template file not found: " + templateName);
            }

            string template = File.ReadAllText(templatePath);

            foreach (KeyValuePair<string, object> kvp in context)
            {
                if (kvp.Value is string)
                {
                    template = template.Replace("{{" + kvp.Key + "}}", kvp.Value.ToString());
                }
            }

            // Handle arrays
            template = ProcessArrays(template, context);

            return CleanupTemplate(template);
        }

        protected virtual string ProcessArrays(string template, Dictionary<string, object> context)
        {
            // Process usings
            if (context["usings"] is string[] usings)
            {
                var usingsText = new StringBuilder();
                foreach (string @using in usings)
                {
                    usingsText.AppendLine($"using {@using};");
                }
                template = template.Replace("{{usings}}", usingsText.ToString());
            }

            // Process dependencies
            if (context["dependencies"] is object[] dependencies)
            {
                template = ProcessDependencies(template, dependencies);
            }

            // Process test methods
            if (context["tests"] is object[] tests)
            {
                template = ProcessTestMethods(template, tests);
            }

            return template;
        }

        protected virtual string ProcessDependencies(string template, object[] dependencies)
        {
            var dependencyFields = new StringBuilder();
            var dependencyInits = new StringBuilder();
            var constructorParams = new StringBuilder();

            foreach (dynamic dependency in dependencies)
            {
                if (dependency.needsSubstitute)
                {
                    dependencyFields.AppendLine($"        private {dependency.interfaceType} _{dependency.name};");
                    dependencyInits.AppendLine($"            _{dependency.name} = Substitute.For<{dependency.interfaceType}>();");
                    constructorParams.Append($"_{dependency.name}, ");
                }
                else
                {
                    constructorParams.Append($"{dependency.defaultValue}, ");
                }
            }

            template = template.Replace("{{dependency_section}}", dependencyFields.ToString().TrimEnd());
            template = template.Replace("{{dependencyInitSection}}", dependencyInits.ToString().TrimEnd());
            template = template.Replace("{{constructorParams}}", constructorParams.ToString().TrimEnd(',', ' '));

            return template;
        }

        protected virtual string ProcessTestMethods(string template, object[] tests)
        {
            var testMethods = new StringBuilder();
            foreach (dynamic test in tests)
            {
                testMethods.AppendLine($@"
        [TestMethod]
        public void {test.name}()
        {{
            // Arrange
            {test.arrangeCode}

            // Act
            {test.actCode}

            // Assert
            {test.assertCode}
        }}");
            }
            return template.Replace("{{testMethods}}", testMethods.ToString());
        }

        protected string CleanupTemplate(string template)
        {
            return string.Join(Environment.NewLine,
                template.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                       .Where(line => !line.Trim().StartsWith("{{") && !line.Trim().EndsWith("}}"))
                       .Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        protected object CreateTestMethod(string name, Method method = null)
        {
            string arrangeCode = "// TODO: Set up test prerequisites";
            string actCode = "// TODO: Execute the operation being tested";
            string assertCode = "// TODO: Verify the expected outcome";
            
            // If we have method context, we can be more specific
            if (method != null)
            {
                // Generate arrange code based on parameters
                var arrangeBuilder = new StringBuilder();
                foreach (var param in method.ParameterList)
                {
                    string defaultValue = GetDefaultValueForType(param.Type);
                    arrangeBuilder.AppendLine($"            var {param.Name} = {defaultValue};");
                }
                
                if (arrangeBuilder.Length > 0)
                    arrangeCode = arrangeBuilder.ToString().TrimEnd();
                    
                // Generate act code
                string paramList = string.Join(", ", method.ParameterList.Select(p => 
                    p.IsOut ? $"out {p.Name}" : 
                    p.IsRef ? $"ref {p.Name}" : 
                    p.Name));
                    
                actCode = method.ReturnType.ToLower() == "void" ? 
                    $"_sut.{method.Name}({paramList});" : 
                    $"var result = _sut.{method.Name}({paramList});";
                    
                // Generate assert code
                if (method.ReturnType.ToLower() != "void")
                {
                    assertCode = "Assert.IsNotNull(result);";
                }
            }
            
            // Exception testing
            if (name.Contains("ThrowsException") || name.Contains("Throws"))
            {
                string exceptionType = "Exception";
                if (name.Contains("NotFoundException"))
                    exceptionType = "NotFoundException";
                else if (name.Contains("ValidationException"))
                    exceptionType = "ValidationException";
                else if (name.Contains("ArgumentException"))
                    exceptionType = "ArgumentException";
                else if (name.Contains("InvalidOperation"))
                    exceptionType = "InvalidOperationException";
                
                actCode = $"// Act & Assert\r\n            var exception = Assert.ThrowsException<{exceptionType}>(() =>\r\n            {{\r\n                {actCode.TrimEnd(';')}\r\n            }});";
                assertCode = "// Additional assertions on exception properties if needed";
            }
            
            return new
            {
                name = name,
                arrangeCode = arrangeCode,
                actCode = actCode,
                assertCode = assertCode
            };
        }

        private string GetDefaultValueForType(string type)
        {
            switch (type.ToLower())
            {
                case "string":
                    return "\"test\"";
                case "int":
                case "int32":
                    return "1";  // More meaningful than 0
                case "long":
                case "int64":
                    return "1L";
                case "bool":
                case "boolean":
                    return "true";  // More meaningful than false
                case "decimal":
                    return "1.0m";
                case "double":
                    return "1.0d";
                case "float":
                    return "1.0f";
                case "datetime":
                    return "DateTime.Now";
                case "guid":
                    return "Guid.NewGuid()";
                case "object":
                    return "new object()";
            }
            
            // Handle collections
            if (type.Contains("IEnumerable<") || type.Contains("ICollection<") || 
                type.Contains("IList<") || type.Contains("List<"))
            {
                string innerType = ExtractGenericType(type);
                return $"new List<{innerType}>()";
            }
            
            if (type.Contains("[]"))
            {
                string arrayType = type.Replace("[]", "");
                return $"new {arrayType}[0]";
            }
            
            // Handle dictionaries
            if (type.Contains("Dictionary<") || type.Contains("IDictionary<"))
            {
                string[] genericTypes = ExtractMultipleGenericTypes(type);
                if (genericTypes.Length == 2)
                {
                    return $"new Dictionary<{genericTypes[0]}, {genericTypes[1]}>()";
                }
            }
            
            return "null";
        }

        private string ExtractGenericType(string type)
        {
            int startIndex = type.IndexOf('<') + 1;
            int endIndex = type.LastIndexOf('>');
            
            if (startIndex > 0 && endIndex > startIndex)
            {
                return type.Substring(startIndex, endIndex - startIndex);
            }
            
            return "object";
        }

        private string[] ExtractMultipleGenericTypes(string type)
        {
            int startIndex = type.IndexOf('<') + 1;
            int endIndex = type.LastIndexOf('>');
            
            if (startIndex > 0 && endIndex > startIndex)
            {
                string genericPart = type.Substring(startIndex, endIndex - startIndex);
                return genericPart.Split(',').Select(t => t.Trim()).ToArray();
            }
            
            return new[] { "object", "object" };
        }
    }
}