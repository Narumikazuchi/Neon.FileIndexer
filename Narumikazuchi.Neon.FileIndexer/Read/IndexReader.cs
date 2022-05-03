namespace Narumikazuchi.Neon.FileIndexer;

public sealed partial class IndexReader
{
    public IndexReader(String indexStorageLocation) :
        this(new DirectoryInfo(indexStorageLocation))
    { }
    public IndexReader(DirectoryInfo indexStorageLocation)
    {
        ArgumentNullException.ThrowIfNull(indexStorageLocation);

        this.IndexStoreLocation = indexStorageLocation;
        if (!indexStorageLocation.Exists)
        {
            Directory.CreateDirectory(indexStorageLocation.FullName);
        }

        m_DictionaryFile = new(Path.Combine(indexStorageLocation.FullName,
                                            "dictionary"));
        m_KeywordFile = new(Path.Combine(indexStorageLocation.FullName,
                                         "keywords"));
        m_AddressFile = new(Path.Combine(indexStorageLocation.FullName,
                                         "addresses"));
        m_DataFile = new(Path.Combine(indexStorageLocation.FullName,
                                      "data"));

        m_DictionaryMemoryFile = CreateMemoryMappedFile(m_DictionaryFile);
        m_KeywordMemoryFile = CreateMemoryMappedFile(m_KeywordFile);
        m_AddressMemoryFile = CreateMemoryMappedFile(m_AddressFile);
        m_DataMemoryFile = CreateMemoryMappedFile(m_DataFile);
    }
}

// Non-Public
partial class IndexReader
{
    private static MemoryMappedFile CreateMemoryMappedFile(FileInfo file)
    {
        FileStream stream = new(path: file.FullName,
                                mode: FileMode.OpenOrCreate);
        return MemoryMappedFile.CreateFromFile(fileStream: stream,
                                               mapName: file.Name,
                                               capacity: 0,
                                               access: MemoryMappedFileAccess.ReadWrite,
                                               inheritability: HandleInheritability.None,
                                               leaveOpen: false);
    }

    private __Address FindSection(in Char letter)
    {
        using MemoryMappedViewAccessor accessor = m_DictionaryMemoryFile.CreateViewAccessor();
        Int64 length = m_DictionaryFile.Length;

        Int64 offset = 0L;
        while (offset < length)
        {
            Byte charCount = accessor.ReadByte(offset++);
            List<Byte> charBytes = new();
            for (Int32 i = 0;
                 i < charCount;
                 i++)
            {
                charBytes.Add(accessor.ReadByte(offset++));
            }
            Char current = Encoding.UTF8.GetString(charBytes.ToArray())[0];
            if (current == letter)
            {
                List<Byte> bytes = new();
                for (Int32 i = 0;
                     i < 8;
                     i++)
                {
                    bytes.Add(accessor.ReadByte(offset++));
                }
                Int64 start = BitConverter.ToInt64(bytes.ToArray());

                bytes.Clear();
                for (Int32 i = 0;
                     i < 8;
                     i++)
                {
                    bytes.Add(accessor.ReadByte(offset++));
                }
                Int64 addressLength = BitConverter.ToInt64(bytes.ToArray());
                return new() { Start = start, Length = addressLength };
            }

            unsafe
            {
                offset += sizeof(__Address);
            }
        }

        return new() { Start = -1L, Length = 0L };
    }

    private IReadOnlyCollection<__Address> GetAddressLists(String keyword,
                                                           in __Address address)
    {
        using MemoryMappedViewAccessor accessor = m_KeywordMemoryFile.CreateViewAccessor(offset: address.Start,
                                                                                         size: address.Length);

        List<__Address> options = new();

        Int64 offset = 0L;
        while (offset < address.Length)
        {
            List<Byte> bytes = new();
            while (bytes.Count < sizeof(Int32))
            {
                bytes.Add(accessor.ReadByte(offset++));
            }
            Int32 count = BitConverter.ToInt32(bytes.ToArray());
            while (bytes.Count < sizeof(Int32) + count)
            {
                bytes.Add(accessor.ReadByte(offset++));
            }

            ReadOnlySpan<Byte> keywordBytes = bytes.ToArray();
            String current = keywordBytes.ToUTF8String();

            if (current.StartsWith(keyword) ||
                current.Contains(keyword))
            {
                bytes.Clear();
                unsafe
                {
                    for (Int32 i = 0;
                         i < sizeof(__Address);
                         i++)
                    {
                        bytes.Add(accessor.ReadByte(offset + i));
                    }
                }
                ReadOnlySpan<Byte> addressBytes = bytes.ToArray();
                Int64 start = BitConverter.ToInt64(addressBytes[..sizeof(Int64)]);
                Int64 length = BitConverter.ToInt64(addressBytes[sizeof(Int64)..]);

                options.Add(new()
                {
                    Start = start,
                    Length = length,
                });
            }
            unsafe
            {
                offset += sizeof(__Address);
            }
        }

        return options;
    }

    private IEnumerable<__Address> GetDataAdresses(in __Address address)
    {
        using MemoryMappedViewAccessor accessor = m_AddressMemoryFile.CreateViewAccessor(offset: address.Start,
                                                                                         size: address.Length);

        List<Byte> bytes = new();
        Int64 offset = 0L;
        while (offset < address.Length)
        {
            bytes.Add(accessor.ReadByte(offset++));
        }

        ReadOnlySpan<Byte> listBytes = bytes.ToArray();
        Int32 count = BitConverter.ToInt32(listBytes[..sizeof(Int32)]);

        List<__Address> result = new();
        ReadOnlySpan<Byte> dataBytes = listBytes[sizeof(Int32)..];
        for (Int32 i = 0;
             i < count;
             i++)
        {
            unsafe
            {
                ReadOnlySpan<Byte> addressBytes = dataBytes.Slice(start: sizeof(__Address) * i,
                                                                  length: sizeof(__Address));
                __Address current = new()
                {
                    Start = BitConverter.ToInt64(addressBytes[..sizeof(Int64)]),
                    Length = BitConverter.ToInt64(addressBytes[sizeof(Int64)..])
                };
                result.Add(current);
            }
        }

        return result;
    }

    private IndexEntry GetEntry(in __Address address)
    {
        using MemoryMappedViewAccessor accessor = m_DataMemoryFile.CreateViewAccessor(offset: address.Start,
                                                                                      size: address.Length);

        List<Byte> bytes = new();
        Int64 offset = 0L;
        while (offset < address.Length)
        {
            bytes.Add(accessor.ReadByte(offset++));
        }

        return IndexEntry.FromBytes(bytes.Skip(sizeof(Int32)).ToArray());
    }

    private readonly FileInfo m_DictionaryFile;
    private readonly FileInfo m_KeywordFile;
    private readonly FileInfo m_AddressFile;
    private readonly FileInfo m_DataFile;
    private readonly MemoryMappedFile m_DictionaryMemoryFile;
    private readonly MemoryMappedFile m_KeywordMemoryFile;
    private readonly MemoryMappedFile m_AddressMemoryFile;
    private readonly MemoryMappedFile m_DataMemoryFile;
    private Boolean m_IsDisposed;
}

// IDisposable
partial class IndexReader : IDisposable
{
    public void Dispose()
    {
        if (m_IsDisposed)
        {
            return;
        }

        m_DictionaryMemoryFile.Dispose();
        m_KeywordMemoryFile.Dispose();
        m_AddressMemoryFile.Dispose();
        m_DataMemoryFile.Dispose();
        m_IsDisposed = true;
    }
}

// IIndexReader
partial class IndexReader : IIndexReader
{
    public IndexDocument ReadAll()
    {
        using MemoryMappedViewAccessor accessor = m_DataMemoryFile.CreateViewAccessor();
        Int64 length = m_DataFile.Length;

        List<Byte> bytes = new();
        Int64 offset = 0L;
        while (offset < length)
        {
            bytes.Add(accessor.ReadByte(offset++));
        }

        IndexDocument result = new();
        ReadOnlySpan<Byte> dataBytes = bytes.ToArray();
        while (dataBytes.Length > 0)
        {
            Int32 size = BitConverter.ToInt32(dataBytes[..sizeof(Int32)]);
            ReadOnlySpan<Byte> myBytes = dataBytes[sizeof(Int32)..(size + sizeof(Int32))];
            IndexEntry entry = IndexEntry.FromBytes(myBytes);
            result.Add(entry);
            dataBytes = dataBytes[(size + sizeof(Int32))..];
        }

        return result;
    }

    public IReadOnlyCollection<IndexEntry> ReadByKeyword(String keyword)
    {
        __Address section = this.FindSection(keyword[0]);

        if (section.Start == -1L ||
            section.Length == 0L)
        {
            return Array.Empty<IndexEntry>();
        }

        IReadOnlyCollection<__Address> addressLists = this.GetAddressLists(keyword: keyword,
                                                                           address: section);

        if (addressLists.Count == 0)
        {
            return Array.Empty<IndexEntry>();
        }

        HashSet<IndexEntry> entries = new(__IndexEntryComparer.Instance);

        foreach (__Address listAddress in addressLists)
        {
            IEnumerable<__Address> addresses = this.GetDataAdresses(listAddress);

            foreach (__Address address in addresses)
            {
                entries.Add(this.GetEntry(address));
            }
        }

        List<IndexEntry> result = new(entries);
        result.SortByFileName();

        return result;
    }

    public DirectoryInfo IndexStoreLocation { get; }
}