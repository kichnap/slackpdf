namespace SlackPDF.Core.Models;

public record PageRange(int From, int? To);

public class PageSelection
{
    public bool SelectAll { get; }
    public IReadOnlyList<PageRange> Ranges { get; }

    public PageSelection()
    {
        SelectAll = true;
        Ranges = [];
    }

    private PageSelection(IReadOnlyList<PageRange> ranges)
    {
        SelectAll = false;
        Ranges = ranges;
    }

    public static PageSelection All => new();

    public static PageSelection Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return All;

        var ranges = new List<PageRange>();
        foreach (var part in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.EndsWith('-'))
            {
                if (int.TryParse(part.TrimEnd('-'), out int from))
                    ranges.Add(new PageRange(from, null));
            }
            else if (part.Contains('-'))
            {
                var idx = part.IndexOf('-');
                if (int.TryParse(part[..idx], out int from) && int.TryParse(part[(idx + 1)..], out int to))
                    ranges.Add(new PageRange(from, to));
            }
            else if (int.TryParse(part, out int page))
            {
                ranges.Add(new PageRange(page, page));
            }
        }

        return ranges.Count == 0 ? All : new PageSelection(ranges);
    }

    public bool Contains(int pageNumber)
    {
        if (SelectAll) return true;
        foreach (var r in Ranges)
        {
            if (pageNumber >= r.From && (r.To == null || pageNumber <= r.To))
                return true;
        }
        return false;
    }

    public IEnumerable<int> Resolve(int totalPages)
    {
        if (SelectAll)
            return Enumerable.Range(1, totalPages);

        var result = new SortedSet<int>();
        foreach (var r in Ranges)
        {
            int end = r.To ?? totalPages;
            for (int i = r.From; i <= Math.Min(end, totalPages); i++)
                result.Add(i);
        }
        return result;
    }
}
