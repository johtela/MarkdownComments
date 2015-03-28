using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownComments
{
    [NameAttribute("MarkdownComments/Enabled")]
    [ExportAttribute(typeof(EditorOptionDefinition))]
    public sealed class MarkdownCommentsEnabled : EditorOptionDefinition<bool>
    {
        public override bool Default { get { return true; } }
        public override EditorOptionKey<bool> Key { get { return MarkdownCommentsTagger.EnabledOption; } }
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(ClassificationTag))]
    [TagType(typeof(IntraTextAdornmentTag))]
    [TagType(typeof(ErrorTag))]
    internal sealed class MarkdownCommentsTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService = null;

        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistryService = null;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("MarkdownComments")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        //[Import(AllowDefault = true)]
        //internal ISettingsStore settingsStore = null;
        [Import]
        IEditorOptionsFactoryService editorOptionsFactory = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer) where T : ITag
        {
            //provide highlighting only on the top buffer 
            if (textView.TextBuffer != textBuffer)
                return null;

            if (!(textView is IWpfTextView))
                return null;

            MarkdownCommentsTagger tagger = textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTagger>(
                () => new MarkdownCommentsTagger(textView as IWpfTextView, textBuffer, bufferTagAggregatorFactoryService, classificationTypeRegistryService, editorOptionsFactory)
            );
            return tagger as ITagger<T>;
        }
    }
}
