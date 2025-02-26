// /Services/TemplateGeneration/Generators/UnitTestTemplateGenerator.cs
using QuickTest.Models;
using QuickTest.Services.TemplateGeneration.Generators.Base;
using System.Collections.Generic;
using System.Linq;

namespace QuickTest.Services.TemplateGeneration.Generators
{
    internal class UnitTestTemplateGenerator : TestTemplateGenerator
    {
        private const string TemplateName = "UnitTest.template";

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
                ["additionalFields"] = GetAdditionalFields(),
                ["setupMethods"] = GetSetupMethods(),
                ["tests"] = GenerateTestMethods(context.ClassName)
            };
        }

        private object[] GetAdditionalFields()
        {
            return new[]
            {
                new
                {
                    type = "string",
                    name = "codDiv",
                    hasInitialValue = true,
                    initialValue = "\"4500\""
                }
            };
        }

        private object[] GetSetupMethods()
        {
            return new[]
            {
                new
                {
                    name = "SetupDecodeEntries",
                    implementation = @"
                    _configurationsTestHelper.SetQtabsEntry(new DecodeEntry(_codDiv, ""REFDAT|Z_ABP_TYPE"", ""BUDGET"", ""Budget for"", 1, ""VARCHAR(30)""));
                    _configurationsTestHelper.SetQtabsEntry(new DecodeEntry(_codDiv, ""Z_ABP_TYPE"", ""ABP"", ""Promotion Type"", 0, ""PROMOTION""));"
                }
            };
        }

        private object[] GenerateTestMethods(string className)
        {
            var methods = new List<object>();

            if (className.EndsWith("Repository"))
            {
                methods.AddRange(new[]
                {
                    CreateTestMethod("Get_WhenValidId_ReturnsEntity"),
                    CreateTestMethod("Create_WhenValidEntity_Succeeds"),
                    CreateTestMethod("Update_WhenValidEntity_Succeeds"),
                    CreateTestMethod("Delete_WhenValidId_Succeeds")
                });
            }
            else if (className.EndsWith("Validator"))
            {
                methods.AddRange(new[]
                {
                    CreateTestMethod("Validate_WhenValid_ReturnsTrue"),
                    CreateTestMethod("Validate_WhenInvalid_ReturnsFalse")
                });
            }

            return methods.ToArray();
        }
    }
}
