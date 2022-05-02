namespace Narumikazuchi.Neon.FileIndexer;

[Singleton]
internal sealed partial class __IndexEntryComparer : IEqualityComparer<IndexEntry>
{
    public Boolean Equals(IndexEntry? x,
                          IndexEntry? y)
    {
        if (x is null)
        {
            return y is null;
        }
        if (y is null)
        {
            return false;
        }
        return String.Equals(a: x.File.FullName,
                             b: y.File.FullName,
                             comparisonType: StringComparison.InvariantCultureIgnoreCase);
    }

    public Int32 GetHashCode([DisallowNull] IndexEntry obj) =>
        obj.File.FullName.GetHashCode();
}