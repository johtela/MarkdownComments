using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownComments
{

    static class ClassificationTypes
    {
        public const String MarkdownComments = "MarkdownComments";
        [Export]
        [Name(MarkdownComments)]
        internal static ClassificationTypeDefinition MarkdownCommentsDefinition = null;

        // ***

        public const String Header = "MarkdownComments.Header";

        [Export]
        [Name(Header)]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition HeaderDefinition = null;

        //[Export(typeof(EditorFormatDefinition))]
        //[ClassificationType(ClassificationTypeNames = Header)]
        //[Name(Header)]
        //[DisplayName(Header)]
        //[UserVisible(false)]
        //[Order(After = Priority.Default, Before = Priority.High)]
        //public sealed class HeaderFormat : ClassificationFormatDefinition
        //{
        //    public HeaderFormat()
        //    {
        //        IsBold = true;
        //    }
        //}

        // ***

        public const String H1 = "MarkdownComments.Header.H1";

        [Export]
        [Name(H1)]
        [BaseDefinition(Header)]
        internal static ClassificationTypeDefinition H1Definition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = H1)]
        [Name(H1)]
        [DisplayName(H1)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class H1Format : ClassificationFormatDefinition
        {
            public H1Format()
            {
                FontRenderingSize = 28;
                IsBold = true;
            }
        }

        // ***

        public const String H2 = "MarkdownComments.Header.H2";

        [Export]
        [Name(H2)]
        [BaseDefinition(Header)]
        internal static ClassificationTypeDefinition H2Definition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = H2)]
        [Name(H2)]
        [DisplayName(H2)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class H2Format : ClassificationFormatDefinition
        {
            public H2Format()
            {
                FontRenderingSize = 24;
                IsBold = true;
            }
        }

        // ***

        public const String H3 = "MarkdownComments.Header.H3";

        [Export]
        [Name(H3)]
        [BaseDefinition(Header)]
        internal static ClassificationTypeDefinition H3Definition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = H3)]
        [Name(H3)]
        [DisplayName(H3)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class H3Format : ClassificationFormatDefinition
        {
            public H3Format()
            {
                FontRenderingSize = 18;
                IsBold = true;
            }
        }

        // ***

        public const String H4 = "MarkdownComments.Header.H4";

        [Export]
        [Name(H4)]
        [BaseDefinition(Header)]
        internal static ClassificationTypeDefinition H4Definition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = H4)]
        [Name(H4)]
        [DisplayName(H4)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class H4Format : ClassificationFormatDefinition
        {
            public H4Format()
            {
                FontRenderingSize = 16;
                IsBold = true;
            }
        }

        // ***

        public const String H5 = "MarkdownComments.Header.H5";

        [Export]
        [Name(H5)]
        [BaseDefinition(Header)]
        internal static ClassificationTypeDefinition H5Definition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = H5)]
        [Name(H5)]
        [DisplayName(H5)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class H5Format : ClassificationFormatDefinition
        {
            public H5Format()
            {
                FontRenderingSize = 14;
                IsBold = true;
            }
        }

        // ***

        public const String H6 = "MarkdownComments.Header.H6";

        [Export]
        [Name(H6)]
        [BaseDefinition(Header)]
        internal static ClassificationTypeDefinition H6Definition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = H6)]
        [Name(H6)]
        [DisplayName(H6)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class H6Format : ClassificationFormatDefinition
        {
            public H6Format()
            {
                FontRenderingSize = 14;
                IsBold = true;
            }
        }

        // Emphasis

        public const String Emphasis = "MarkdownComments.Emphasis";

        [Export]
        [Name(Emphasis)]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition EmphasisDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Emphasis)]
        [Name(Emphasis)]
        [DisplayName(Emphasis)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class MarkdownCommentsEmphasisFormat : ClassificationFormatDefinition
        {
            public MarkdownCommentsEmphasisFormat()
            {
                IsItalic = true;
            }
        }

        // ***

        public const String StrongEmphasis = "MarkdownComments.StrongEmphasis";

        [Export]
        [Name(StrongEmphasis)]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition StrongEmphasisDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = StrongEmphasis)]
        [Name(StrongEmphasis)]
        [DisplayName(StrongEmphasis)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class MarkdownCommentsStrongEmphasisFormat : ClassificationFormatDefinition
        {
            public MarkdownCommentsStrongEmphasisFormat()
            {
                IsBold = true;
            }
        }

        // ***

        public const String Strikethrough = "MarkdownComments.Strikethrough";

        [Export]
        [Name(Strikethrough)]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition StrikethroughDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Strikethrough)]
        [Name(Strikethrough)]
        [DisplayName(Strikethrough)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        public sealed class MarkdownCommentsStrikethroughFormat : ClassificationFormatDefinition
        {
            public MarkdownCommentsStrikethroughFormat()
            {
                TextDecorations = System.Windows.TextDecorations.Strikethrough;
            }
        }

        // References

        [Export]
        [Name("MarkdownComments.Reference")]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition ReferenceDefinition = null;

        [Export]
        [Name("MarkdownComments.Reference.Label")]
        [BaseDefinition("MarkdownComments.Reference")]
        internal static ClassificationTypeDefinition ReferenceLabelDefinition = null;

        [Export]
        [Name("MarkdownComments.Reference.Url")]
        [BaseDefinition("MarkdownComments.Reference")]
        internal static ClassificationTypeDefinition ReferenceUrlDefinition = null;

        [Export]
        [Name("MarkdownComments.Reference.Title")]
        [BaseDefinition("MarkdownComments.Reference")]
        internal static ClassificationTypeDefinition ReferenceTitleDefinition = null;

        // Links

        [Export]
        [Name("MarkdownComments.Link")]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition LinkDefinition = null;

        [Export]
        [Name("MarkdownComments.Link.Text")]
        [BaseDefinition("MarkdownComments.Link")]
        internal static ClassificationTypeDefinition LinkTextDefinition = null;

        [Export]
        [Name("MarkdownComments.Link.Url")]
        [BaseDefinition("MarkdownComments.Link")]
        internal static ClassificationTypeDefinition LinkUrlDefinition = null;

        [Export]
        [Name("MarkdownComments.Link.Title")]
        [BaseDefinition("MarkdownComments.link")]
        internal static ClassificationTypeDefinition LinkTitleDefinition = null;

        [Export]
        [Name("MarkdownComments.Link.ReferenceLabel")]
        [BaseDefinition("MarkdownComments.Link")]
        internal static ClassificationTypeDefinition LinkReferenceLabelDefinition = null;

        // Images

        public const String Image = "MarkdownComments.Image";
        [Export]
        [Name(Image)]
        [BaseDefinition(MarkdownComments)]
        internal static ClassificationTypeDefinition ImageDefinition = null;

        [Export]
        [Name("MarkdownComments.Image.AltText")]
        [BaseDefinition("MarkdownComments.Image")]
        internal static ClassificationTypeDefinition ImageAltTextDefinition = null;

        [Export]
        [Name("MarkdownComments.Image.Url")]
        [BaseDefinition("MarkdownComments.Image")]
        internal static ClassificationTypeDefinition ImageUrlDefinition = null;

        [Export]
        [Name("MarkdownComments.Image.Title")]
        [BaseDefinition("MarkdownComments.Image")]
        internal static ClassificationTypeDefinition ImageTitleDefinition = null;

        [Export]
        [Name("MarkdownComments.Image.ReferenceLabel")]
        [BaseDefinition("MarkdownComments.Image")]
        internal static ClassificationTypeDefinition ImageReferenceLabelDefinition = null;
    }

}
