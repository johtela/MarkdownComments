using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System.Collections.Generic;

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

            // Loop through each comment span
            SnapshotSpan lineSpan = line.Extent;
            IList<ClassificationSpan> classificationSpanList = _classifier.GetClassificationSpans(lineSpan);
            foreach(ClassificationSpan classificationSpan in classificationSpanList)
            {
                if(classificationSpan.ClassificationType.IsOfType("comment"))
                {
                    SnapshotSpan span = classificationSpan.Span;
                    //DrawMarkerGeometry(span);

                    Regex regex = new Regex(@"\!\[([^\]]+)\]");
                    Match match = regex.Match(span.GetText());
                    if(match.Success)
                    {
                        SnapshotSpan imageSpan = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                        DrawMarkerGeometry(imageSpan);

                        Geometry g = _view.TextViewLines.GetMarkerGeometry(imageSpan);
                        if (g != null)
                        {
                            Image image = new Image();
                            BitmapFrame bitmapFrame = BitmapFrame.Create(new Uri("https://github.com/adam-p/markdown-here/raw/master/src/common/images/icon48.png", UriKind.RelativeOrAbsolute));
                            image.Source = bitmapFrame;

                            //Align the image with the bottom of the bounds of the text geometry
                            Canvas.SetLeft(image, g.Bounds.Left);
                            if (!bitmapFrame.IsDownloading)
                                Canvas.SetTop(image, g.Bounds.Bottom - image.Source.Height);
                            else
                            {
                                Canvas.SetTop(image, g.Bounds.Top);
                                bitmapFrame.DownloadCompleted += delegate(Object sender, EventArgs e)
                                {
                                    Canvas.SetTop(image, g.Bounds.Bottom - image.Source.Height);
                                };
                            }

                            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, imageSpan, null, image, null);
                        }
                    }
                }
            }

            ////Loop through each character, and place a box around any a 
            //int start = line.Start;
            //int end = line.End;
            //for (int i = start; (i < end); ++i)
            //{
            //    if (_view.TextSnapshot[i] == 'a')
            //    {
            //        SnapshotSpan span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(i, i + 1));
            //        DrawMarkerGeometry(span);
            //    }
            //}
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

                _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
            }
        }
    }
}
