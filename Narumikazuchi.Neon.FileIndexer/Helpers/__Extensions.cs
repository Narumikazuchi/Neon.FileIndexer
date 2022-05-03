namespace Narumikazuchi.Neon.FileIndexer;

internal static class __Extensions
{
    internal static String[] SplitNormalised(this String source)
    {
        Char[] separators = new Char[] { ' ', '.', ',', ';', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_' };
        return source.Split(separator: separators,
                            options: StringSplitOptions.RemoveEmptyEntries);
    }

    internal static Byte[] ToUTF8ByteArray(this String source)
    {
        List<Byte> result = new();

        Byte[] raw = Encoding.UTF8.GetBytes(source);
        Byte[] count = BitConverter.GetBytes(raw.Length);
        foreach (Byte b in count.Concat(raw))
        {
            result.Add(b);
        }

        return result.ToArray();
    }

    internal static String ToUTF8String(this ReadOnlySpan<Byte> source)
    {
        Int32 length = BitConverter.ToInt32(source);
        ReadOnlySpan<Byte> slice = source.Slice(4, length);
        String result = Encoding.UTF8.GetString(slice);
        return result;
    }

    internal static Byte[] ToByteArray(this ICollection<IndexEntry> source,
                                       IReadOnlyDictionary<IndexEntry, __Address> addresses)
    {
        List<Byte> result = new();

        Byte[] count = BitConverter.GetBytes(source.Count);
        result.AddRange(count);
        foreach (IndexEntry item in source)
        {
            if (!addresses.ContainsKey(item))
            {
                continue;
            }
            Byte[] address = addresses[item].ToByteArray();
            result.AddRange(address);
        }

        return result.ToArray();
    }

    internal static void SortByFileName(this List<IndexEntry> source) =>
        source.Sort(IndexEntryComparison);

    private static Int32 IndexEntryComparison(IndexEntry left,
                                              IndexEntry right) =>
        left.File.Name
            .CompareTo(right.File.Name);
}