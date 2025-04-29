namespace LZNT1Decompressor;

public partial class LZNT1Decompressor
{
    protected class DecompressionBuffer
    {
        public DecompressionBuffer(int bufferSize) => Buffer = new byte[bufferSize];
        
        public byte[] Buffer { get; init; }
        public int BlockPointer { get; private set; }
        public int InsertPointer { get; private set; }

        public void InsertByte(byte value)
        {
            if (InsertPointer + 1 > Buffer.Length)
                throw new ArgumentException("The destination buffer is too small.");
            
            Buffer[InsertPointer++] = value;
            BlockPointer += 1;
        }

        public void CloseCurrentBlock()
        {
            BlockPointer = 0;
        }
    
        public void InsertRange(Span<byte> range)
        {
            if (InsertPointer + range.Length > Buffer.Length)
                throw new ArgumentException("The destination buffer is too small.");
            
            range.CopyToAt(Buffer, InsertPointer);
            InsertPointer += range.Length;
            BlockPointer += range.Length;
        }
    
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
    }
}