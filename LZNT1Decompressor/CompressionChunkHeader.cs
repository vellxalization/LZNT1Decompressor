namespace LZNT1Decompressor;

public readonly struct CompressionChunkHeader
{
    public bool IsCompressed { get; init; }
    public int ChunkSize { get; init; }

    public CompressionChunkHeader(Span<byte> rawChunkHeader)
    {
        var header = (short)(rawChunkHeader[0] | rawChunkHeader[1] << 8);
        var isCompressed = (header & 0x8000) != 0;
        var chunkSize = header & 0xFFF;
        
        ChunkSize = chunkSize;
        IsCompressed = isCompressed;
    }
}