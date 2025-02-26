using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using QuickTest.Utilities;
using System;

namespace QuickTest.Services
{
    internal class CommandService
    {
        private readonly AsyncPackage package;
        private readonly MessageService messageService;
        private readonly NavigationService navigationService;
        private const string UnitTestSuffix = "Unit.Tests";
        private const string IntegrationTestSuffix = "Integration.Tests";

        public CommandService(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.messageService = new MessageService();
            this.navigationService = new NavigationService(messageService);
        }

        public void ExecuteUnitTest() => ExecuteCommand(UnitTestSuffix);

        public void ExecuteIntegrationTest() => ExecuteCommand(IntegrationTestSuffix);

        public void ExecuteOriginalClass() => ExecuteCommand(null);

        private void ExecuteCommand(string targetSuffix)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentFilePath = GetValidatedFilePath();
            if (currentFilePath == null) return;

            navigationService.NavigateToEquivalentFile(currentFilePath, targetSuffix);
        }

        private string GetValidatedFilePath()
        {
            string currentFilePath = FileUtility.GetCurrentFilePath();

            if (string.IsNullOrEmpty(currentFilePath) || currentFilePath == "No active file")
            {
                messageService.ShowError("Unable to determine the current file path. Please ensure a file is selected.");
                return null;
            }

            return currentFilePath;
        }
    }
}
