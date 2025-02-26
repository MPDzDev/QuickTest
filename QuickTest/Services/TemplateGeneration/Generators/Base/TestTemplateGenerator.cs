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

        protected object CreateTestMethod(string name)
        {
            return new
            {
                name = name,
                arrangeCode = "// TODO: Add arrangement code",
                actCode = "// TODO: Add action code",
                assertCode = "Assert.IsTrue(true);"
            };
        }
    }
}
