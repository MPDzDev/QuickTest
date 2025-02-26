// /Services/TemplateGeneration/Generators/IntegrationTestTemplateGenerator.cs
using QuickTest.Models;
using QuickTest.Services.TemplateGeneration.Generators.Base;
using System.Collections.Generic;
using System.Linq;

namespace QuickTest.Services.TemplateGeneration.Generators
{
    internal class IntegrationTestTemplateGenerator : TestTemplateGenerator
    {
        private const string TemplateName = "IntegrationTest.template";

        public override string GenerateContent(TestContext context)
        {
            var templateContext = CreateTemplateContext(context);
            return RenderTemplate(TemplateName, templateContext);
        }

        private Dictionary<string, object> CreateTemplateContext(TestContext context)
        {
            return new Dictionary<string, object>
            {
                ["namespace"] = context.Namespace,
                ["className"] = context.ClassName,
                ["usings"] = context.Usings,
                ["dependencies"] = context.Dependencies,
                ["tests"] = GenerateTestMethods(context.ClassName)
            };
        }

        private object[] GenerateTestMethods(string className)
        {
            var methods = new List<object>();

            if (className.EndsWith("Repository"))
            {
                methods.AddRange(new[]
                {
                    CreateTestMethod("GetIntegration_WhenValidId_ReturnsEntity"),
                    CreateTestMethod("CreateIntegration_WhenValidEntity_Succeeds"),
                    CreateTestMethod("UpdateIntegration_WhenValidEntity_Succeeds"),
                    CreateTestMethod("DeleteIntegration_WhenValidId_Succeeds")
                });
            }
            else if (className.EndsWith("Service"))
            {
                methods.AddRange(new[]
                {
                    CreateTestMethod("ProcessIntegration_WhenValidInput_Succeeds"),
                    CreateTestMethod("HandleIntegration_WhenValidRequest_ReturnsExpectedResponse")
                });
            }

            return methods.ToArray();
        }
    }
}
