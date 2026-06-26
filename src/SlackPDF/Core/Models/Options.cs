namespace SlackPDF.Core.Models;

public enum BookmarkBehavior  { Merge, Discard, OneEntryPerFile }
public enum AcroFormBehavior  { Discard, Merge, MergeWithRename }
public enum SplitMode         { EveryPage, EveryNPages, AtPages, BySize, ByBookmarks, EvenOdd }
public enum ExtractMode       { SingleFile, OneFilePerPage, OneFilePerRange }
public enum InsertMode        { AtPosition, EveryNPages, AfterEveryPage }

public record MergeOptions(
    BookmarkBehavior Bookmarks,
    AcroFormBehavior AcroForms,
    bool AddTableOfContents);

public record SplitOptions(
    SplitMode Mode,
    int? NPages,
    int[]? AtPages,
    double? MaxSizeMb,
    int? BookmarkLevel,
    string FileNamePrefix);

public record InsertOptions(
    InsertMode Mode,
    int Position);
