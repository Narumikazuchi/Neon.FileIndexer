namespace Narumikazuchi.Neon.FileIndexer;

public sealed partial class IndexSearcher
{
    public IndexSearcher(IIndexReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        m_Reader = reader;
    }

    public IReadOnlyCollection<IndexEntry> Search(String pattern) =>
        this.Search(pattern: pattern,
                    exclude: Array.Empty<String>(),
                    maxResults: -1);
    public IReadOnlyCollection<IndexEntry> Search(String pattern,
                                                  IEnumerable<String> exclude) =>
        this.Search(pattern: pattern,
                    exclude: exclude,
                    maxResults: -1);
    public IReadOnlyCollection<IndexEntry> Search(String pattern,
                                                  in Int32 maxResults) =>
        this.Search(pattern: pattern,
                    exclude: Array.Empty<String>(),
                    maxResults: maxResults);
    public IReadOnlyCollection<IndexEntry> Search(String pattern,
                                                  IEnumerable<String> exclude,
                                                  in Int32 maxResults)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(exclude);

        IEnumerable<String> filters = pattern.SplitNormalised()
                                             .Select(x => x.ToLower());

        List<IndexEntry> result = new();
        if (maxResults > 0)
        {
            result.AddRange(this.GetLimitedEntries(filters: filters,
                                                   exclude: exclude,
                                                   maxItems: maxResults));
        }
        else
        {
            result.AddRange(this.GetUnlimitedEntries(filters: filters,
                                                     exclude: exclude));
        }
        result.SortByFileName();

        return result;
    }
}

// Non-Public
partial class IndexSearcher
{
    private static Boolean IsContainedInEntry(String exclude,
                                              IndexEntry entry) =>
        entry.Keywords
             .Any(x => x.Contains(exclude));

    private IEnumerable<IndexEntry> GetLimitedEntries(IEnumerable<String> filters,
                                                      IEnumerable<String> exclude,
                                                      in Int32 maxItems)
    {
        HashSet<IndexEntry> entries = new(__IndexEntryComparer.Instance);

        String? first = filters.FirstOrDefault();
        if (first is null)
        {
            return entries;
        }

        foreach (IndexEntry entry in m_Reader.ReadByKeyword(first))
        {
            if (!exclude.Any(x => IsContainedInEntry(x, entry)))
            {
                entries.Add(entry);
            }
        }

        foreach (String filter in filters.Skip(1))
        {
            HashSet<IndexEntry> temp = new(__IndexEntryComparer.Instance);
            foreach (IndexEntry entry in m_Reader.ReadByKeyword(filter))
            {
                if (!exclude.Any(x => IsContainedInEntry(x, entry)))
                {
                    temp.Add(entry);
                }
            }

            List<IndexEntry> remove = new();
            foreach (IndexEntry entry in entries)
            {
                if (!temp.Contains(entry))
                {
                    remove.Add(entry);
                }
            }

            foreach (IndexEntry entry in remove)
            {
                entries.Remove(entry);
            }
        }

        if (entries.Count > maxItems)
        {
            return entries.Take(maxItems);
        }
        else
        {
            return entries;
        }
    }

    private IEnumerable<IndexEntry> GetUnlimitedEntries(IEnumerable<String> filters,
                                                        IEnumerable<String> exclude)
    {
        HashSet<IndexEntry> entries = new(__IndexEntryComparer.Instance);

        String? first = filters.FirstOrDefault();
        if (first is null)
        {
            return entries;
        }

        foreach (IndexEntry entry in m_Reader.ReadByKeyword(first))
        {
            if (!exclude.Any(x => IsContainedInEntry(x, entry)))
            {
                entries.Add(entry);
            }
        }

        foreach (String filter in filters.Skip(1))
        {
            HashSet<IndexEntry> temp = new(__IndexEntryComparer.Instance);
            foreach (IndexEntry entry in m_Reader.ReadByKeyword(filter))
            {
                if (!exclude.Any(x => IsContainedInEntry(x, entry)))
                {
                    temp.Add(entry);
                }
            }

            List<IndexEntry> remove = new();
            foreach (IndexEntry entry in entries)
            {
                if (!temp.Contains(entry))
                {
                    remove.Add(entry);
                }
            }

            foreach (IndexEntry entry in remove)
            {
                entries.Remove(entry);
            }
        }

        return entries;
    }

    private readonly IIndexReader m_Reader;
}