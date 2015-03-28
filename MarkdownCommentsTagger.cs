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

        IAdornmentLayer _layer;
        ITextBuffer _textBuffer;
        IWpfTextView _view;
        ITagAggregator<IClassificationTag> _classificationTagAggregator;
        IClassificationTypeRegistryService _classificationTypeRegistry;
        IEditorOptionsFactoryService _editorOptionsFactoryService;
        Brush _brush;
        Pen _pen;
        bool _enabled = true;

        bool _layoutChanged = false;

        List<ITagSpan<ErrorTag>> _errorTags = new List<ITagSpan<ErrorTag>>();
        List<ITagSpan<IntraTextAdornmentTag>> _intraTextAdornmentTags = new List<ITagSpan<IntraTextAdornmentTag>>();
        Dictionary<ITrackingSpan, ITagSpan<IntraTextAdornmentTag>> _intraTextAdornmentTagsDico = new Dictionary<ITrackingSpan, ITagSpan<IntraTextAdornmentTag>>(new TrackingSpanSpanEqualityComparer());
        Dictionary<string, string> _uriErrors = new Dictionary<string, string>();

        public MarkdownCommentsTagger(IWpfTextView view, ITextBuffer textBuffer, IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, IClassificationTypeRegistryService classificationTypeRegistryService, IEditorOptionsFactoryService editorOptionsFactoryService)
        {
            _view = view;
            _layer = view.GetAdornmentLayer("MarkdownComments");

            _textBuffer = textBuffer;
            _classificationTagAggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<ClassificationTag>(textBuffer, TagAggregatorOptions.MapByContentType);
            _classificationTypeRegistry = classificationTypeRegistryService;

            _editorOptionsFactoryService = editorOptionsFactoryService;
            _enabled = _editorOptionsFactoryService.GlobalOptions.GetOptionValue<bool>(EnabledOption);

            //Listen to any event that changes the layout (text changes, scrolling, etc)
            _view.LayoutChanged += OnLayoutChanged;
            _view.TextBuffer.Changed += OnTextBufferChanged;
            _view.Caret.PositionChanged += OnCaretPositionChanged;

            //Create the pen and brush to color the boxes
            Brush brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
            brush.Freeze();
            Brush penBrush = new SolidColorBrush(Colors.Red);
            penBrush.Freeze();
            Pen pen = new Pen(penBrush, 0.5);
            pen.Freeze();

            _brush = brush;
            _pen = pen;
        }

        public void Dispose()
        {
            _classificationTagAggregator.Dispose();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            ITextViewLine oldLine = _view.TextViewLines.GetTextViewLineContainingBufferPosition(e.OldPosition.BufferPosition);
            ITextViewLine newLine = _view.TextViewLines.GetTextViewLineContainingBufferPosition(e.NewPosition.BufferPosition);
            if(newLine != oldLine)
            {
                if(oldLine != null)
                {
                    TagsChanged(this, new SnapshotSpanEventArgs(oldLine.Extent));
                }

                if (newLine != null)
                {
                    TagsChanged(this, new SnapshotSpanEventArgs(newLine.Extent));
                }
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _intraTextAdornmentTagsDico.Clear();

            if (TagsChanged != null)
            {
                foreach(ITextChange change in e.Changes)
                {
                    var changeSpan = new SnapshotSpan(_view.TextSnapshot, change.OldSpan);
                    foreach (ITextViewLine line in _view.TextViewLines.GetTextViewLinesIntersectingSpan(changeSpan))
                    {
                        TagsChanged(this, new SnapshotSpanEventArgs(line.Extent));
                    }
                }
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewSnapshot != e.OldSnapshot) // make sure that there has really been a change
            {
                return;
            }

            //_layoutChanged = true;

            //foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                //this.ParseCode(line.Extent);

                //if (TagsChanged != null)
                //{
                //    TagsChanged(this, new SnapshotSpanEventArgs(line.Extent));
                //}
            }
        }

        ITagSpan<ClassificationTag> MakeClassificationTagSpan(SnapshotSpan span, string tagName)
        {
            return new TagSpan<ClassificationTag>(span, new ClassificationTag(_classificationTypeRegistry.GetClassificationType(tagName)));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        IEnumerable<ITagSpan<ClassificationTag>> ITagger<ClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            //_intraTextAdornmentTags.Clear();
            //_errorTags.Clear();

            //if (span.Snapshot != _view.TextBuffer.CurrentSnapshot)
            //    continue;

            foreach (MarkdownElement element in ParseCode(spans))
            {
                if (element is MarkdownImage)
                {
                    //var image = element as MarkdownImage;
                    yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.Image);
                }
                if (element is MarkdownHeader)
                {
                    var header = element as MarkdownHeader;
                    switch (header.Level)
                    {
                        case 1:
                            yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.H1);
                            break;
                        case 2:
                            yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.H2);
                            break;
                        case 3:
                            yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.H3);
                            break;
                        case 4:
                            yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.H4);
                            break;
                        case 5:
                            yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.H5);
                            break;
                        case 6:
                            yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.H6);
                            break;
                    }
                }
                if (element is MarkdownEmphasis)
                {
                    yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.Emphasis);
                }
                if (element is MarkdownStrongEmphasis)
                {
                    yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.StrongEmphasis);
                }
                if (element is MarkdownStrikethrough)
                {
                    yield return MakeClassificationTagSpan(element.Span, ClassificationTypes.Strikethrough);
                }
            }
        }

        void AddMarkdownUIElement<T>(T element) where T : MarkdownElement
        {
        }
        void AddMarkdownUIElement(MarkdownHeader header)
        {
            AddUIElement(header.DelimiterSpan, () => new UIElement());
        }
        void AddMarkdownUIElement(MarkdownEmphasis emphasis)
        {
            AddUIElement(emphasis.StartDelimiterSpan, () => new UIElement());
            AddUIElement(emphasis.EndDelimiterSpan, () => new UIElement());
        }
        void AddMarkdownUIElement(MarkdownStrongEmphasis strongEmphasis)
        {
            AddUIElement(strongEmphasis.StartDelimiterSpan, () => new UIElement());
            AddUIElement(strongEmphasis.EndDelimiterSpan, () => new UIElement());
        }
        void AddMarkdownUIElement(MarkdownStrikethrough strikethrough)
        {
            AddUIElement(strikethrough.StartDelimiterSpan, () => new UIElement());
            AddUIElement(strikethrough.EndDelimiterSpan, () => new UIElement());
        }
        void AddMarkdownUIElement(MarkdownImage image)
        {
            AddImage(image.Span, image.AltText, image.Uri, image.OptTitle);
        }

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            _intraTextAdornmentTags.Clear();
            _errorTags.Clear();

            foreach (SnapshotSpan span in spans)
            {
                //if (span.Snapshot != _view.TextBuffer.CurrentSnapshot)
                //    continue;

                //foreach (SnapshotSpan commentSpan in GetCodeCommentsSpans(span, ClassificationTypes.Image))
                foreach (SnapshotSpan commentSpan in GetCodeCommentsSpans(spans))
                {
                    foreach (MarkdownElement element in ParseComment(span, true))
                    {
                        AddMarkdownUIElement((dynamic)element);
                    }
                }
            }

            return _intraTextAdornmentTags;
        }

        IEnumerable<ITagSpan<ErrorTag>> ITagger<ErrorTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            //_errorTags.Clear();

            //foreach (SnapshotSpan span in spans)
            //{
            //    //if (span.Snapshot != _view.TextBuffer.CurrentSnapshot)
            //    //    continue;

            //    ParseCode(span);
            //}

            return _errorTags;
        }

        private void AddUIElement(SnapshotSpan span, Func<UIElement> creator)
        {
            ITagSpan<IntraTextAdornmentTag> tag;
            ITrackingSpan trackingSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            if (!_intraTextAdornmentTagsDico.TryGetValue(trackingSpan, out tag))
            {
                UIElement element = creator();
                tag = new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(element, null));
                _intraTextAdornmentTagsDico.Add(trackingSpan, tag);
            }
            _intraTextAdornmentTags.Add(tag);
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
                    foreach(SnapshotSpan commentSpanPart in classificationMappingTagSpan.Span.GetSpans(_view.TextSnapshot))
                    {
                        //AddMarkerGeometry(span);

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
                var caretLineSpan = _view.Caret.Position.BufferPosition.GetContainingLine().ExtentIncludingLineBreak;
                bool skipThisOne = _view.Selection.IsEmpty
                    ? span.OverlapsWith(caretLineSpan.TranslateTo(span.Snapshot, SpanTrackingMode.EdgeInclusive))
                    : span.Contains(_view.Selection.AnchorPoint.Position.TranslateTo(span.Snapshot, PointTrackingMode.Positive));

                if (skipThisOne)
                    yield break;
            }

            //AddMarkerGeometry(span);

            foreach (MarkdownImage element in MarkdownCommentsParser.GetImageSpans(span))
            {
                //AddMarkerGeometry(element.Span);
                //AddImage(element.Span, element.AltText, element.Uri, element.OptTitle);
                yield return element;
            }

            foreach (MarkdownElement element in MarkdownCommentsParser.GetHeaderSpans(span))
            {
                //AddMarkerGeometry(element.Span);
                yield return element;
            }

            foreach (MarkdownElement element in MarkdownCommentsParser.GetEmphasisSpans(span))
            {
                //AddMarkerGeometry(element.Span);
                yield return element;
            }

            foreach (MarkdownElement element in MarkdownCommentsParser.GetStrongEmphasisSpans(span))
            {
                //AddMarkerGeometry(element.Span);
                yield return element;
            }

            foreach (MarkdownElement element in MarkdownCommentsParser.GetStrikethroughSpans(span))
            {
                //AddMarkerGeometry(element.Span);
                yield return element;
            }
        }

        private void AddMarkerGeometry(SnapshotSpan span)
        {
            Geometry g = null;
            if (_layoutChanged && _view.TextViewLines != null)
                g = _view.TextViewLines.GetMarkerGeometry(span);

            if (g != null)
            {
                GeometryDrawing drawing = new GeometryDrawing(_brush, _pen, g);
                drawing.Freeze();

                DrawingImage drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();

                Image image = new Image();
                image.Source = drawingImage;

                //Align the image with the top of the bounds of the text geometry
                Canvas.SetLeft(image, g.Bounds.Left);
                Canvas.SetTop(image, g.Bounds.Top);

                _layer.AddAdornment(span, null, image);
            }

            //TextBox textBox = new TextBox();
            //textBox.Text = _view.TextSnapshot.GetText(span);
            //textBox.BorderThickness = new Thickness(0);
            //textBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //AddUIElement(span, () => textBox);
        }

        private void AddImage(SnapshotSpan span, string altText, string uri, string optTitle)
        {
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
                    TagsChanged(this, new SnapshotSpanEventArgs(span));
                };
                imageSource.DownloadFailed += delegate(Object sender, ExceptionEventArgs e)
                {
                    _uriErrors[uri] = string.Format("Failed to download image from {0}.\n{1}", uri, e.ErrorException.Message);
                    TagsChanged(this, new SnapshotSpanEventArgs(span));
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
                //textBox.BorderThickness = new Thickness(0);
                //textBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                //AddUIElement(span, () => textBox);
            }
            else
            {
                AddUIElement(span, () => image);
            }
        }

    }
}
