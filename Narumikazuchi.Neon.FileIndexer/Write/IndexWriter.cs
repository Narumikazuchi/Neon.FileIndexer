namespace Narumikazuchi.Neon.FileIndexer;

public sealed partial class IndexWriter
{
    public IndexWriter(String indexStorageLocation) :
        this(new DirectoryInfo(indexStorageLocation))
    { }
    public IndexWriter(DirectoryInfo indexStorageLocation)
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
    }

    public IIndexReader GetReader() =>
        new IndexReader(this.IndexStoreLocation);
}

// Non-Public
partial class IndexWriter
{
    private IndexDocument OpenDocument()
    {
        if (!m_DictionaryFile.Exists ||
            !m_KeywordFile.Exists ||
            !m_AddressFile.Exists ||
            !m_DataFile.Exists)
        {
            return new();
        }
        else
        {
            using IIndexReader reader = new IndexReader(this.IndexStoreLocation);
            return reader.ReadAll();
        }
    }

    private void WriteToDisk(IndexDocument document)
    {
        List<Byte> fileData = new();
        List<Byte> listData = new();
        List<Byte> keywordData = new();
        List<Byte> dictionaryData = new();

        Dictionary<IndexEntry, __Address> fileAddresses = FilesToArray(bytes: fileData,
                                                                       document: document);
        Dictionary<String, __Address> fileListAddresses = FileListToArray(bytes: listData,
                                                                          document: document,
                                                                          fileAddresses: fileAddresses);
        Dictionary<Char, __Address> keywordAddresses = KeywordsToArray(bytes: keywordData,
                                                                       document: document,
                                                                       fileListAddresses: fileListAddresses);
        DictionaryToArray(bytes: dictionaryData,
                          keywordAddresses: keywordAddresses);

        WriteToDisk(file: m_DataFile,
                    bytes: fileData);
        WriteToDisk(file: m_AddressFile,
                    bytes: listData);
        WriteToDisk(file: m_KeywordFile,
                    bytes: keywordData);
        WriteToDisk(file: m_DictionaryFile,
                    bytes: dictionaryData);
    }

    private static Dictionary<IndexEntry, __Address> FilesToArray(List<Byte> bytes,
                                                                  IndexDocument document)
    {
        Dictionary<IndexEntry, __Address> addresses = new();

        Int64 address = 0L;
        foreach (IndexEntry entry in document)
        {
            Byte[] data = entry.ToByteArray();
            data = BitConverter.GetBytes(data.Length)
                               .Concat(data)
                               .ToArray();

            bytes.AddRange(data);

            addresses.Add(key: entry,
                          value: new() { Start = address, Length = data.LongLength });
            address += data.LongLength;
        }

        return addresses;
    }

    private static Dictionary<String, __Address> FileListToArray(List<Byte> bytes,
                                                                 IndexDocument document,
                                                                 Dictionary<IndexEntry, __Address> fileAddresses)
    {
        Dictionary<String, __Address> addresses = new();

        Int64 address = 0L;
        foreach (String keyword in document.m_KeywordMap
                                           .Keys)
        {
            Byte[] data = document.m_KeywordMap[keyword]
                                  .ToByteArray(fileAddresses);

            bytes.AddRange(data);

            addresses.Add(key: keyword,
                          value: new() { Start = address, Length = data.LongLength });
            address += data.LongLength;
        }

        return addresses;
    }

    private static Dictionary<Char, __Address> KeywordsToArray(List<Byte> bytes,
                                                               IndexDocument document,
                                                               Dictionary<String, __Address> fileListAddresses)
    {
        Dictionary<Char, __Address> addresses = new();

        Int64 offset = 0L;
        foreach (String keyword in document.m_KeywordMap
                                           .Keys)
        {
            Int64 start = offset;

            Byte[] data = keyword.ToUTF8ByteArray();
            Byte[] address = fileListAddresses[keyword].ToByteArray();

            data = data.Concat(address)
                       .ToArray();

            offset += data.LongLength;
            bytes.AddRange(data);

            if (!addresses.ContainsKey(keyword[0]))
            {
                addresses.Add(key: keyword[0],
                              value: new() { Start = start, Length = data.LongLength });
                continue;
            }
            else
            {
                __Address extend = new()
                {
                    Start = addresses[keyword[0]].Start,
                    Length = addresses[keyword[0]].Length + data.LongLength
                };
                addresses[keyword[0]] = extend;
                continue;
            }
        }

        return addresses;
    }

    private static void DictionaryToArray(List<Byte> bytes,
                                          Dictionary<Char, __Address> keywordAddresses)
    {
        Int64 offset = 0L;
        foreach (Char letter in keywordAddresses.Keys)
        {
            Byte count = (Byte)Encoding.UTF8.GetByteCount(letter.ToString());
            Byte[] firstLetter = new Byte[] { count }.Concat(Encoding.UTF8.GetBytes(letter.ToString()))
                                                     .ToArray();
            Byte[] data = firstLetter.Concat(keywordAddresses[letter].ToByteArray())
                                     .ToArray();

            offset += data.LongLength;
            bytes.AddRange(data);
        }
    }

    private static void WriteToDisk(FileInfo file,
                                    IReadOnlyCollection<Byte> bytes)
    {
        Int64 size = bytes.Count;
        using FileStream stream = new(path: file.FullName,
                                      mode: FileMode.Create);
        using MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileStream: stream,
                                                                     mapName: file.Name,
                                                                     capacity: size,
                                                                     access: MemoryMappedFileAccess.ReadWrite,
                                                                     inheritability: HandleInheritability.None,
                                                                     leaveOpen: false);
        using MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();

        Int64 offset = 0;
        foreach (Byte b in bytes)
        {
            accessor.Write(position: offset++,
                           value: b);
        }
    }

    private readonly FileInfo m_DictionaryFile;
    private readonly FileInfo m_KeywordFile;
    private readonly FileInfo m_AddressFile;
    private readonly FileInfo m_DataFile;
    private IndexDocument? m_Document;
}

// IIndexWriter
partial class IndexWriter : IIndexWriter
{
    public void Exclude(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (this.IndexStoreLocation.FullName == file.DirectoryName)
        {
            return;
        }

        if (m_Document is null)
        {
            m_Document = this.OpenDocument();
        }

        m_Document.Remove(file);
    }
    public void Exclude(IEnumerable<FileInfo> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (m_Document is null)
        {
            m_Document = this.OpenDocument();
        }

        foreach (FileInfo file in files)
        {
            if (this.IndexStoreLocation.FullName == file.DirectoryName)
            {
                continue;
            }
            m_Document.Remove(file);
        }
    }

    public void Include(IEnumerable<FileInfo> files,
                        IEnumerable<String> tags)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(tags);

        if (m_Document is null)
        {
            m_Document = this.OpenDocument();
        }

        foreach (FileInfo file in files)
        {
            if (this.IndexStoreLocation.FullName == file.DirectoryName)
            {
                continue;
            }
            m_Document.Add(file: file,
                           tags: tags);
        }
    }
    public void Include(FileInfo file,
                        IEnumerable<String> tags)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(tags);

        if (this.IndexStoreLocation.FullName == file.DirectoryName)
        {
            return;
        }

        if (m_Document is null)
        {
            m_Document = this.OpenDocument();
        }

        m_Document.Add(file: file,
                       tags: tags);
    }

    public void Write()
    {
        if (m_Document is null)
        {
            return;
        }

        this.WriteToDisk(m_Document);
        m_Document = null;
    }

    public DirectoryInfo IndexStoreLocation { get; }
}