using System;
using System.Collections.Generic;

namespace QuickTest.Models
{
    internal class MethodBodyPatterns
    {
        public bool UsesDatabaseOperations { get; set; }
        public bool PerformsValidation { get; set; }
        public bool HasExternalDependencies { get; set; }
        public bool UsesFileOperations { get; set; }
    }
}