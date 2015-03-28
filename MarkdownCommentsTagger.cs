using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using System.Net;

namespace MarkdownComments
{
    class TrackingSpanSpanEqualityComparer : EqualityComparer<ITrackingSpan>
    {
	    public override bool Equals(ITrackingSpan tspan1, ITrackingSpan tspan2)
        {
            SnapshotSpan span1 = tspan1.GetSpan(tspan1.TextBuffer.CurrentSnapshot);
            SnapshotSpan span2 = tspan2.GetSpan(tspan2.TextBuffer.CurrentSnapshot);
            return span1.Equals(span2);
	    }

	    public override int GetHashCode(ITrackingSpan tspan)
	    {
            SnapshotSpan span = tspan.GetSpan(tspan.TextBuffer.CurrentSnapshot);
		    return span.GetHashCode();
	    }
    }

    internal class MarkdownCommentsTagger : ITagger<ClassificationTag>, ITagger<IntraTextAdornmentTag>, ITagger<ErrorTag>, IDisposable
    {
        public static readonly EditorOptionKey<bool> EnabledOption = new EditorOptionKey<bool>("MarkdownComments/Enabled");

        ITextBuffer _textBuffer;
        IWpfTextView _textView;
        ITagAggregator<IClassificationTag> _classificationTagAggregator;
        IClassificationTypeRegistryService _classificationTypeRegistry;
        IEditorOptionsFactoryService _editorOptionsFactoryService;
        bool _enabled = true;

        List<ITagSpan<ErrorTag>> _errorTags = new List<ITagSpan<ErrorTag>>();
        Dictionary<ITrackingSpan, ITagSpan<IntraTextAdornmentTag>> _intraTextAdornmentTagsDico = new Dictionary<ITrackingSpan, ITagSpan<IntraTextAdornmentTag>>(new TrackingSpanSpanEqualityComparer());
        Dictionary<string, string> _uriErrors = new Dictionary<string, string>();

        public MarkdownCommentsTagger(IWpfTextView textView, ITextBuffer textBuffer, IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, IClassificationTypeRegistryService classificationTypeRegistryService, IEditorOptionsFactoryService editorOptionsFactoryService)
        {
            _textView = textView;
            _textBuffer = textBuffer;
            _classificationTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<ClassificationTag>(textBuffer, TagAggregatorOptions.MapByContentType);
            _classificationTypeRegistry = classificationTypeRegistryService;

            _editorOptionsFactoryService = editorOptionsFactoryService;
            _enabled = _editorOptionsFactoryService.GlobalOptions.GetOptionValue<bool>(EnabledOption);

            //Listen to any event that changes the layout (text changes, scrolling, etc)
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.TextBuffer.Changed += OnTextBufferChanged;
            _textView.Caret.PositionChanged += OnCaretPositionChanged;
        }

        public void Dispose()
        {
            _classificationTagAggregator.Dispose();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            ITextViewLine oldLine = _textView.TextViewLines.GetTextViewLineContainingBufferPosition(e.OldPosition.BufferPosition);
            ITextViewLine newLine = _textView.TextViewLines.GetTextViewLineContainingBufferPosition(e.NewPosition.BufferPosition);
            if(newLine != oldLine)
            {
                if(oldLine != null)
                {
                    NotifyTagsChanged(oldLine.Extent);
                }

                if (newLine != null)
                {
                    NotifyTagsChanged(newLine.Extent);
                }
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _intraTextAdornmentTagsDico.Clear();

            foreach(ITextChange change in e.Changes)
            {
                var changeSpan = new SnapshotSpan(_textView.TextSnapshot, change.OldSpan);
                foreach (ITextViewLine line in _textView.TextViewLines.GetTextViewLinesIntersectingSpan(changeSpan))
                {
                    NotifyTagsChanged(line.Extent);
                }
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewSnapshot != e.OldSnapshot) // make sure that there has really been a change
            {
                return;
            }

            //foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                //this.ParseCode(line.Extent);

                //NotifyTagsChanged(line.Extent);
            }
        }

        ITagSpan<ClassificationTag> MakeClassificationTagSpan(SnapshotSpan span, string tagName)
        {
            return new TagSpan<ClassificationTag>(span, new ClassificationTag(_classificationTypeRegistry.GetClassificationType(tagName)));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void NotifyTagsChanged(SnapshotSpan span)
        {
            if(TagsChanged != null)
            {
                TagsChanged(this, new SnapshotSpanEventArgs(span));
            }
        }

        IEnumerable<ITagSpan<ClassificationTag>> GetClassificationTagSpans<T>(MarkdownElement element) where T : MarkdownElement
        {
            yield break;
        }
        IEnumerable<ITagSpan<ClassificationTag>> GetClassificationTagSpans(MarkdownEmphasis emphasis)
        {
            yield return MakeClassificationTagSpan(emphasis.Span, ClassificationTypes.Emphasis);
        }
        IEnumerable<ITagSpan<ClassificationTag>> GetClassificationTagSpans(MarkdownStrongEmphasis strongEmphasis)
        {
            yield return MakeClassificationTagSpan(strongEmphasis.Span, ClassificationTypes.StrongEmphasis);
        }
        IEnumerable<ITagSpan<ClassificationTag>> GetClassificationTagSpans(MarkdownStrikethrough strikethrough)
        {
            yield return MakeClassificationTagSpan(strikethrough.Span, ClassificationTypes.Strikethrough);
        }
        IEnumerable<ITagSpan<ClassificationTag>> GetClassificationTagSpans(MarkdownHeader header)
        {
            switch (header.Level)
            {
                case 1:
                    yield return MakeClassificationTagSpan(header.Span, ClassificationTypes.H1);
                    break;
                case 2:
                    yield return MakeClassificationTagSpan(header.Span, ClassificationTypes.H2);
                    break;
                case 3:
                    yield return MakeClassificationTagSpan(header.Span, ClassificationTypes.H3);
                    break;
                case 4:
                    yield return MakeClassificationTagSpan(header.Span, ClassificationTypes.H4);
                    break;
                case 5:
                    yield return MakeClassificationTagSpan(header.Span, ClassificationTypes.H5);
                    break;
                case 6:
                    yield return MakeClassificationTagSpan(header.Span, ClassificationTypes.H6);
                    break;
            }
        }
        IEnumerable<ITagSpan<ClassificationTag>> GetClassificationTagSpans(MarkdownImage image)
        {
            yield return MakeClassificationTagSpan(image.Span, ClassificationTypes.Image);
        }

        IEnumerable<ITagSpan<ClassificationTag>> ITagger<ClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            //if (span.Snapshot != _textView.TextBuffer.CurrentSnapshot)
            //    continue;

            foreach (MarkdownElement element in ParseCode(spans))
            {
                foreach (var classificationTagSpan in GetClassificationTagSpans((dynamic)element))
                {
                    yield return classificationTagSpan;
                }
            }
        }

        private ITagSpan<IntraTextAdornmentTag> MakeIntraTextAdornmentTagSpan(SnapshotSpan span, Func<UIElement> creator)
        {
            ITagSpan<IntraTextAdornmentTag> tag;
            ITrackingSpan trackingSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            if (!_intraTextAdornmentTagsDico.TryGetValue(trackingSpan, out tag))
            {
                UIElement element = creator();
                tag = new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(element, null));
                _intraTextAdornmentTagsDico.Add(trackingSpan, tag);
            }
            return tag;
        }

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans<T>(T element) where T : MarkdownElement
        {
            yield break;
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownHeader header)
        {
            yield return MakeIntraTextAdornmentTagSpan(header.DelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownEmphasis emphasis)
        {
            yield return MakeIntraTextAdornmentTagSpan(emphasis.StartDelimiterSpan, () => new UIElement());
            yield return MakeIntraTextAdornmentTagSpan(emphasis.EndDelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownStrongEmphasis strongEmphasis)
        {
            yield return MakeIntraTextAdornmentTagSpan(strongEmphasis.StartDelimiterSpan, () => new UIElement());
            yield return MakeIntraTextAdornmentTagSpan(strongEmphasis.EndDelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownStrikethrough strikethrough)
        {
            yield return MakeIntraTextAdornmentTagSpan(strikethrough.StartDelimiterSpan, () => new UIElement());
            yield return MakeIntraTextAdornmentTagSpan(strikethrough.EndDelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownImage image)
        {
            var tagSpan = MakeImageIntraTextAdornmentTagSpans(image.Span, image.AltTextSpan, image.UriSpan, image.OptTitleSpan);
            if (tagSpan != null)
                yield return tagSpan;
        }

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            _errorTags.Clear();

            //if (span.Snapshot != _textView.TextBuffer.CurrentSnapshot)
            //    continue;

            //foreach (SnapshotSpan commentSpan in GetCodeCommentsSpans(span, ClassificationTypes.Image))
            foreach (SnapshotSpan commentSpan in GetCodeCommentsSpans(spans))
            {
                foreach (MarkdownElement element in ParseComment(commentSpan, true))
                {
                    foreach(ITagSpan<IntraTextAdornmentTag> tagSpan in GetIntraTextAdornmentTagSpans((dynamic)element))
                    {
                        yield return tagSpan;
                    }
                }
            }
        }

        IEnumerable<ITagSpan<ErrorTag>> ITagger<ErrorTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            //_errorTags.Clear();

            //foreach (SnapshotSpan span in spans)
            //{
            //    //if (span.Snapshot != _textView.TextBuffer.CurrentSnapshot)
            //    //    continue;

            //    ParseCode(span);
            //}

            return _errorTags;
        }

        private IEnumerable<SnapshotSpan> GetCodeCommentsSpans(NormalizedSnapshotSpanCollection spans)
        {
            Nullable<SnapshotSpan> commentSpan = null;

            // Loop through each comment span
            foreach (IMappingTagSpan<IClassificationTag> classificationMappingTagSpan in _classificationTagAggregator.GetTags(spans))
            {
                if (classificationMappingTagSpan.Tag.ClassificationType.IsOfType("comment")
                    || classificationMappingTagSpan.Tag.ClassificationType.IsOfType("XML Doc Comment"))
                {
                    foreach(SnapshotSpan commentSpanPart in classificationMappingTagSpan.Span.GetSpans(_textView.TextSnapshot))
                    {
                        if (commentSpan != null && commentSpanPart.Start.Position == commentSpan.Value.End.Position)
                        {
                            commentSpan = new SnapshotSpan(commentSpan.Value.Start, commentSpanPart.End);
                        }
                        else
                        {
                            if (commentSpan != null)
                            {
                                yield return commentSpan.Value;
                            }

                            commentSpan = commentSpanPart;
                        }
                    }
                }
            }

            if (commentSpan != null)
            {
                yield return commentSpan.Value;
            }
        }

        private IEnumerable<MarkdownElement> ParseCode(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan commentSpan in GetCodeCommentsSpans(spans))
            {
                foreach(MarkdownElement element in ParseComment(commentSpan, false))
                {
                    yield return element;
                }
            }
        }

        private IEnumerable<MarkdownElement> ParseComment(SnapshotSpan span, bool skipEditingSpan)
        {
            if (skipEditingSpan)
            {
                var caretLineSpan = _textView.Caret.Position.BufferPosition.GetContainingLine().ExtentIncludingLineBreak;
                bool skipThisOne = _textView.Selection.IsEmpty
                    ? span.OverlapsWith(caretLineSpan.TranslateTo(span.Snapshot, SpanTrackingMode.EdgeInclusive))
                    : span.Contains(_textView.Selection.AnchorPoint.Position.TranslateTo(span.Snapshot, PointTrackingMode.Positive));

                if (skipThisOne)
                    yield break;
            }

            foreach (MarkdownImage element in MarkdownCommentsParser.GetImageSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in MarkdownCommentsParser.GetHeaderSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in MarkdownCommentsParser.GetEmphasisSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in MarkdownCommentsParser.GetStrongEmphasisSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in MarkdownCommentsParser.GetStrikethroughSpans(span))
            {
                yield return element;
            }
        }

        private ITagSpan<IntraTextAdornmentTag> MakeImageIntraTextAdornmentTagSpans(SnapshotSpan span, SnapshotSpan altTextSpan, SnapshotSpan uriSpan, SnapshotSpan optTitleSpan)
        {
            string altText = altTextSpan.GetText();
            string uri = uriSpan.GetText();
            string optTitle = optTitleSpan.GetText();

            Image image = new Image();

#if false
            BitmapImage imageSource = new BitmapImage();
            using (FileStream stream = File.OpenRead("samples\\icon48.png"))
            //WebRequest req = HttpWebRequest.Create("https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png");
            //using (Stream stream = req.GetResponse().GetResponseStream())
            //using (Stream stream = Application.GetResourceStream(new Uri("https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png", UriKind.Absolute)).Stream)
            {
                imageSource.BeginInit();
                imageSource.StreamSource = stream;
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.EndInit(); // load the image from the stream
            } // close the stream
#else
            BitmapFrame imageSource = null;
            try
            {
                imageSource = BitmapFrame.Create(new Uri(uri, UriKind.RelativeOrAbsolute));
            }
            catch (FileNotFoundException e)
            {
                _uriErrors.Add(uri, string.Format("Failed to load image from {0}.\n{1}", uri, e.Message));
            }
#endif

            image.Source = imageSource;

            if (imageSource.IsDownloading)
            {
                imageSource.DownloadCompleted += delegate(Object sender, EventArgs e)
                {
                    _uriErrors.Remove(uri);
                    NotifyTagsChanged(span);
                };
                imageSource.DownloadFailed += delegate(Object sender, ExceptionEventArgs e)
                {
                    _uriErrors[uri] = string.Format("Failed to download image from {0}.\n{1}", uri, e.ErrorException.Message);
                    NotifyTagsChanged(span);
                };
            }

            if (optTitle != "")
            {
                image.ToolTip = optTitle;
            }

            string error;
            if(_uriErrors.TryGetValue(uri, out error))
            {
                var errorTag = new ErrorTag("other error", error);
                _errorTags.Add(new TagSpan<ErrorTag>(span, errorTag));

                //TextBox textBox = new TextBox();
                //textBox.Text = altText;
                //textBox.ToolTip = error;
                //textBox.BorderThickness = new Thickness(0);
                //textBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                //return MakeIntraTextAdornmentTagSpan(span, () => textBox);
                return null;
            }
            else
            {
                return MakeIntraTextAdornmentTagSpan(span, () => image);
            }
        }

    }
}
