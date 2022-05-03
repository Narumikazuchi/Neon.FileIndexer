
namespace Narumikazuchi.Neon.FileIndexer;

public interface IIndexReader : 
    IDisposable
{
    public IndexDocument ReadAll();

    public IReadOnlyCollection<IndexEntry> ReadByKeyword(String keyword);

    public DirectoryInfo IndexStoreLocation { get; }
}