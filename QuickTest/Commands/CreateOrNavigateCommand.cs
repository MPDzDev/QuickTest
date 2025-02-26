using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using QuickTest.Services;

namespace QuickTest.Commands
{
    internal sealed class CreateOrNavigateCommand
    {
        private readonly AsyncPackage package;
        private readonly CommandService commandService;
        private static CreateOrNavigateCommand instance;

        private CreateOrNavigateCommand(AsyncPackage package, OleMenuCommandService menuCommandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.commandService = new CommandService(package);

            if (menuCommandService == null)
                throw new ArgumentNullException(nameof(menuCommandService));

            InitializeCommands(menuCommandService);
        }

        public static CreateOrNavigateCommand Instance => instance;

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            
            var menuCommandService = await package.GetServiceAsync(typeof(IMenuCommandService)) 
                as OleMenuCommandService;
                
            instance = new CreateOrNavigateCommand(package, menuCommandService);
        }

        private void InitializeCommands(OleMenuCommandService menuCommandService)
        {
            var commands = new (int CommandId, Action Handler)[]
            {
                (CommandConstants.UnitTestCommandId, commandService.ExecuteUnitTest),
                (CommandConstants.IntegrationTestCommandId, commandService.ExecuteIntegrationTest),
                (CommandConstants.OriginalClassCommandId, commandService.ExecuteOriginalClass)
            };

            foreach (var (commandId, action) in commands)
            {
                RegisterCommand(menuCommandService, commandId, action);
            }
        }

        private void RegisterCommand(OleMenuCommandService menuCommandService, int commandId, Action action)
        {
            var menuCommandID = new CommandID(CommandConstants.CommandSet, commandId);
            var menuItem = new MenuCommand((s, e) => action(), menuCommandID);
            menuCommandService.AddCommand(menuItem);
        }
    }
}
