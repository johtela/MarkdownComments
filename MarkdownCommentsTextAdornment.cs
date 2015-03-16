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
using System.Net;

namespace MarkdownComments
{
    ///<summary>
    ///MarkdownCommentsTextAdornment parse Markdown in code comments
    ///</summary>
    public class MarkdownCommentsTextAdornment
    {
        IAdornmentLayer _layer;
        IWpfTextView _view;
        IClassifier _classifier;
        Brush _brush;
        Pen _pen;

        public MarkdownCommentsTextAdornment(IWpfTextView view, IViewClassifierAggregatorService viewClassifierAggregatorService)
        {
            _view = view;
            _layer = view.GetAdornmentLayer("MarkdownCommentsTextAdornment");
            _classifier = viewClassifierAggregatorService.GetClassifier(view);

            //Listen to any event that changes the layout (text changes, scrolling, etc)
            _view.LayoutChanged += OnLayoutChanged;

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

        /// <summary>
        /// On layout change add the adornment to any reformatted lines
        /// </summary>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                this.CreateVisuals(line);
            }
        }

        private void CreateVisuals(ITextViewLine line)
        {
            //grab a reference to the lines in the current TextView 
            IWpfTextViewLineCollection textViewLines = _view.TextViewLines;

            Nullable<SnapshotSpan> commentSpan = null;

            // Loop through each comment span
            SnapshotSpan lineSpan = line.Extent;
            IList<ClassificationSpan> classificationSpanList = _classifier.GetClassificationSpans(lineSpan);
            foreach(ClassificationSpan classificationSpan in classificationSpanList)
            {
                //DrawMarkerGeometry(classificationSpan.Span);
                if (classificationSpan.ClassificationType.IsOfType("comment"))
                {
                    SnapshotSpan span = classificationSpan.Span;
                    //DrawMarkerGeometry(span);

                    if(commentSpan != null && span.Start.Position == commentSpan.Value.End.Position)
                    {
                        commentSpan = new SnapshotSpan(commentSpan.Value.Start, span.End);
                    }
                    else
                    {
                        if (commentSpan != null)
                        {
                            ParseMarkdown(commentSpan.Value);
                        }

                        commentSpan = span;
                    }
                }
            }

            if (commentSpan != null)
            {
                ParseMarkdown(commentSpan.Value);
            }
        }

        private void ParseMarkdown(SnapshotSpan span)
        {
            //DrawMarkerGeometry(span);

            {
                string imageAltTextRegex = @"\!\[([^\]]*)\]";
                string imageOptTitleRegex = @"(?:\s+""([^""\)]*)"")?";
                string imageUrlRegex = @"\(([^\s\)]+)" + imageOptTitleRegex + @"\)";
                Regex imageRegex = new Regex(imageAltTextRegex + imageUrlRegex);
                foreach (Match match in imageRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    string altText = match.Groups[1].Value;
                    string uri = match.Groups[2].Value;
                    string optTitle = match.Groups[3].Value;
                    DrawMarkerGeometry(matchedSpan);
                    DrawImage(matchedSpan, altText, uri, optTitle);
                }
            }

            {
                Regex emphasisRegex = new Regex(@"(?<delimiter>[\*_])" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>)" + @"(?:.(?<!\k<delimiter>))+?" + @"\k<delimiter>" + @"(?!(?:\w|\k<delimiter>))");
                foreach (Match match in emphasisRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    DrawMarkerGeometry(matchedSpan);
                }
            }

            {
                Regex strongEmphasisRegex = new Regex(@"(?<delimiter>[\*_]){2}" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"(?:.(?<!\k<delimiter>))+?" + @"\k<delimiter>{2}" + @"(?!(?:\w|\k<delimiter>))");
                foreach (Match match in strongEmphasisRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    DrawMarkerGeometry(matchedSpan);
                }
            }

            {
                Regex strikethroughRegex = new Regex(@"(?<delimiter>~){2}" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"(?:.(?<!\k<delimiter>))+?" + @"\k<delimiter>{2}" + @"(?!(?:\w|\k<delimiter>))");
                foreach (Match match in strikethroughRegex.Matches(span.GetText()))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                    DrawMarkerGeometry(matchedSpan);
                }
            }
        }

        private void DrawMarkerGeometry(SnapshotSpan span)
        {
            IWpfTextViewLineCollection textViewLines = _view.TextViewLines;
            Geometry g = textViewLines.GetMarkerGeometry(span);
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
        }

        private void DrawImage(SnapshotSpan span, string altText, string uri, string optTitle)
        {
            Geometry g = _view.TextViewLines.GetMarkerGeometry(span);
            if (g != null)
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
                    // TODO: set alt text instead?
                }
#endif
                image.Source = imageSource;

                if(optTitle != "")
                {
                    //ToolTip tt = new ToolTip();
                    //tt.Content = optTitle;
                    //image.ToolTip = tt;
                    image.ToolTip = optTitle;
                }

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
                    imageSource.DownloadFailed += delegate(Object sender, ExceptionEventArgs e)
                    {
                        // TODO: set alt text instead?
                    };
                }

                _layer.AddAdornment(span, null, image);
            }
        }
    }
}
