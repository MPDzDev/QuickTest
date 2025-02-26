using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using QuickTest.VisualStudio;
using System.Collections.Generic;

namespace QuickTest.Utilities
{
    internal static class FileUtility
    {
        #region Visual Studio Integration
        public static string GetCurrentFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            using (var vsSelection = new VSSelectionScope())
            {
                if (vsSelection.IsValidSelection && vsSelection.Hierarchy is IVsProject project)
                {
                    project.GetMkDocument(vsSelection.ItemId, out string filePath);
                    return filePath ?? "Unknown file path";
                }
                return "No active file";
            }
        }

        public static string GetBaseDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE dte = (EnvDTE.DTE)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE));
            return dte?.Solution?.FullName != null ? Path.GetDirectoryName(dte.Solution.FullName) : null;
        }
        #endregion

        #region Path Operations
        public static string GetEquivalentFilePath(string currentFilePath, string targetSuffix = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string baseDirectory = GetBaseDirectory();
            if (!IsValidBasePath(baseDirectory, currentFilePath))
                return null;

            string relativePath = GetRelativePath(baseDirectory, currentFilePath);
            var pathSegments = relativePath.Split(Path.DirectorySeparatorChar);

            if (pathSegments.Length < 2)
                return null;

            UpdateProjectFolder(ref pathSegments, targetSuffix);
            UpdateFileName(ref pathSegments, currentFilePath, targetSuffix);

            return Path.Combine(baseDirectory, Path.Combine(pathSegments));
        }

        private static bool IsValidBasePath(string baseDirectory, string currentFilePath)
        {
            return baseDirectory != null &&
                   currentFilePath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static void UpdateProjectFolder(ref string[] pathSegments, string targetSuffix)
        {
            RemoveTestSuffix(ref pathSegments[0]);

            if (targetSuffix != null)
            {
                pathSegments[0] = $"{pathSegments[0]}.{targetSuffix}";
            }
        }

        private static void RemoveTestSuffix(ref string folder)
        {
            if (folder.EndsWith(".Unit.Tests"))
                folder = folder.Replace(".Unit.Tests", string.Empty);
            else if (folder.EndsWith(".Integration.Tests"))
                folder = folder.Replace(".Integration.Tests", string.Empty);
        }

        private static void UpdateFileName(ref string[] pathSegments, string currentFilePath, string targetSuffix)
        {
            string originalFileName = Path.GetFileNameWithoutExtension(currentFilePath).Replace("Tests", "");
            string fileExtension = Path.GetExtension(currentFilePath);
            string testSuffix = targetSuffix != null ? "Tests" : "";
            pathSegments[pathSegments.Length - 1] = $"{originalFileName}{testSuffix}{fileExtension}";
        }

        public static string GetRelativePath(string basePath, string targetPath)
        {
            ValidatePathParameters(basePath, targetPath);

            Uri baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            Uri targetUri = new Uri(targetPath);

            if (baseUri.Scheme != targetUri.Scheme)
                throw new InvalidOperationException("Paths must have the same scheme.");

            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }
        #endregion

        #region File Operations
        public static bool FileExists(string filePath) =>
            !string.IsNullOrEmpty(filePath) && File.Exists(filePath);

        public static string SearchFileRecursively(string directory, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                    return null;

                return Directory.GetFiles(directory, fileName, SearchOption.AllDirectories)
                              .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public static string GetFileNameWithoutExtension(string filePath) =>
            Path.GetFileNameWithoutExtension(filePath);

        public static string GetFileExtension(string filePath) =>
            Path.GetExtension(filePath);
        #endregion

        #region Helper Methods
        private static void ValidatePathParameters(string basePath, string targetPath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentNullException(nameof(basePath));
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException(nameof(targetPath));
        }

        private static string AppendDirectorySeparatorChar(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? path
                : path + Path.DirectorySeparatorChar;
        #endregion
    }
}
