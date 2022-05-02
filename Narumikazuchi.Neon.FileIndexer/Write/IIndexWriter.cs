
namespace Narumikazuchi.Neon.FileIndexer;

public interface IIndexWriter
{
    public IIndexReader GetReader();

    public void Write(DirectoryInfo directory) =>
        this.Write(directory: directory,
                   tags: Array.Empty<String>(),
                   recursive: false);
    public void Write(DirectoryInfo directory,
                      IEnumerable<String> tags) =>
        this.Write(directory: directory,
                   tags: tags,
                   recursive: false);
    public void Write(DirectoryInfo directory,
                      IEnumerable<String> tags,
                      in Boolean recursive)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (recursive)
        {
            this.Write(files: GetFilesRecursive(directory),
                       tags: tags);
            return;
        }
        else
        {
            this.Write(files: directory.EnumerateFiles(),
                       tags: tags);
            return;
        }
    }
    public void Write(DirectoryInfo directory,
                      in Boolean recursive) =>
        this.Write(directory: directory,
                   tags: Array.Empty<String>(),
                   recursive: recursive);
    public void Write(FileInfo file) =>
        this.Write(file: file,
                   tags: Array.Empty<String>());
    public void Write(FileInfo file,
                      IEnumerable<String> tags);
    public void Write(IEnumerable<FileInfo> files) =>
        this.Write(files: files,
                   tags: Array.Empty<String>());
    public void Write(IEnumerable<FileInfo> files,
                      IEnumerable<String> tags);

    public DirectoryInfo IndexStoreLocation { get; }

    private static IEnumerable<FileInfo> GetFilesRecursive(DirectoryInfo directory) =>
        directory.EnumerateFiles()
                 .Concat(directory.EnumerateDirectories()
                                  .SelectMany(x => GetFilesRecursive(x)));
}