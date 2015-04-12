using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    class MarkdownCommentsParser
    {
        [DefaultValue(true)]
        public bool SkipPreprocessor { set { MakeHeaderRegex(value); } }

        Regex _headerRegex;
        Regex _emphasisRegex;
        Regex _strongEmphasisRegex;
        Regex _strikethroughRegex;
        Regex _inlineImageRegex;

        public MarkdownCommentsParser()
        {
            {
                MakeHeaderRegex(true);
            }
            {
                _emphasisRegex = new Regex(@"((?<delimiter>[\*_]))" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>)" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>)" + @"(?!(?:\w|\k<delimiter>))", RegexOptions.Compiled);
            }
            {
                _strongEmphasisRegex = new Regex(@"((?<delimiter>[\*_]){2})" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>{2})" + @"(?!(?:\w|\k<delimiter>))", RegexOptions.Compiled);
            }
            {
                _strikethroughRegex = new Regex(@"((?<delimiter>~){2})" + @"(?<!(?:\w|\k<delimiter>)\k<delimiter>{2})" + @"((?:.(?<!\k<delimiter>))+?)" + @"(\k<delimiter>{2})" + @"(?!(?:\w|\k<delimiter>))", RegexOptions.Compiled);
            }
            {
                //string textRegex = @"\[([^\[\]]*)\]";
                string textRegex = @"(?<titleOpen>\[)([^\[\]]*)(?<titleClose-titleOpen>\])(?(titleOpen)(?!))";
                string optTitleRegex = @"(?:\s+""([^""\(\)]*)"")?";
                string urlRegex = @"\(([^\s\)]+)" + optTitleRegex + @"\)";
                //_inlineLinkRegex = new Regex(@"(?<!\!)" + textRegex + urlRegex);
                _inlineImageRegex = new Regex(@"\!" + textRegex + urlRegex, RegexOptions.Compiled);
            }
        }

        void MakeHeaderRegex(bool _skipProprocessor)
        {
            // C/C++: #define #undef #include #if #ifdef #ifndef #else #elif #endif #line #error #pragma
            // C#: #if #else #elif #endif #define #undef #warning #error #line #region #endregion #pragma
            string skipIncludesPattern = _skipProprocessor ? @"(?!#(?:define|undef|include|if|ifdef|ifndef|else|elif|endif|line|error|pragma|warning|region|endregion))" : @"";

            _headerRegex = new Regex(@"^[^\w#]*" + skipIncludesPattern + @"((#{1,6})(?!#)\s*).*", RegexOptions.Compiled);
        }

        IEnumerable<T> GetRegexSpans<T>(SnapshotSpan span, Regex regex, Func<SnapshotSpan, Match, T> elementFactory)
        {
            foreach (Match match in regex.Matches(span.GetText()))
            {
                SnapshotSpan matchedSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(span.Start.Position + match.Index, span.Start.Position + match.Index + match.Length));
                T element = elementFactory(matchedSpan, match);
                yield return element;
            }
        }

        SnapshotSpan GetGroupSpan(SnapshotSpan regexSpan, Group group)
        {
            return new SnapshotSpan(regexSpan.Snapshot, Span.FromBounds(regexSpan.Start.Position + group.Index, regexSpan.Start.Position + group.Index + group.Length));
        }

        public IEnumerable<MarkdownHeader> GetHeaderSpans(SnapshotSpan span)
        {
            return GetRegexSpans<MarkdownHeader>(span, _headerRegex, (matchedSpan, match) => {
                return new MarkdownHeader(matchedSpan, GetGroupSpan(span, match.Groups[1]), match.Groups[2].Length);
            });
        }

        public IEnumerable<MarkdownEmphasis> GetEmphasisSpans(SnapshotSpan span)
        {
            return GetRegexSpans<MarkdownEmphasis>(span, _emphasisRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetGroupSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownEmphasis(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public IEnumerable<MarkdownStrongEmphasis> GetStrongEmphasisSpans(SnapshotSpan span)
        {
            return GetRegexSpans<MarkdownStrongEmphasis>(span, _strongEmphasisRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetGroupSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownStrongEmphasis(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public IEnumerable<MarkdownStrikethrough> GetStrikethroughSpans(SnapshotSpan span)
        {
            return GetRegexSpans<MarkdownStrikethrough>(span, _strikethroughRegex, (matchedSpan, match) => {
                SnapshotSpan startDelimiterSpan = GetGroupSpan(span, match.Groups[1]);
                //SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan endDelimiterSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownStrikethrough(matchedSpan, startDelimiterSpan, endDelimiterSpan);
            });
        }

        public IEnumerable<MarkdownImage> GetImageSpans(SnapshotSpan span)
        {
            return GetRegexSpans<MarkdownImage>(span, _inlineImageRegex, (matchedSpan, match) => {
                SnapshotSpan textSpan = GetGroupSpan(span, match.Groups[1]);
                SnapshotSpan uriSpan = GetGroupSpan(span, match.Groups[2]);
                SnapshotSpan titleSpan = GetGroupSpan(span, match.Groups[3]);
                return new MarkdownImage(matchedSpan, textSpan, uriSpan, titleSpan);
            });
        }

    }
}
