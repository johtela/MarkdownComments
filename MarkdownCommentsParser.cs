using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownComments
{
    abstract class MarkdownElement
    {
        public SnapshotSpan Span { get; set; }

        public MarkdownElement() {}
        public MarkdownElement(SnapshotSpan span) { Span = span; }
    }

    class MarkdownHeader : MarkdownElement
    {
        public SnapshotSpan DelimiterSpan { get; set; }
        public int Level { get; set; }

        public MarkdownHeader(SnapshotSpan span, SnapshotSpan delimiter, int level) : base(span) { DelimiterSpan = delimiter; Level = level; }
    }

    class MarkdownEmphasis : MarkdownElement
    {
        public SnapshotSpan StartDelimiterSpan { get; set; }
        public SnapshotSpan EndDelimiterSpan { get; set; }

        public MarkdownEmphasis(SnapshotSpan span, SnapshotSpan startDelimiterSpan, SnapshotSpan endDelimiterSpan) : base(span) { StartDelimiterSpan = startDelimiterSpan; EndDelimiterSpan = endDelimiterSpan; }
    }
    class MarkdownStrongEmphasis : MarkdownElement
    {
        public SnapshotSpan StartDelimiterSpan { get; set; }
        public SnapshotSpan EndDelimiterSpan { get; set; }

        public MarkdownStrongEmphasis(SnapshotSpan span, SnapshotSpan startDelimiterSpan, SnapshotSpan endDelimiterSpan) : base(span) { StartDelimiterSpan = startDelimiterSpan; EndDelimiterSpan = endDelimiterSpan; }
    }
    class MarkdownStrikethrough : MarkdownElement
    {
        public SnapshotSpan StartDelimiterSpan { get; set; }
        public SnapshotSpan EndDelimiterSpan { get; set; }

        public MarkdownStrikethrough(SnapshotSpan span, SnapshotSpan startDelimiterSpan, SnapshotSpan endDelimiterSpan) : base(span) { StartDelimiterSpan = startDelimiterSpan; EndDelimiterSpan = endDelimiterSpan; }
    }

    class MarkdownLink : MarkdownElement
    {
        public SnapshotSpan TextSpan { get; set; }
        public SnapshotSpan UriSpan { get; set; }

        public MarkdownLink(SnapshotSpan span, SnapshotSpan textSpan, SnapshotSpan uriSpan) : base(span) { TextSpan = textSpan; UriSpan = uriSpan; }
    }

    class MarkdownImage : MarkdownLink
    {
        public SnapshotSpan AltTextSpan { get { return TextSpan; } }
        public SnapshotSpan OptTitleSpan { get; set; }

        public MarkdownImage(SnapshotSpan span, SnapshotSpan altTextSpan, SnapshotSpan uriSpan, SnapshotSpan optTitleSpan) : base(span, altTextSpan, uriSpan) { OptTitleSpan = optTitleSpan; }
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

        static SnapshotSpan GetGroupSpan(SnapshotSpan regexSpan, Group group)
        {
            return new SnapshotSpan(regexSpan.Snapshot, Span.FromBounds(regexSpan.Start.Position + group.Index, regexSpan.Start.Position + group.Index + group.Length));
        }

        public static IEnumerable<MarkdownHeader> GetHeaderSpans(SnapshotSpan span)
        {
            Regex headerRegex = new Regex(@"^[^\w#]*(#{1,6}(?!#)\s*).*");
            return GetRegexSpans<MarkdownHeader>(span, headerRegex, (matchedSpan, match) => {
                return new MarkdownHeader(matchedSpan, GetGroupSpan(span, match.Groups[1]), match.Groups[1].Length);
            });
        }

        public static IEnumerable<MarkdownEmphasis> GetEmphasisSpans(SnapshotSpan span)
        {
            Regex emphasisRegex = new Regex(@"((?<delimiter>[\*_]))" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>)" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>)" + @"(?!(?:\w|\k<delimiter>))");
            return GetRegexSpans<MarkdownEmphasis>(span, emphasisRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetGroupSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownEmphasis(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public static IEnumerable<MarkdownStrongEmphasis> GetStrongEmphasisSpans(SnapshotSpan span)
        {
            Regex strongEmphasisRegex = new Regex(@"((?<delimiter>[\*_]){2})" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>{2})" + @"(?!(?:\w|\k<delimiter>))");
            return GetRegexSpans<MarkdownStrongEmphasis>(span, strongEmphasisRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetGroupSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownStrongEmphasis(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public static IEnumerable<MarkdownStrikethrough> GetStrikethroughSpans(SnapshotSpan span)
        {
            Regex strikethroughRegex = new Regex(@"((?<delimiter>~){2})" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>{2})" + @"(?!(?:\w|\k<delimiter>))");
            return GetRegexSpans<MarkdownStrikethrough>(span, strikethroughRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetGroupSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetGroupSpan(span, match.Groups[3]);
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
            return GetRegexSpans<MarkdownImage>(span, inlineImageRegex, (matchedSpan, match) => {
                SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[1]);
                SnapshotSpan uriSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan titleSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownImage(matchedSpan, textSpan, uriSpan, titleSpan);
            });
        }

    }
}
