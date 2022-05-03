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

        String path = file.FullName.ToLower();
        if (m_Files.ContainsKey(path) ||
            m_Excluded.Contains(path))
        {
            return;
        }

        List<String> keywords = new(tags);

        String[] words = path.SplitNormalised();
        keywords.AddRange(words);

        IndexEntry entry = new(file: file,
                               keywords: keywords);
        foreach (String keyword in keywords.Select(x => x.ToLower()))
        {
            if (m_KeywordMap.ContainsKey(keyword))
            {
                m_KeywordMap[keyword].Add(entry);
                continue;
            }
            else
            {
                m_KeywordMap.Add(key: keyword,
                            value: new() { entry });
                continue;
            }
        }

        m_Files.Add(key: path,
                    value: entry);
    }

    public void Remove(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        String path = file.FullName.ToLower();
        m_Excluded.Add(path);
        if (!m_Files.ContainsKey(path))
        {
            return;
        }

        foreach (String keyword in m_Files[path].Keywords)
        {
            m_KeywordMap[keyword].Remove(m_Files[path]);
        }

        m_Files.Remove(path);

        List<String> orphans = new();
        foreach (String keyword in m_KeywordMap.Keys)
        {
            if (m_KeywordMap[keyword].Count == 0)
            {
                orphans.Add(keyword);
            }
        }

        foreach (String keyword in orphans)
        {
            m_KeywordMap.Remove(keyword);
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
        String path = entry.File.FullName.ToLower();
        if (m_Files.ContainsKey(path))
        {
            return;
        }
        m_Files.Add(key: path,
                    value: entry);

        foreach (String keyword in entry.Keywords)
        {
            if (m_KeywordMap.ContainsKey(keyword))
            {
                m_KeywordMap[keyword].Add(entry);
                continue;
            }
            else
            {
                m_KeywordMap.Add(key: keyword,
                                 value: new() { entry });
                continue;
            }
        }
    }

    internal readonly SortedDictionary<String, List<IndexEntry>> m_KeywordMap = new();
    internal readonly SortedDictionary<String, IndexEntry> m_Files = new();
    private readonly List<String> m_Excluded = new();
}

// IEnumerable
partial class IndexDocument : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() =>
        m_Files.Values
               .OrderBy(x => x.File.Name)
               .GetEnumerator();
}

// IEnumerable<T>
partial class IndexDocument : IEnumerable<IndexEntry>
{
    public IEnumerator<IndexEntry> GetEnumerator() =>
        m_Files.Values
               .OrderBy(x => x.File.Name)
               .GetEnumerator();
}

// IReadOnlyCollection<T>
partial class IndexDocument : IReadOnlyCollection<IndexEntry>
{
    public Int32 Count =>
        m_Files.Count;
}