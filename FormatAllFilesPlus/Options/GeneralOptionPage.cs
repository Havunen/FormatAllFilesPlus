using Microsoft.VisualStudio.Shell;

namespace FormatAllFilesPlus.Options
{
    public class GeneralOptionPage : DialogPage
    {
        private readonly GeneralOption _option = new GeneralOption();

        /// <inheritdoc />
        public override object AutomationObject
        {
            get { return _option; }
        }
    }
}
