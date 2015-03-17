using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownComments
{
    #region Adornment Factory
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class MarkdownCommentsTextAdornmentFactory : IWpfTextViewCreationListener
    {
        [Import]
        IViewClassifierAggregatorService viewClassifierAggregatorService = null;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("MarkdownCommentsTextAdornment")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        /// <summary>
        /// Instantiates a MarkdownCommentsTextAdornment manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTextAdornment>(() => new MarkdownCommentsTextAdornment(textView, viewClassifierAggregatorService));
        }
    }
    #endregion //Adornment Factory

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal sealed class MarkdownCommentsTaggerProvider : IViewTaggerProvider
    {
        [Import]
        IViewClassifierAggregatorService viewClassifierAggregatorService = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            //provide highlighting only on the top buffer 
            if (textView.TextBuffer != buffer)
                return null;

            if (!(textView is IWpfTextView))
                return null;

            return textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTextAdornment>(() => new MarkdownCommentsTextAdornment(textView as IWpfTextView, viewClassifierAggregatorService)) as ITagger<T>;
        }
    }
}
