namespace LZNT1Decompressor;

public partial class LZNT1Decompressor
{
    /// <summary>
    /// A decompression buffer
    /// </summary>
    protected class DecompressionBuffer
    {
        public DecompressionBuffer(int bufferSize) => Buffer = new byte[bufferSize];
        
        public byte[] Buffer { get; init; }
        /// <summary>
        /// Index of the current byte in block. Set to zero when CloseCurrentBlock() is called.
        /// </summary>
        public int BlockPointer { get; private set; }
        /// <summary>
        /// Index of the current byte in the buffer.
        /// </summary>
        public int InsertPointer { get; private set; }

        /// <summary>
        /// Insert a byte at current InsertPointer position
        /// </summary>
        public void InsertByte(byte value)
        {
            if (InsertPointer + 1 > Buffer.Length)
                throw new ArgumentException("The destination buffer is too small.");
            
            Buffer[InsertPointer++] = value;
            BlockPointer += 1;
        }
        
        /// <summary>
        /// Insert bytes at current InsertPointer position
        /// </summary>
        public void InsertRange(Span<byte> range)
        {
            if (InsertPointer + range.Length > Buffer.Length)
                throw new ArgumentException("The destination buffer is too small.");
            
            range.CopyToAt(Buffer, InsertPointer);
            InsertPointer += range.Length;
            BlockPointer += range.Length;
        }
    
        /// <summary>
        /// Copy previously inserted bytes using a backreference
        /// </summary>
        public void InsertFromBackreference(Backreference backreference)
        {
            var span = Buffer.AsSpan();
            if (backreference.Size <= backreference.Offset)
            {
                var data = span.Slice(InsertPointer - backreference.Offset, backreference.Size);
                data.CopyToAt(span, InsertPointer);
                InsertPointer += data.Length;
            }
            else
            {
                for (int i = 0; i < backreference.Size; ++i)
                {
                    span[InsertPointer] = span[InsertPointer - backreference.Offset];
                    ++InsertPointer;
                }
            }
            BlockPointer += backreference.Size;
        }
        
        /// <summary>
        /// Close current chunk. Should be called whenever a compression chunk is fully decompressed
        /// </summary>
        public void CloseCurrentChunk()
        {
            BlockPointer = 0;
        }
    }
}