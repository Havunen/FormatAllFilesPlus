using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FormatAllFilesPlus
{
    internal abstract class CommandBase
    {
        protected IServiceProvider ServiceProvider
        {
            get { return Package; }
        }

        protected Package Package { get; private set; }

        protected CommandBase(Package package, int commandId, Guid commandSetId)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            Package = package;

            IMenuCommandService commandService = (IMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(commandSetId, commandId);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        protected abstract void Execute(object sender, EventArgs e);

        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                Execute(sender, e);
            }
            catch (Exception ex)
            {
                ShowMessageBox(
                    string.Format(CultureInfo.CurrentCulture, "{0} is not executable.", GetType().Name),
                    string.Format(CultureInfo.CurrentCulture, "{0}: {1}.", ex.GetType().FullName, ex.Message),
                    OLEMSGICON.OLEMSGICON_WARNING);
            }
        }

        private void ShowMessageBox(string title, string message, OLEMSGICON icon)
        {
            VsShellUtilities.ShowMessageBox(
                ServiceProvider,
                message,
                title,
                icon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
