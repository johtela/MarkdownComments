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
    internal class MarkdownCommentsTextAdornment : ITagger<IntraTextAdornmentTag>, ITagger<ErrorTag>
    {
        IAdornmentLayer _layer;
        IWpfTextView _view;
        IClassifier _classifier;
        Brush _brush;
        Pen _pen;

        //bool _parsed = false;
        bool _layoutChanged = false;

        List<ITagSpan<ErrorTag>> _errorTags = new List<ITagSpan<ErrorTag>>();
        List<ITagSpan<IntraTextAdornmentTag>> _intraTextAdornmentTags = new List<ITagSpan<IntraTextAdornmentTag>>();
        Dictionary<SnapshotSpan, ITagSpan<IntraTextAdornmentTag>> _intraTextAdornmentTagsDico = new Dictionary<SnapshotSpan, ITagSpan<IntraTextAdornmentTag>>();
        Dictionary<string, string> _uriErrors = new Dictionary<string, string>();

        public MarkdownCommentsTextAdornment(IWpfTextView view, IViewClassifierAggregatorService viewClassifierAggregatorService)
        {
            _view = view;
            _layer = view.GetAdornmentLayer("MarkdownCommentsTextAdornment");
            _classifier = viewClassifierAggregatorService.GetClassifier(view);

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

                TagsChanged(this, new SnapshotSpanEventArgs(newLine.Extent));
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _intraTextAdornmentTagsDico.Clear();

            if (TagsChanged != null)
            {
                foreach(ITextChange change in e.Changes)
                {
                    var changeSpan = new SnapshotSpan(_view.TextSnapshot, change.NewSpan);
                    foreach (ITextViewLine line in _view.TextViewLines.GetTextViewLinesIntersectingSpan(changeSpan))
                    {
                        TagsChanged(this, new SnapshotSpanEventArgs(line.Extent));
                    }
                }
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
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

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            _intraTextAdornmentTags.Clear();
            _errorTags.Clear();

            foreach (SnapshotSpan span in spans)
            {
                //if (span.Snapshot != _view.TextBuffer.CurrentSnapshot)
                //    continue;

                ParseCode(span);
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
            if (!_intraTextAdornmentTagsDico.TryGetValue(span, out tag))
            {
                UIElement element = creator();
                tag = new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(element, null));
                _intraTextAdornmentTagsDico.Add(span, tag);
            }
            _intraTextAdornmentTags.Add(tag);
        }

        private void ParseCode(SnapshotSpan span)
        {
            Nullable<SnapshotSpan> commentSpan = null;

            // Loop through each comment span
            IList<ClassificationSpan> classificationSpanList = _classifier.GetClassificationSpans(span);
            foreach(ClassificationSpan classificationSpan in classificationSpanList)
            {
                //AddMarkerGeometry(classificationSpan.Span);
                if (classificationSpan.ClassificationType.IsOfType("comment"))
                {
                    SnapshotSpan commentSpanPart = classificationSpan.Span;
                    //AddMarkerGeometry(span);

                    if (commentSpan != null && commentSpanPart.Start.Position == commentSpan.Value.End.Position)
                    {
                        commentSpan = new SnapshotSpan(commentSpan.Value.Start, commentSpanPart.End);
                    }
                    else
                    {
                        if (commentSpan != null)
                        {
                            ParseComment(commentSpan.Value);
                        }

                        commentSpan = commentSpanPart;
                    }
                }
            }

            if (commentSpan != null)
            {
                ParseComment(commentSpan.Value);
            }
        }

        private void ParseComment(SnapshotSpan span)
        {
            var caretLineSpan = _view.Caret.Position.BufferPosition.GetContainingLine().ExtentIncludingLineBreak;
            bool dontParse = _view.Selection.IsEmpty
                ? span.OverlapsWith(caretLineSpan.TranslateTo(span.Snapshot, SpanTrackingMode.EdgeInclusive))
                : span.Contains(_view.Selection.AnchorPoint.Position.TranslateTo(span.Snapshot, PointTrackingMode.Positive));
            if (dontParse)
                return;

            //AddMarkerGeometry(span);

            {
                //string textRegex = @"\[([^\[\]]*)\]";
                string textRegex = @"(?<titleOpen>\[)([^\[\]]*)(?<titleClose-titleOpen>\])(?(titleOpen)(?!))";
                string optTitleRegex = @"(?:\s+""([^""\(\)]*)"")?";
                string urlRegex = @"\(([^\s\)]+)" + optTitleRegex + @"\)";
                //Regex inlineLinkRegex = new Regex(@"(?<!\!)" + textRegex + urlRegex);
                Regex inlineImageRegex = new Regex(@"\!" + textRegex + urlRegex);
                foreach (Match match in inlineImageRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    string altText = match.Groups[1].Value;
                    string uri = match.Groups[2].Value;
                    string optTitle = match.Groups[3].Value;
                    //AddMarkerGeometry(matchedSpan);
                    AddImage(matchedSpan, altText, uri, optTitle);
                }
            }

            {
                Regex emphasisRegex = new Regex(@"(?<delimiter>[\*_])" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>)" + @"(?:.(?<!\k<delimiter>))+?" + @"\k<delimiter>" + @"(?!(?:\w|\k<delimiter>))");
                foreach (Match match in emphasisRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    AddMarkerGeometry(matchedSpan);
                }
            }

            {
                Regex strongEmphasisRegex = new Regex(@"(?<delimiter>[\*_]){2}" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"(?:.(?<!\k<delimiter>))+?" + @"\k<delimiter>{2}" + @"(?!(?:\w|\k<delimiter>))");
                foreach (Match match in strongEmphasisRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    AddMarkerGeometry(matchedSpan);
                }
            }

            {
                Regex strikethroughRegex = new Regex(@"(?<delimiter>~){2}" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"(?:.(?<!\k<delimiter>))+?" + @"\k<delimiter>{2}" + @"(?!(?:\w|\k<delimiter>))");
                foreach (Match match in strikethroughRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    AddMarkerGeometry(matchedSpan);
                }
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

            //TextBox textBox = null;
            //textBox = new TextBox();
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
                _uriErrors.Add(uri, e.Message);
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
                    _uriErrors[uri] = e.ErrorException.Message;
                    TagsChanged(this, new SnapshotSpanEventArgs(span));
                };
            }

            if (optTitle != "")
            {
                image.ToolTip = optTitle;
            }

            Geometry g = null;
            if (_layoutChanged && _view.TextViewLines != null)
                g = _view.TextViewLines.GetMarkerGeometry(span);

            if (g != null)
            {
                //Align the image with the bottom of the bounds of the text geometry
                Canvas.SetLeft(image, g.Bounds.Left);
                if (!imageSource.IsDownloading)
                    Canvas.SetTop(image, g.Bounds.Bottom - image.Source.Height);
                else
                {
                    Canvas.SetTop(image, g.Bounds.Top);
                    imageSource.DownloadCompleted += delegate(Object sender, EventArgs e)
                    {
                        Canvas.SetTop(image, g.Bounds.Bottom - image.Source.Height);
                    };
                }

                _layer.AddAdornment(span, null, image);
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
