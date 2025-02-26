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
                    methods.AddRange(GenerateRepositoryIntegrationTests(className));
                    break;
                case ClassType.Service:
                    methods.AddRange(GenerateServiceIntegrationTests(className));
                    break;
                case ClassType.Controller:
                    methods.AddRange(GenerateControllerIntegrationTests(className));
                    break;
                case ClassType.Validator:
                    methods.AddRange(GenerateValidatorIntegrationTests(className));
                    break;
                case ClassType.Factory:
                case ClassType.Provider:
                case ClassType.Manager:
                case ClassType.Handler:
                    methods.AddRange(GenerateComponentIntegrationTests(className));
                    break;
            }
            
            // Add tests based on method analysis and special behaviors
            foreach (var method in context.Methods)
            {
                // Generate integration tests for methods with external dependencies
                if (method.BodyPatterns.HasExternalDependencies)
                {
                    methods.Add(CreateTestMethod($"{method.Name}Integration_WithExternalDependencies_CompletesSuccessfully", method));
                }
                
                // Generate integration tests for methods with file operations
                if (method.BodyPatterns.UsesFileOperations)
                {
                    methods.Add(CreateTestMethod($"{method.Name}Integration_WithFileOperations_ProcessesFilesCorrectly", method));
                }
                
                // Generate integration tests for database operations
                if (method.BodyPatterns.UsesDatabaseOperations)
                {
                    if (method.Name.StartsWith("Get") || method.Name.StartsWith("Find") || method.Name.StartsWith("Retrieve"))
                    {
                        methods.Add(CreateTestMethod($"{method.Name}Integration_WithDataInDatabase_ReturnsPersistentData", method));
                    }
                    else if (method.Name.StartsWith("Create") || method.Name.StartsWith("Insert"))
                    {
                        methods.Add(CreateTestMethod($"{method.Name}Integration_WithValidEntity_PersistsToDatabase", method));
                    }
                    else if (method.Name.StartsWith("Update"))
                    {
                        methods.Add(CreateTestMethod($"{method.Name}Integration_WithValidEntity_UpdatesDatabaseRecord", method));
                    }
                    else if (method.Name.StartsWith("Delete") || method.Name.StartsWith("Remove"))
                    {
                        methods.Add(CreateTestMethod($"{method.Name}Integration_WithValidId_RemovesRecordFromDatabase", method));
                    }
                }
            }
            
            // If we have no tests yet, add a default integration test
            if (methods.Count == 0)
            {
                methods.Add(CreateTestMethod($"{className}Integration_BasicIntegrationScenario_CompletesSuccessfully"));
            }
            
            return methods.ToArray();
        }

        private object[] GenerateRepositoryIntegrationTests(string className)
        {
            return new[]
            {
                CreateTestMethod("GetIntegration_WhenValidId_ReturnsEntityWithCorrectRelations"),
                CreateTestMethod("CreateIntegration_WhenValidEntity_SuccessfullyPersists"),
                CreateTestMethod("UpdateIntegration_WhenValidEntity_SuccessfullyModifiesData"),
                CreateTestMethod("DeleteIntegration_WhenValidId_RemovesAllRelatedData"),
                CreateTestMethod("IntegrationTest_TransactionRollback_PreservesDataIntegrity")
            };
        }

        private object[] GenerateServiceIntegrationTests(string className)
        {
            return new[]
            {
                CreateTestMethod("ProcessIntegration_WhenCalledWithValidData_UpdatesMultipleSystems"),
                CreateTestMethod("HandleIntegration_WhenValidRequest_ReturnsExpectedResponse"),
                CreateTestMethod("ProcessIntegration_WithInvalidPermissions_ReturnsAuthorizationError"),
                CreateTestMethod("IntegrationTest_EndToEndFlow_CompletesSuccessfully")
            };
        }

        private object[] GenerateControllerIntegrationTests(string className)
        {
            return new[]
            {
                CreateTestMethod("GetEndpointIntegration_WhenCalled_ReturnsCorrectDataFromDatabase"),
                CreateTestMethod("PostEndpointIntegration_WithValidData_CreatesResourceAndReturnsLocation"),
                CreateTestMethod("PutEndpointIntegration_WithValidData_UpdatesResource"),
                CreateTestMethod("DeleteEndpointIntegration_WithValidId_RemovesResource"),
                CreateTestMethod("IntegrationTest_ApiFlow_CompletesEndToEndScenario")
            };
        }

        private object[] GenerateValidatorIntegrationTests(string className)
        {
            return new[]
            {
                CreateTestMethod("ValidateIntegration_WithRealWorldData_ValidatesCorrectly"),
                CreateTestMethod("ValidationIntegration_WithExternalRules_AppliesAllConstraints")
            };
        }

        private object[] GenerateComponentIntegrationTests(string className)
        {
            return new[]
            {
                CreateTestMethod($"{className}Integration_WithRealComponents_WorksCorrectly"),
                CreateTestMethod($"{className}Integration_CompleteWorkflow_SuccessfullyProcesses")
            };
        }
    }
}