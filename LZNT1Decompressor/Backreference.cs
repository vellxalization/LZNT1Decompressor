namespace LZNT1Decompressor;

public readonly struct Backreference
{
    public int Offset { get; init; }
    public int Size { get; init; }
    
    public Backreference(Span<byte> rawBackreference, int positionInsideBlock)
    {
        const int offsetConst = 1; // offset in compressed tuple is stored as "true offset - 1" since offset of 0 is impossible
        const int sizeConst = 3; // size in compressed tuple is stored as "true size - 3" since backreference size is 2 bytes, hence there is no point in compressing 1- or 2-byte long substring
        var backreference = (short)(rawBackreference[0] | rawBackreference[1] << 8);
        var info = GetBackreferenceInfo(positionInsideBlock);
        var size = ((backreference & info.LengthMask)) + sizeConst;
        var offset = ((backreference & info.OffsetMask) >> info.OffsetShift) + offsetConst;

        Offset = offset;
        Size = size;
    }
    
    private static (ushort OffsetMask, ushort LengthMask, int OffsetShift) GetBackreferenceInfo(int indexInDecompressedBlock)
    {
        if (indexInDecompressedBlock is < 0 or > 4096)
            throw new ArgumentException($"Position must be between 0 and 4096, got: {indexInDecompressedBlock}");
        
        const ushort fullBitMask = 0b_11111111_11111111;
        unchecked
        {
            return (indexInDecompressedBlock - 1) switch
            {
                < 0b10000 => ((ushort)(fullBitMask << 12), fullBitMask >> 4, 12),
                < 0b100000 => ((ushort)(fullBitMask << 11), fullBitMask >> 5, 11),
                < 0b1000000 => ((ushort)(fullBitMask << 10), fullBitMask >> 6, 10),
                < 0b10000000 => ((ushort)(fullBitMask << 9), fullBitMask >> 7, 9),
                < 0b100000000 => ((ushort)(fullBitMask << 8), fullBitMask >> 8, 8),
                < 0b1000000000 => ((ushort)(fullBitMask << 7), fullBitMask >> 9, 7),
                < 0b10000000000 => ((ushort)(fullBitMask << 6), fullBitMask >> 10, 6),
                < 0b100000000000 => ((ushort)(fullBitMask << 5), fullBitMask >> 11, 5),
                _ => ((ushort)(fullBitMask << 4), fullBitMask >> 12, 4)
            };
        }
    }
}