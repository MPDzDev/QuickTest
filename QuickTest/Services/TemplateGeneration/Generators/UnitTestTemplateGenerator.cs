using QuickTest.Models;
using QuickTest.Services.TemplateGeneration.Generators.Base;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                ["tests"] = GenerateTestMethods(context)
            };
        }

        private object[] GenerateTestMethods(TestContext context)
        {
            var methods = new List<object>();
            var className = context.ClassName;
            var classType = context.ClassType;
            
            // First, generate tests based on class type
            switch (classType)
            {
                case ClassType.Repository:
                    methods.AddRange(GenerateRepositoryTests(className));
                    break;
                case ClassType.Service:
                    methods.AddRange(GenerateServiceTests(className));
                    break;
                case ClassType.Controller:
                    methods.AddRange(GenerateControllerTests(className));
                    break;
                case ClassType.Validator:
                    methods.AddRange(GenerateValidatorTests(className));
                    break;
                case ClassType.Factory:
                    methods.AddRange(GenerateFactoryTests(className));
                    break;
                case ClassType.Provider:
                    methods.AddRange(GenerateProviderTests(className));
                    break;
                case ClassType.Manager:
                case ClassType.Handler:
                    methods.AddRange(GenerateManagerTests(className));
                    break;
            }
            
            // Then add tests based on method analysis
            foreach (var method in context.Methods)
            {
                // Skip methods that already have tests
                if (methods.Any(m => ((dynamic)m).name.Contains(method.Name)))
                    continue;
                
                // Add tests based on method signature and body analysis
                if (method.BodyPatterns.UsesDatabaseOperations)
                {
                    methods.Add(CreateTestMethod($"{method.Name}_WithValidDataInDatabase_PerformsCorrectQuery", method));
                }
                
                if (method.BodyPatterns.PerformsValidation)
                {
                    methods.Add(CreateTestMethod($"{method.Name}_WhenValidationFails_ThrowsException", method));
                }
                
                // Add tests based on suggested test methods
                foreach (var suggestion in method.SuggestedTestMethods)
                {
                    if (!methods.Any(m => ((dynamic)m).name == suggestion))
                    {
                        methods.Add(CreateTestMethod(suggestion, method));
                    }
                }
            }
            
            // If we still have no tests, add a generic one
            if (methods.Count == 0)
            {
                methods.Add(CreateTestMethod($"{className}_BasicFunctionality_WorksAsExpected"));
            }
            
            return methods.ToArray();
        }

        private object[] GenerateRepositoryTests(string className)
        {
            return new[]
            {
                CreateTestMethod("Get_WhenValidId_ReturnsEntity"),
                CreateTestMethod("Create_WhenValidEntity_Succeeds"),
                CreateTestMethod("Update_WhenValidEntity_Succeeds"),
                CreateTestMethod("Delete_WhenValidId_Succeeds"),
                CreateTestMethod("Get_WhenInvalidId_ReturnsNull"),
                CreateTestMethod("Create_WhenInvalidEntity_ThrowsException")
            };
        }

        private object[] GenerateServiceTests(string className)
        {
            return new[]
            {
                CreateTestMethod("Process_WhenValidInput_ReturnsExpectedResult"),
                CreateTestMethod("Process_WhenInvalidInput_ThrowsException"),
                CreateTestMethod("Validate_WhenValidData_ReturnsTrue"),
                CreateTestMethod("Validate_WhenInvalidData_ReturnsFalse")
            };
        }

        private object[] GenerateControllerTests(string className)
        {
            return new[]
            {
                CreateTestMethod("Get_WhenValidRequest_ReturnsOkResult"),
                CreateTestMethod("Post_WhenValidModel_CreatesResource"),
                CreateTestMethod("Put_WhenValidUpdate_ModifiesResource"),
                CreateTestMethod("Delete_WhenValidId_RemovesResource"),
                CreateTestMethod("Get_WhenInvalidRequest_ReturnsBadRequest")
            };
        }

        private object[] GenerateValidatorTests(string className)
        {
            return new[]
            {
                CreateTestMethod("Validate_WhenAllPropertiesValid_ReturnsTrue"),
                CreateTestMethod("Validate_WhenRequiredPropertyMissing_ReturnsFalse"),
                CreateTestMethod("Validate_WhenValueExceedsMaximum_ReturnsFalse")
            };
        }

        private object[] GenerateFactoryTests(string className)
        {
            return new[]
            {
                CreateTestMethod("Create_WithValidParameters_ReturnsCorrectType"),
                CreateTestMethod("Create_WithInvalidParameters_ThrowsException")
            };
        }

        private object[] GenerateProviderTests(string className)
        {
            return new[]
            {
                CreateTestMethod("GetData_WhenValidRequest_ReturnsExpectedData"),
                CreateTestMethod("GetData_WhenInvalidRequest_ThrowsException")
            };
        }

        private object[] GenerateManagerTests(string className)
        {
            return new[]
            {
                CreateTestMethod("Process_WhenValidInput_CompletesSuccessfully"),
                CreateTestMethod("Handle_WhenValidRequest_ReturnsExpectedResponse"),
                CreateTestMethod("Execute_WithInvalidParameters_ThrowsException")
            };
        }
    }
}