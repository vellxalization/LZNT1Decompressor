namespace LZNT1Decompressor;

/// <summary>
/// A struct representing a header of a single compression chunk. 
/// </summary>
public readonly struct CompressionChunkHeader
{
    public bool IsCompressed { get; init; }
    public int ChunkSize { get; init; }

    public CompressionChunkHeader(Span<byte> rawChunkHeader)
    {
        var header = (short)(rawChunkHeader[0] | rawChunkHeader[1] << 8);
        var isCompressed = (header & 0x8000) != 0;
        var chunkSize = header & 0xFFF;
        
        ChunkSize = chunkSize + 1;
        IsCompressed = isCompressed;
    }
}