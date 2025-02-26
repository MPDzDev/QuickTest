using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace QuickTest.Services
{
    internal class MessageService
    {
        private const string DefaultTitle = "Quick Test";

        public void ShowError(string message, string title = DefaultTitle + " Error")
        {
            ShowMessage(message, title, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        public void ShowWarning(string message, string title = DefaultTitle)
        {
            ShowMessage(message, title, OLEMSGICON.OLEMSGICON_WARNING);
        }

        private void ShowMessage(string message, string title, OLEMSGICON icon)
        {
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                title,
                icon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
