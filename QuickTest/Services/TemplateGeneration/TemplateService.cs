// /Services/TemplateGeneration/TemplateService.cs
using Microsoft.VisualStudio.Shell;
using QuickTest.Services.TemplateGeneration.Context;
using QuickTest.Services.TemplateGeneration.Generators;
using QuickTest.Services.TemplateGeneration.Generators.Base;
using System;
using System.Collections.Generic;

namespace QuickTest.Services.TemplateGeneration
{
    internal class TemplateService
    {
        private readonly SourceContextExtractor contextExtractor;
        private readonly Dictionary<string, TestTemplateGenerator> generators;

        public TemplateService()
        {
            contextExtractor = new SourceContextExtractor();
            generators = new Dictionary<string, TestTemplateGenerator>
            {
                { "Unit.Tests", new UnitTestTemplateGenerator() },
                { "Integration.Tests", new IntegrationTestTemplateGenerator() }
            };
        }

        public string GenerateTestContent(string originalFilePath, string targetFilePath, string fileType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(originalFilePath))
                throw new ArgumentNullException(nameof(originalFilePath));
            if (string.IsNullOrEmpty(targetFilePath))
                throw new ArgumentNullException(nameof(targetFilePath));
            if (string.IsNullOrEmpty(fileType))
                throw new ArgumentNullException(nameof(fileType));

            if (!generators.ContainsKey(fileType))
                throw new ArgumentException($"Unknown file type: {fileType}", nameof(fileType));

            var context = contextExtractor.ExtractContext(originalFilePath, targetFilePath);
            return generators[fileType].GenerateContent(context);
        }
    }
}
