using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace MarkdownComments
{
    static class MarkdownCommentsGuids
    {
        public const string guidPackageString = "5e2d0567-b03d-4629-80e8-27c1fa34d247";

        public static readonly Guid guidCommandSet = new Guid("81a44082-1101-4dac-86bd-dbfbbd9dcc5e");
    };

    static class MarkdownCommentsCommandIds
    {
        public const int cmdidEnable = 0x100;
        public const int cmdidShowImages = 0x101;
        public const int cmdidHideDelimiters = 0x102;
    };

    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "0.3", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPageAttribute(typeof(MarkdownCommentsOptionsPage), "MarkdownComments", "General", 101, 106, true)]
    [ProvideProfileAttribute(typeof(MarkdownCommentsOptionsPage), "MarkdownComments", "General", 101, 106, true, DescriptionResourceID = 101)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(MarkdownCommentsGuids.guidPackageString)]
    public sealed class MarkdownCommentsPackage : Package
    {
        private Dictionary<int, MarkdownCommentsOptions> _optionByCommandId;

        public MarkdownCommentsPackage()
        {
            _optionByCommandId = new Dictionary<int, MarkdownCommentsOptions>();
            _optionByCommandId.Add(MarkdownCommentsCommandIds.cmdidEnable, MarkdownCommentsOptions.EnableMarkdownComments);
            _optionByCommandId.Add(MarkdownCommentsCommandIds.cmdidShowImages, MarkdownCommentsOptions.ShowImages);
            _optionByCommandId.Add(MarkdownCommentsCommandIds.cmdidHideDelimiters, MarkdownCommentsOptions.HideDelimiters);
        }

        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                // Create the command for the menu item.
                foreach(KeyValuePair<int, MarkdownCommentsOptions> pair in _optionByCommandId)
                {
                    CommandID menuCommandID = new CommandID(MarkdownCommentsGuids.guidCommandSet, pair.Key);
                    OleMenuCommand menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                    menuItem.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                    mcs.AddCommand(menuItem);
                }
            }

            MarkdownCommentsFactory.Package = this;

            var options = GetDialogPage(typeof(MarkdownCommentsOptionsPage)) as MarkdownCommentsOptionsPage;
            options.OptionsChanged += OnOptionsChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (MarkdownCommentsFactory.Package == this)
            {
                MarkdownCommentsFactory.Package = null;
            }

            base.Dispose(disposing);
        }

        public MarkdownCommentsOptionsPage GetOptions()
        {
            return GetDialogPage(typeof(MarkdownCommentsOptionsPage)) as MarkdownCommentsOptionsPage;
        }

        public event EventHandler<MarkdownCommentsOptionsChanged> OptionsChanged;

        protected void OnOptionsChanged(object sender, MarkdownCommentsOptionsChanged e)
        {
            if(OptionsChanged != null)
            {
                OptionsChanged(sender, e);
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var myCommand = sender as OleMenuCommand;
            if (myCommand != null)
            {
                var options = GetOptions();
                if (options != null)
                {
                    if (myCommand.CommandID.Guid == MarkdownCommentsGuids.guidCommandSet)
                    {
                        MarkdownCommentsOptions option;
                        if(_optionByCommandId.TryGetValue(myCommand.CommandID.ID, out option))
                        {
                            options[option] = !options[option];
                            options.SaveSettingsToStorage();
                        }
                    }
                }
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var myCommand = sender as OleMenuCommand;
            if (myCommand != null)
            {
                var options = GetOptions();
                if (options != null)
                {
                    if(myCommand.CommandID.Guid == MarkdownCommentsGuids.guidCommandSet)
                    {
                        MarkdownCommentsOptions option;
                        if (_optionByCommandId.TryGetValue(myCommand.CommandID.ID, out option))
                        {
                            myCommand.Checked = options[option];
                        }
                    }
                }
            }
        }

    }
}
