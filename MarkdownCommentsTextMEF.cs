using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownComments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class MarkdownCommentsTaggerFactory : IWpfTextViewCreationListener
    {
        [Import]
        IViewClassifierAggregatorService viewClassifierAggregatorService = null;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("MarkdownComments")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTagger>(() => new MarkdownCommentsTagger(textView, viewClassifierAggregatorService));
        }
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(IntraTextAdornmentTag))]
    [TagType(typeof(ErrorTag))]
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

            return textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTagger>(() => new MarkdownCommentsTagger(textView as IWpfTextView, viewClassifierAggregatorService)) as ITagger<T>;
        }
    }
}
