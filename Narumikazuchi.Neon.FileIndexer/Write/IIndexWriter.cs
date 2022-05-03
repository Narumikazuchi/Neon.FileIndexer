
namespace Narumikazuchi.Neon.FileIndexer;

public interface IIndexWriter
{
    public IIndexReader GetReader();

    public void Exclude(DirectoryInfo directory) =>
        this.Exclude(directory: directory,
                     recursive: false);
    public void Exclude(DirectoryInfo directory,
                        in Boolean recursive)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (recursive)
        {
            this.Exclude(files: GetFilesRecursive(directory));
            return;
        }
        else
        {
            this.Exclude(files: directory.EnumerateFiles());
            return;
        }
    }
    public void Exclude(FileInfo file);
    public void Exclude(IEnumerable<FileInfo> files);

    public void Include(DirectoryInfo directory) =>
        this.Include(directory: directory,
                   tags: Array.Empty<String>(),
                   recursive: false);
    public void Include(DirectoryInfo directory,
                        IEnumerable<String> tags) =>
        this.Include(directory: directory,
                   tags: tags,
                   recursive: false);
    public void Include(DirectoryInfo directory,
                        IEnumerable<String> tags,
                        in Boolean recursive)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (recursive)
        {
            this.Include(files: GetFilesRecursive(directory),
                       tags: tags);
            return;
        }
        else
        {
            this.Include(files: directory.EnumerateFiles(),
                       tags: tags);
            return;
        }
    }
    public void Include(DirectoryInfo directory,
                        in Boolean recursive) =>
        this.Include(directory: directory,
                   tags: Array.Empty<String>(),
                   recursive: recursive);
    public void Include(FileInfo file) =>
        this.Include(file: file,
                   tags: Array.Empty<String>());
    public void Include(FileInfo file,
                        IEnumerable<String> tags);
    public void Include(IEnumerable<FileInfo> files) =>
        this.Include(files: files,
                   tags: Array.Empty<String>());
    public void Include(IEnumerable<FileInfo> files,
                        IEnumerable<String> tags);

    public void Write();

    public DirectoryInfo IndexStoreLocation { get; }

    private static IEnumerable<FileInfo> GetFilesRecursive(DirectoryInfo directory) =>
        directory.EnumerateFiles()
                 .Concat(directory.EnumerateDirectories()
                                  .SelectMany(x => GetFilesRecursive(x)));
}