namespace Narumikazuchi.Neon.FileIndexer;

[DebuggerDisplay("{File.Name}")]
public sealed partial class IndexEntry
{
    public FileInfo File { get; }

    public ICollection<String> Keywords =>
        m_Keywords;
}

// Non-Public
partial class IndexEntry
{
    internal IndexEntry(FileInfo file) :
        this(file: file,
             keywords: Array.Empty<String>())
    { }
    internal IndexEntry(FileInfo file,
                        IEnumerable<String> keywords)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(keywords);

        this.File = file;
        m_Keywords = new(collection: keywords);
    }

    internal static IndexEntry FromBytes(ReadOnlySpan<Byte> bytes)
    {
        Int32 count = BitConverter.ToInt32(bytes);
        String path = bytes[..(count + sizeof(Int32))].ToUTF8String()
                                                      .ToLower();

        ReadOnlySpan<Byte> rest = bytes[(count + sizeof(Int32))..];
        count = BitConverter.ToInt32(rest);
        HashSet<String> keywords = new(path.SplitNormalised());

        rest = rest[sizeof(Int32)..];
        for (Int32 i = 0;
             i < count;
             i++)
        {
            Int32 size = BitConverter.ToInt32(rest);
            String keyword = rest[..(size + sizeof(Int32))].ToUTF8String();
            keywords.Add(keyword);
            rest = rest[(size + sizeof(Int32))..];
        }

        return new(file: new(path),
                   keywords: keywords);
    }

    internal Byte[] ToByteArray()
    {
        Byte[] name = this.File
                          .FullName
                          .ToUTF8ByteArray();

        Byte[] count = BitConverter.GetBytes(this.Keywords.Count);
        List<Byte> keywords = new();
        foreach (String keyword in this.Keywords)
        {
            keywords.AddRange(keyword.ToLower()
                                     .ToUTF8ByteArray());
        }

        return name.Concat(count)
                   .Concat(keywords)
                   .ToArray();
    }

    private readonly HashSet<String> m_Keywords;
}