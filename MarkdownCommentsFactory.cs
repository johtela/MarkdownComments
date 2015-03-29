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
    internal sealed class MarkdownCommentsFactory : IViewTaggerProvider
    {
        [Import]
        internal IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService { get; set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        //[Import(AllowDefault = true)]
        //internal ISettingsStore SettingsStore { get; set; }
        [Import]
        internal IEditorOptionsFactoryService EditorOptionsFactory { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer) where T : ITag
        {
            // Provide tagger only on the top buffer 
            if (textView.TextBuffer != textBuffer)
                return null;

            if (!(textView is IWpfTextView))
                return null;

            MarkdownCommentsTagger tagger = textView.Properties.GetOrCreateSingletonProperty<MarkdownCommentsTagger>(
                () => new MarkdownCommentsTagger(textView as IWpfTextView, textBuffer, this)
            );
            return tagger as ITagger<T>;
        }
    }
}
