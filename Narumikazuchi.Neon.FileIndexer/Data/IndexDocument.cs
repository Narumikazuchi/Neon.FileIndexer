namespace Narumikazuchi.Neon.FileIndexer;

public sealed partial class IndexDocument
{
    public void Add(FileInfo file) => 
        this.Add(file: file,
                 tags: Array.Empty<String>());
    public void Add(FileInfo file,
                    IEnumerable<String> tags)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(tags);

        if (m_Files.BinarySearch(file.FullName.ToLower()) > -1)
        {
            return;
        }
        m_Files.Add(file.FullName.ToLower());
        m_Files.Sort();

        List<String> keywords = new(tags);

        String[] words = file.FullName.ToLower()
                                      .SplitNormalised();
        keywords.AddRange(words);

        IndexEntry entry = new(file: file,
                               keywords: keywords);
        foreach (String keyword in keywords.Select(x => x.ToLower()))
        {
            if (m_Items.ContainsKey(keyword))
            {
                m_Items[keyword].Add(entry);
                continue;
            }
            else
            {
                m_Items.Add(key: keyword,
                            value: new() { entry });
                continue;
            }
        }
    }
}

// Non-Public
partial class IndexDocument
{
    internal IndexDocument()
    { }

    internal void Add(IndexEntry entry)
    {
        if (m_Files.BinarySearch(entry.File.FullName.ToLower()) > -1)
        {
            return;
        }
        m_Files.Add(entry.File.FullName.ToLower());
        m_Files.Sort();

        foreach (String keyword in entry.Keywords)
        {
            if (m_Items.ContainsKey(keyword))
            {
                m_Items[keyword].Add(entry);
                continue;
            }
            else
            {
                m_Items.Add(key: keyword,
                            value: new() { entry });
                continue;
            }
        }
    }

    internal SortedDictionary<String, List<IndexEntry>> Items =>
        m_Items;

    private readonly SortedDictionary<String, List<IndexEntry>> m_Items = new();
    private readonly List<String> m_Files = new();
}

// IEnumerable
partial class IndexDocument : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() =>
        m_Items.Values
               .SelectMany(x => x)
               .Distinct(__IndexEntryComparer.Instance)
               .OrderBy(x => x.File.Name)
               .GetEnumerator();
}

// IEnumerable<T>
partial class IndexDocument : IEnumerable<IndexEntry>
{
    public IEnumerator<IndexEntry> GetEnumerator() =>
        m_Items.Values
               .SelectMany(x => x)
               .Distinct(__IndexEntryComparer.Instance)
               .OrderBy(x => x.File.Name)
               .GetEnumerator();
}

// IReadOnlyCollection<T>
partial class IndexDocument : IReadOnlyCollection<IndexEntry>
{
    public Int32 Count =>
        m_Files.Count;
}