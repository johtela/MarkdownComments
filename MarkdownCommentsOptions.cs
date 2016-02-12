using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
        [AttributeUsage(AttributeTargets.Property)]
        internal class MarkdownCommentsOptionAttribute : Attribute
        {
            public MarkdownCommentsOptions Option;

            public MarkdownCommentsOptionAttribute(MarkdownCommentsOptions option)
            {
                Option = option;
            }
        }

        static Dictionary<MarkdownCommentsOptions, PropertyInfo> _propertyInfoByOption;
        static MarkdownCommentsOptionsPage()
        {

            _propertyInfoByOption = typeof(MarkdownCommentsOptionsPage).GetProperties()
                .Where(propertyInfo => Attribute.IsDefined(propertyInfo, typeof(MarkdownCommentsOptionAttribute)))
                .Select(propertyInfo => new { Key = (Attribute.GetCustomAttribute(propertyInfo, typeof(MarkdownCommentsOptionAttribute)) as MarkdownCommentsOptionAttribute).Option, Value = propertyInfo })
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private bool optionEnable = true;
        private bool optionShowImages = true;
        private bool optionHideDelimiters = true;
        private bool optionSkipPreprocessor = true;

        [Category("Global")]
        [DisplayName("Enable MarkdownComments")]
        [MarkdownCommentsOption(MarkdownCommentsOptions.EnableMarkdownComments)]
        public bool OptionEnableMarkdownComments { get { return optionEnable; } set { optionEnable = value; NotifyOptionsChanged(MarkdownCommentsOptions.EnableMarkdownComments); } }

        [Category("Features")]
        [DisplayName("Show Images")]
        [MarkdownCommentsOption(MarkdownCommentsOptions.ShowImages)]
        public bool OptionShowImages { get { return optionShowImages; } set { optionShowImages = value; NotifyOptionsChanged(MarkdownCommentsOptions.ShowImages); } }

        [Category("Features")]
        [DisplayName("Hide Delimiters")]
        [MarkdownCommentsOption(MarkdownCommentsOptions.HideDelimiters)]
        public bool OptionHideDelimiters { get { return optionHideDelimiters; } set { optionHideDelimiters = value; NotifyOptionsChanged(MarkdownCommentsOptions.HideDelimiters); } }

        [Category("Features")]
        [DisplayName("Skip C-style Preprocessor")]
        [MarkdownCommentsOption(MarkdownCommentsOptions.SkipPreprocessor)]
        public bool OptionSkipPreprocessor { get { return optionSkipPreprocessor; } set { optionSkipPreprocessor = value; NotifyOptionsChanged(MarkdownCommentsOptions.SkipPreprocessor); } }

        public bool this[MarkdownCommentsOptions option]
        {
            get
            {
                PropertyInfo propertyInfo;
                if(_propertyInfoByOption.TryGetValue(option, out propertyInfo) && propertyInfo.PropertyType == typeof(bool))
                    return (propertyInfo.GetValue(this) as Nullable<bool>).Value;
                else
                    throw new ArgumentException(String.Format("No bool property with option {0} found.", option.ToString()));
            }
            set
            {
                PropertyInfo propertyInfo;
                if (_propertyInfoByOption.TryGetValue(option, out propertyInfo) && propertyInfo.PropertyType == typeof(bool))
                    propertyInfo.SetValue(this, value);
                else
                    throw new ArgumentException(String.Format("No bool property with option {0} found.", option.ToString()));
            }
        }

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
            //e.ApplyBehavior = ApplyKind.Cancel;
            base.OnApply(e);
        }
        #endregion Event Handlers
    }
}
