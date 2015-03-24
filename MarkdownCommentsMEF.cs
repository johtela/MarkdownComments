using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownComments
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(ClassificationTag))]
    [TagType(typeof(IntraTextAdornmentTag))]
    [TagType(typeof(ErrorTag))]
    internal sealed class MarkdownCommentsTaggerProvider : IViewTaggerProvider
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("MarkdownComments")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        [Import]
        internal IClassifierAggregatorService classifierAggregatorService = null;

        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistryService = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            //provide highlighting only on the top buffer 
            if (textView.TextBuffer != buffer)
                return null;

            if (!(textView is IWpfTextView))
                return null;

            var classifier = classifierAggregatorService.GetClassifier(buffer);
            return textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTagger>(() => new MarkdownCommentsTagger(textView as IWpfTextView, classifier, classificationTypeRegistryService)) as ITagger<T>;
        }
    }
}
