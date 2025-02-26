using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickTest.VisualStudio
{
    internal class VSSelectionScope : IDisposable
    {
        private readonly IntPtr hierarchyPointer;
        private readonly IntPtr selectionContainerPointer;
        public readonly uint ItemId;
        public readonly IVsHierarchy Hierarchy;
        public bool IsValidSelection => hierarchyPointer != IntPtr.Zero && ItemId != VSConstants.VSITEMID_NIL;

        public VSSelectionScope()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var monitorSelection = (IVsMonitorSelection)ServiceProvider.GlobalProvider
                .GetService(typeof(SVsShellMonitorSelection));

            hierarchyPointer = IntPtr.Zero;
            selectionContainerPointer = IntPtr.Zero;

            monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                              out uint itemid,
                                              out IVsMultiItemSelect _,
                                              out selectionContainerPointer);

            ItemId = itemid;
            Hierarchy = hierarchyPointer != IntPtr.Zero
                ? Marshal.GetObjectForIUnknown(hierarchyPointer) as IVsHierarchy
                : null;
        }

        public void Dispose()
        {
            if (hierarchyPointer != IntPtr.Zero)
                Marshal.Release(hierarchyPointer);
            if (selectionContainerPointer != IntPtr.Zero)
                Marshal.Release(selectionContainerPointer);
        }
    }
}
