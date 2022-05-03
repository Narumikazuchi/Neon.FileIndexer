namespace Narumikazuchi.Neon.FileIndexer;

[DebuggerDisplay("{Start} - {Start + Length} ({Length})")]
internal readonly struct __Address
{
    public readonly Byte[] ToByteArray() =>
        BitConverter.GetBytes(this.Start)
                    .Concat(BitConverter.GetBytes(this.Length))
                    .ToArray();

    public Int64 Start
    {
        get;
        init;
    }

    public Int64 Length
    {
        get;
        init;
    }
}