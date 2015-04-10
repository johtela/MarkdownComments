using System;
using System.Globalization;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace MarkdownComments
{
    public enum MarkdownCommentsOptions
    {
        All = -1,
        None = 0,
        EnableMarkdownComments,
        ShowImages,
        HideDelimiters,
        SkipPreprocessor
    }

    public class MarkdownCommentsOptionsChanged : EventArgs
    {
        public MarkdownCommentsOptionsChanged(MarkdownCommentsOptions option)
        {
            _option = option;
        }

        public bool hasOptionChanged(MarkdownCommentsOptions option)
        {
            return _option == option || _option == MarkdownCommentsOptions.All;
        }

        protected MarkdownCommentsOptions _option = MarkdownCommentsOptions.None;
    }

    /// <summary> 
    // Extends a standard dialog functionality for implementing ToolsOptions pages,  
    // with support for the Visual Studio automation model, Windows Forms, and state  
    // persistence through the Visual Studio settings mechanism. 
    /// </summary> 
    [Guid("10B815DF-ABE2-49BD-AAC4-7A8E090BBC65")]
    [ComVisible(true)]
    public class MarkdownCommentsOptionsPage : DialogPage
    {
        private bool optionEnable = true;
        private bool optionShowImages = true;
        private bool optionHideDelimiters = true;
        private bool optionSkipPreprocessor = true;

        [Category("Global")]
        [Description("Enable MarkdownComments")]
        public bool OptionEnableMarkdownComments { get { return optionEnable; } set { optionEnable = value; NotifyOptionsChanged(MarkdownCommentsOptions.EnableMarkdownComments); } }

        [Category("Features")]
        [Description("Show Images")]
        public bool OptionShowImages { get { return optionShowImages; } set { optionShowImages = value; NotifyOptionsChanged(MarkdownCommentsOptions.ShowImages); } }

        [Category("Features")]
        [Description("Hide Delimiters")]
        public bool OptionHideDelimiters { get { return optionHideDelimiters; } set { optionHideDelimiters = value; NotifyOptionsChanged(MarkdownCommentsOptions.HideDelimiters); } }

        [Category("Features")]
        [Description("Skip C-style Preprocessor")]
        public bool OptionSkipPreprocessor { get { return optionSkipPreprocessor; } set { optionSkipPreprocessor = value; NotifyOptionsChanged(MarkdownCommentsOptions.SkipPreprocessor); } }

        public event EventHandler<MarkdownCommentsOptionsChanged> OptionsChanged;
        void NotifyOptionsChanged(MarkdownCommentsOptions option)
        {
            if(OptionsChanged != null)
            {
                OptionsChanged(this, new MarkdownCommentsOptionsChanged(option));
            }
        }

        #region Event Handlers
        /// <summary> 
        /// Handles "Activate" messages from the Visual Studio environment. 
        /// </summary> 
        /// <devdoc> 
        /// This method is called when Visual Studio wants to activate this page.   
        /// </devdoc> 
        /// <remarks>If the Cancel property of the event is set to true, the page is not activated.</remarks> 
        protected override void OnActivate(CancelEventArgs e)
        {
            //if (false)
            //{
            //    e.Cancel = true;
            //}

            base.OnActivate(e);
        }

        /// <summary> 
        /// Handles "Close" messages from the Visual Studio environment. 
        /// </summary> 
        /// <devdoc> 
        /// This event is raised when the page is closed. 
        /// </devdoc> 
        protected override void OnClosed(EventArgs e)
        {
        }

        /// <summary> 
        /// Handles "Deactive" messages from the Visual Studio environment. 
        /// </summary> 
        /// <devdoc> 
        /// This method is called when VS wants to deactivate this 
        /// page.  If true is set for the Cancel property of the event,  
        /// the page is not deactivated. 
        /// </devdoc> 
        /// <remarks> 
        /// A "Deactive" message is sent when a dialog page's user interface  
        /// window loses focus or is minimized but is not closed. 
        /// </remarks> 
        protected override void OnDeactivate(CancelEventArgs e)
        {
            //if (false)
            //{
            //    e.Cancel = true;
            //}
        }

        /// <summary> 
        /// Handles Apply messages from the Visual Studio environment. 
        /// </summary> 
        /// <devdoc> 
        /// This method is called when VS wants to save the user's  
        /// changes then the dialog is dismissed. 
        /// </devdoc> 
        protected override void OnApply(PageApplyEventArgs e)
        {
            if (false)
            {
                e.ApplyBehavior = ApplyKind.Cancel;
            }
            else
            {
                base.OnApply(e);
            }
        }
        #endregion Event Handlers
    }
}
