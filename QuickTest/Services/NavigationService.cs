using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using QuickTest.Utilities;

namespace QuickTest.Services
{
    internal class NavigationService
    {
        private readonly MessageService messageService;
        private readonly FileCreationService fileCreationService;

        public NavigationService(MessageService messageService)
        {
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.fileCreationService = new FileCreationService(messageService);
        }

        public void NavigateToEquivalentFile(string currentFilePath, string targetSuffix = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(currentFilePath))
                throw new ArgumentNullException(nameof(currentFilePath));

            string targetFilePath = FileUtility.GetEquivalentFilePath(currentFilePath, targetSuffix);

            if (FileUtility.FileExists(targetFilePath))
            {
                OpenFileInVisualStudio(targetFilePath);
                return;
            }

            // Perform a fallback search recursively to find if the file is available at any other location
            var foundFilePath = SearchForFile(currentFilePath, targetFilePath, targetSuffix);
            if (foundFilePath != null)
            {
                messageService.ShowWarning(
                    $"The file was not found in the expected location but was found at:\n{foundFilePath}");
                OpenFileInVisualStudio(foundFilePath);
                return;
            }

            // If no file exists prompt creation of a new one
            fileCreationService.CreateFileWithTemplate(targetFilePath, targetSuffix ?? "Original");
        }

        private string SearchForFile(string currentFilePath, string targetFilePath, string targetSuffix)
        {
            string baseDirectory = FileUtility.GetBaseDirectory();
            string searchDirectory = GetTargetDirectory(baseDirectory, currentFilePath, targetSuffix);

            if (searchDirectory != null)
            {
                return FileUtility.SearchFileRecursively(searchDirectory, Path.GetFileName(targetFilePath));
            }

            return null;
        }

        private static string GetTargetDirectory(string baseDirectory, string currentFilePath, string targetSuffix)
        {
            if (string.IsNullOrEmpty(baseDirectory) || string.IsNullOrEmpty(currentFilePath))
                return null;

            string relativePath = FileUtility.GetRelativePath(baseDirectory, currentFilePath);
            string[] pathSegments = relativePath.Split(Path.DirectorySeparatorChar);

            if (pathSegments.Length < 2)
                return null;

            UpdateProjectFolder(ref pathSegments[0], targetSuffix);

            return Path.Combine(baseDirectory, pathSegments[0]);
        }

        private static void UpdateProjectFolder(ref string folder, string targetSuffix)
        {
            if (targetSuffix == null)
            {
                RemoveTestSuffix(ref folder);
            }
            else
            {
                AddTestSuffix(ref folder, targetSuffix);
            }
        }

        private static void RemoveTestSuffix(ref string folder)
        {
            if (folder.EndsWith(".Unit.Tests"))
                folder = folder.Replace(".Unit.Tests", string.Empty);
            else if (folder.EndsWith(".Integration.Tests"))
                folder = folder.Replace(".Integration.Tests", string.Empty);
        }

        private static void AddTestSuffix(ref string folder, string targetSuffix)
        {
            if (folder.EndsWith(".Unit.Tests") || folder.EndsWith(".Integration.Tests"))
                folder = folder.Substring(0, folder.LastIndexOf('.')) + $".{targetSuffix}";
            else
                folder = $"{folder}.{targetSuffix}";
        }

        private static void OpenFileInVisualStudio(string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE dte = (EnvDTE.DTE)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE));
            dte.ItemOperations.OpenFile(filePath);
        }
    }
}
