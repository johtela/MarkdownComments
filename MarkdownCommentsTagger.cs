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
        ITextBuffer _textBuffer;
        IWpfTextView _textView;
        MarkdownCommentsFactory _factory;
        MarkdownCommentsParser _parser;
        ITagAggregator<IClassificationTag> _classificationTagAggregator;

        // Cached options
        bool _enabled = true;
        bool _showImages = true;
        bool _hideDelimiters = true;

        Dictionary<ITrackingSpan, ITagSpan<IntraTextAdornmentTag>> _intraTextAdornmentTagsDico = new Dictionary<ITrackingSpan, ITagSpan<IntraTextAdornmentTag>>(new TrackingSpanSpanEqualityComparer());
        Dictionary<string, string> _uriErrors = new Dictionary<string, string>();

        public MarkdownCommentsTagger(IWpfTextView textView, ITextBuffer textBuffer, MarkdownCommentsFactory factory)
        {
            _textView = textView;
            _textBuffer = textBuffer;
            _factory = factory;
            _parser = new MarkdownCommentsParser();
            _classificationTagAggregator = _factory.BufferTagAggregatorFactoryService.CreateTagAggregator<ClassificationTag>(textBuffer, TagAggregatorOptions.MapByContentType);

            if(MarkdownCommentsFactory.Package != null)
            {
                var optionsPage = MarkdownCommentsFactory.Package.GetOptions();
                if(optionsPage != null)
                {
                    _enabled = optionsPage.OptionEnableMarkdownComments;
                    _showImages = optionsPage.OptionShowImages;
                    _hideDelimiters = optionsPage.OptionHideDelimiters;
                    _parser.SkipPreprocessor = optionsPage.OptionSkipPreprocessor;
                }

                MarkdownCommentsFactory.Package.OptionsChanged += OnOptionsChanged;
            }

            //Listen to any event that changes the layout (text changes, scrolling, etc)
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.TextBuffer.Changed += OnTextBufferChanged;
            _textView.Caret.PositionChanged += OnCaretPositionChanged;
        }

        public void Dispose()
        {
            _classificationTagAggregator.Dispose();
        }

        protected void OnOptionsChanged(object sender, MarkdownCommentsOptionsChanged e)
        {
            bool needRefreshEntireTextView = false;
            bool needRefreshEntireTextBuffer = false;

            var options = sender as MarkdownCommentsOptionsPage;
            if (e.hasOptionChanged(MarkdownCommentsOptions.EnableMarkdownComments))
            {
                _enabled = options.OptionEnableMarkdownComments;
                needRefreshEntireTextBuffer = true;
            }
            if (e.hasOptionChanged(MarkdownCommentsOptions.ShowImages))
            {
                _showImages = options.OptionShowImages;
                needRefreshEntireTextView = true;
            }
            if (e.hasOptionChanged(MarkdownCommentsOptions.HideDelimiters))
            {
                _hideDelimiters = options.OptionHideDelimiters;
                needRefreshEntireTextView = true;
            }
            if (e.hasOptionChanged(MarkdownCommentsOptions.SkipPreprocessor))
            {
                _parser.SkipPreprocessor = options.OptionSkipPreprocessor;
                needRefreshEntireTextView = true;
            }

            if(needRefreshEntireTextBuffer)
            {
                NotifyTagsChangedOnEntireTextBuffer();
            }
            else if(needRefreshEntireTextView)
            {
                NotifyTagsChangedOnEntireTextView();
            }
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!_enabled)
                return;

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
            if (!_enabled)
                return;

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
            if (!_enabled)
                return;

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
            return new TagSpan<ClassificationTag>(span, new ClassificationTag(_factory.ClassificationTypeRegistryService.GetClassificationType(tagName)));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void NotifyTagsChanged(SnapshotSpan span)
        {
            if(TagsChanged != null)
            {
                TagsChanged(this, new SnapshotSpanEventArgs(span));
            }
        }

        public void NotifyTagsChangedOnEntireTextBuffer()
        {
            NotifyTagsChanged(new SnapshotSpan(_textBuffer.CurrentSnapshot, new Span(0, _textBuffer.CurrentSnapshot.Length)));
        }

        public void NotifyTagsChangedOnEntireTextView()
        {
            //NotifyTagsChanged(new SnapshotSpan(_textView.TextViewLines.FirstVisibleLine.Snapshot, new Span(_textView.TextViewLines.FirstVisibleLine.Start.Position, _textView.TextViewLines.LastVisibleLine.End.Position)));
            NotifyTagsChangedOnEntireTextBuffer();
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
            if (!_enabled)
                yield break;

            //if (span.Snapshot != _textView.TextBuffer.CurrentSnapshot)
            //    continue;

            foreach (MarkdownElement element in ParseCode(spans, false))
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
            if (!_hideDelimiters)
                yield break;

            yield return MakeIntraTextAdornmentTagSpan(header.DelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownEmphasis emphasis)
        {
            if (!_hideDelimiters)
                yield break;

            yield return MakeIntraTextAdornmentTagSpan(emphasis.StartDelimiterSpan, () => new UIElement());
            yield return MakeIntraTextAdornmentTagSpan(emphasis.EndDelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownStrongEmphasis strongEmphasis)
        {
            if (!_hideDelimiters)
                yield break;

            yield return MakeIntraTextAdornmentTagSpan(strongEmphasis.StartDelimiterSpan, () => new UIElement());
            yield return MakeIntraTextAdornmentTagSpan(strongEmphasis.EndDelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownStrikethrough strikethrough)
        {
            if (!_hideDelimiters)
                yield break;

            yield return MakeIntraTextAdornmentTagSpan(strikethrough.StartDelimiterSpan, () => new UIElement());
            yield return MakeIntraTextAdornmentTagSpan(strikethrough.EndDelimiterSpan, () => new UIElement());
        }
        IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetIntraTextAdornmentTagSpans(MarkdownImage image)
        {
            if (!_showImages)
                yield break;

            var tagSpan = MakeImageIntraTextAdornmentTagSpans(image.Span, image.AltTextSpan, image.UriSpan, image.OptTitleSpan);
            if (tagSpan != null)
                yield return tagSpan;
        }

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!_enabled)
                yield break;

            //if (span.Snapshot != _textView.TextBuffer.CurrentSnapshot)
            //    continue;

            foreach (MarkdownElement element in ParseCode(spans, true))
            {
                foreach(ITagSpan<IntraTextAdornmentTag> tagSpan in GetIntraTextAdornmentTagSpans((dynamic)element))
                {
                    yield return tagSpan;
                }
            }
        }

        private ITagSpan<ErrorTag> MakeErrorTagSpan(SnapshotSpan span, string error)
        {
            return new TagSpan<ErrorTag>(span, new ErrorTag("other error", error));
        }

        IEnumerable<ITagSpan<ErrorTag>> GetErrorTagSpans<T>(T element) where T : MarkdownElement
        {
            yield break;
        }
        IEnumerable<ITagSpan<ErrorTag>> GetErrorTagSpans(MarkdownImage image)
        {
            if (!_showImages)
                yield break;

            string error;
            if(_uriErrors.TryGetValue(image.UriSpan.GetText(), out error))
            {
                yield return MakeErrorTagSpan(image.Span, error);
            }
        }

        IEnumerable<ITagSpan<ErrorTag>> ITagger<ErrorTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!_enabled)
                yield break;

            //if (span.Snapshot != _textView.TextBuffer.CurrentSnapshot)
            //    continue;

            if(spans.Count == 1 && spans[0].IsEmpty)
            {
                // To have the tooltip when hovering a squiggle, get all errors that intersect with given span
                // TODO: Move this in a more generic way in ParseCode?!
                spans = new NormalizedSnapshotSpanCollection(_textView.TextViewLines.GetTextViewLineContainingBufferPosition(spans[0].Start).Extent);
            }

            foreach (MarkdownElement element in ParseCode(spans, true))
            {
                foreach (ITagSpan<ErrorTag> tagSpan in GetErrorTagSpans((dynamic)element))
                {
                    yield return tagSpan;
                }
            }
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

        private IEnumerable<MarkdownElement> ParseCode(NormalizedSnapshotSpanCollection spans, bool skipEditingSpan)
        {
            foreach (SnapshotSpan commentSpan in GetCodeCommentsSpans(spans))
            {
                foreach (MarkdownElement element in ParseComment(commentSpan, skipEditingSpan))
                {
                    yield return element;
                }
            }
        }

        private IEnumerable<MarkdownElement> ParseComment(SnapshotSpan span, bool skipEditingSpan)
        {
            if (skipEditingSpan)
            {
                if (!_textView.Selection.AnchorPoint.IsInVirtualSpace && span.Start.GetContainingLine().ExtentIncludingLineBreak.Contains(_textView.Selection.AnchorPoint.Position.Position))
                    yield break;
            }

            foreach (MarkdownImage element in _parser.GetImageSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in _parser.GetHeaderSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in _parser.GetEmphasisSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in _parser.GetStrongEmphasisSpans(span))
            {
                yield return element;
            }
            foreach (MarkdownElement element in _parser.GetStrikethroughSpans(span))
            {
                yield return element;
            }
        }

        private ITagSpan<IntraTextAdornmentTag> MakeImageIntraTextAdornmentTagSpans(SnapshotSpan span, SnapshotSpan altTextSpan, SnapshotSpan uriSpan, SnapshotSpan optTitleSpan)
        {
            string altText = altTextSpan.GetText();
            string uri = uriSpan.GetText();
            string optTitle = optTitleSpan.GetText();

            Uri uriObject = new Uri(uri, UriKind.RelativeOrAbsolute);
            if(!uriObject.IsAbsoluteUri)
            {
                ITextDocument document;
                if(_factory.TextDocumentFactoryService.TryGetTextDocument(_textBuffer, out document))
                {
                    uriObject = new Uri(new Uri(document.FilePath), uri);
                }
            }

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
                imageSource = BitmapFrame.Create(uriObject);
            }
            catch (FileNotFoundException e)
            {
                _uriErrors[uri] = string.Format("File {0} not found.\n{1}", uri, e.Message);
            }
            catch (Exception e)
            {
                _uriErrors[uri] = string.Format("Failed to load image from {0}.\n{1}", uri, e.Message);
            }
#endif

            image.Source = imageSource;

            bool downloading = (imageSource != null) && imageSource.IsDownloading;
            if (downloading)
            {
                imageSource.DownloadCompleted += delegate(Object sender, EventArgs e)
                {
                    if (_uriErrors.ContainsKey(uri))
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

            //if (downloading || _uriErrors.ContainsKey(uri))
            if (_uriErrors.ContainsKey(uri))
            {
                //TextBox textBox = new TextBox();
                //textBox.Text = altText;
                //textBox.BorderThickness = new Thickness(1);
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
