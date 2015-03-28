using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownComments
{
    abstract class MarkdownElement
    {
        public SnapshotSpan Span;

        public MarkdownElement() {}
        public MarkdownElement(SnapshotSpan span) { Span = span; }
    }

    class MarkdownHeader : MarkdownElement
    {
        public SnapshotSpan DelimiterSpan;
        public int Level;

        public MarkdownHeader(SnapshotSpan span, SnapshotSpan delimiter, int level) : base(span) { DelimiterSpan = delimiter; Level = level; }
    }

    class MarkdownEmphasis : MarkdownElement
    {
        public SnapshotSpan StartDelimiterSpan;
        public SnapshotSpan EndDelimiterSpan;

        public MarkdownEmphasis(SnapshotSpan span, SnapshotSpan startDelimiterSpan, SnapshotSpan endDelimiterSpan) : base(span) { StartDelimiterSpan = startDelimiterSpan; EndDelimiterSpan = endDelimiterSpan; }
    }
    class MarkdownStrongEmphasis : MarkdownElement
    {
        public SnapshotSpan StartDelimiterSpan;
        public SnapshotSpan EndDelimiterSpan;

        public MarkdownStrongEmphasis(SnapshotSpan span, SnapshotSpan startDelimiterSpan, SnapshotSpan endDelimiterSpan) : base(span) { StartDelimiterSpan = startDelimiterSpan; EndDelimiterSpan = endDelimiterSpan; }
    }
    class MarkdownStrikethrough : MarkdownElement
    {
        public SnapshotSpan StartDelimiterSpan;
        public SnapshotSpan EndDelimiterSpan;

        public MarkdownStrikethrough(SnapshotSpan span, SnapshotSpan startDelimiterSpan, SnapshotSpan endDelimiterSpan) : base(span) { StartDelimiterSpan = startDelimiterSpan; EndDelimiterSpan = endDelimiterSpan; }
    }

    class MarkdownLink : MarkdownElement
    {
        public string Text;
        public string Uri;

        public MarkdownLink(SnapshotSpan span, string text, string uri) : base(span) { Text = text; Uri = uri; }
    }

    class MarkdownImage : MarkdownLink
    {
        public string AltText { get { return Text; } set{ Text = value; } }
        public string OptTitle;

        public MarkdownImage(SnapshotSpan span, string altText, string uri, string optTitle) : base(span, altText, uri) { OptTitle = optTitle; }
    }

    static class MarkdownCommentsParser
    {

        static IEnumerable<T> GetRegexSpans<T>(SnapshotSpan span, Regex regex, Func<SnapshotSpan, Match, T> elementFactory)
        {
            foreach (Match match in regex.Matches(span.GetText()))
            {
                SnapshotSpan matchedSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                T element = elementFactory(matchedSpan, match);
                yield return element;
            }
        }

        static SnapshotSpan GetMatchSpan(SnapshotSpan span, Group group)
        {
            return new SnapshotSpan(span.Snapshot, Span.FromBounds(span.Start.Position + group.Index, span.Start.Position + group.Index + group.Length));
        }

        public static IEnumerable<MarkdownHeader> GetHeaderSpans(SnapshotSpan span)
        {
            Regex headerRegex = new Regex(@"^[^\w#]*(#{1,6}(?!#)\s*).*");
            return GetRegexSpans<MarkdownHeader>(span, headerRegex, (matchedSpan, match) => {
                return new MarkdownHeader(matchedSpan, GetMatchSpan(span, match.Groups[1]), match.Groups[1].Length);
            });
        }

        public static IEnumerable<MarkdownEmphasis> GetEmphasisSpans(SnapshotSpan span)
        {
            Regex emphasisRegex = new Regex(@"((?<delimiter>[\*_]))" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>)" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>)" + @"(?!(?:\w|\k<delimiter>))");
            return GetRegexSpans<MarkdownEmphasis>(span, emphasisRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetMatchSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetMatchSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetMatchSpan(span, match.Groups[3]);
                return new MarkdownEmphasis(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public static IEnumerable<MarkdownStrongEmphasis> GetStrongEmphasisSpans(SnapshotSpan span)
        {
            Regex strongEmphasisRegex = new Regex(@"((?<delimiter>[\*_]){2})" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>{2})" + @"(?!(?:\w|\k<delimiter>))");
            return GetRegexSpans<MarkdownStrongEmphasis>(span, strongEmphasisRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetMatchSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetMatchSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetMatchSpan(span, match.Groups[3]);
                return new MarkdownStrongEmphasis(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public static IEnumerable<MarkdownStrikethrough> GetStrikethroughSpans(SnapshotSpan span)
        {
            Regex strikethroughRegex = new Regex(@"((?<delimiter>~){2})" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>{2})" + @"(?!(?:\w|\k<delimiter>))");
            return GetRegexSpans<MarkdownStrikethrough>(span, strikethroughRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetMatchSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetMatchSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetMatchSpan(span, match.Groups[3]);
                return new MarkdownStrikethrough(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public static IEnumerable<MarkdownImage> GetImageSpans(SnapshotSpan span)
        {
            //string textRegex = @"\[([^\[\]]*)\]";
            string textRegex = @"(?<titleOpen>\[)([^\[\]]*)(?<titleClose-titleOpen>\])(?(titleOpen)(?!))";
            string optTitleRegex = @"(?:\s+""([^""\(\)]*)"")?";
            string urlRegex = @"\(([^\s\)]+)" + optTitleRegex + @"\)";
            //Regex inlineLinkRegex = new Regex(@"(?<!\!)" + textRegex + urlRegex);
            Regex inlineImageRegex = new Regex(@"\!" + textRegex + urlRegex);
            return GetRegexSpans<MarkdownImage>(span, inlineImageRegex, (matchedSpan, match) => { return new MarkdownImage(matchedSpan, match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value); });
        }

    }
}
