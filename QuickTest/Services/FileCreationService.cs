using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using QuickTest.Services.TemplateGeneration;
using QuickTest.Utilities;

namespace QuickTest.Services
{
    internal class FileCreationService
    {
        private readonly MessageService messageService;
        private readonly TemplateService templateService;

        public FileCreationService(MessageService messageService)
        {
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.templateService = new TemplateService();
        }

        public void CreateFileWithTemplate(string targetFilePath, string fileType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(targetFilePath))
                throw new ArgumentNullException(nameof(targetFilePath));

            if (!PromptUserForFileCreation(targetFilePath, fileType))
                return;

            try
            {
                CreateFileAndAddToProject(targetFilePath, fileType);
            }
            catch (Exception ex)
            {
                messageService.ShowError($"Failed to create the file: {ex.Message}");
            }
        }

        private void CreateFileAndAddToProject(string targetFilePath, string fileType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnsureDirectoryExists(targetFilePath);
            CreateFileWithContent(targetFilePath, fileType);
            AddFileToDestinationProject(targetFilePath);
            OpenFileInVisualStudio(targetFilePath);
        }

        private static void EnsureDirectoryExists(string targetFilePath)
        {
            string directoryPath = Path.GetDirectoryName(targetFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private void CreateFileWithContent(string targetFilePath, string fileType)
        {
            string originalFilePath = FileUtility.GetEquivalentFilePath(targetFilePath, null);
            string templateContent = templateService.GenerateTestContent(
                originalFilePath,
                targetFilePath,
                fileType);

            File.WriteAllText(targetFilePath, templateContent);
        }

        private bool PromptUserForFileCreation(string targetFilePath, string fileType)
        {
            int result = VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                $"The {fileType} file does not exist. Would you like to create it?\n\n{targetFilePath}",
                "File Not Found",
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            return result == (int)VSConstants.MessageBoxResult.IDYES;
        }

        private void AddFileToDestinationProject(string targetFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = GetDTE();
                string projectPath = FindDestinationProject(dte, targetFilePath);

                if (projectPath == null)
                {
                    messageService.ShowWarning("Could not determine the destination project for the file.");
                    return;
                }

                var project = FindProjectByPath(dte, projectPath);
                if (project != null)
                {
                    project.ProjectItems.AddFromFile(targetFilePath);
                }
            }
            catch (Exception ex)
            {
                messageService.ShowError($"Failed to add the file to the destination project: {ex.Message}");
            }
        }

        private static EnvDTE.DTE GetDTE()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte == null)
            {
                throw new InvalidOperationException("Failed to retrieve DTE service.");
            }
            return dte;
        }

        private static string FindDestinationProject(EnvDTE.DTE dte, string targetFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string bestMatch = null;
            int bestMatchLength = 0;

            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                if (project == null || string.IsNullOrEmpty(project.FullName))
                    continue;

                string projectDirectory = Path.GetDirectoryName(project.FullName);

                if (targetFilePath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase)
                    && projectDirectory.Length > bestMatchLength)
                {
                    bestMatch = project.FullName;
                    bestMatchLength = projectDirectory.Length;
                }
            }

            return bestMatch;
        }

        private static EnvDTE.Project FindProjectByPath(EnvDTE.DTE dte, string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                if (project != null && string.Equals(project.FullName, projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    return project;
                }
            }

            return null;
        }

        private static void OpenFileInVisualStudio(string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE dte = GetDTE();
            dte.ItemOperations.OpenFile(filePath);
        }
    }
}
