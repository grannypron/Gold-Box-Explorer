using System;
using System.Collections.Generic;
using System.IO;

namespace GoldBoxExplorer.Lib.Plugins.Dax
{
    public abstract class DaxFile : GoldBoxFile
    {
        List<DaxFileBlock> _blocks;
        MemoryStream _memFile;

        protected DaxFile(string path, bool autoLoad = true)
        {
            _memFile = new MemoryStream();
            if (autoLoad)
                load(path);
        }

        public string FileName { get; private set; }

        public DaxFileBlock getBlockById(int id)
        {
            foreach (var b in _blocks)
            {
                if (b.Id == id)
                    return b;
            }
            return null;
        }
        public IEnumerable<DaxFileBlock> Blocks
        {
            get { return _blocks.AsReadOnly(); }
        }

        public override IList<byte> GetBytes()
        {
            var bytes = new List<byte>();
            foreach (var block in Blocks)
            {
                bytes.AddRange(block.Data);
            }
            return bytes;
        }

        protected abstract void ProcessBlocks();

        public void load(string file)
        {
            FileName = file;

            using (var stream = new System.IO.FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var reader = new BinaryReader(stream);
                var dataOffset = reader.ReadInt16() + 2;
                var headers = new List<DaxFileHeaderEntry>();
                const int headerEntrySize = 9;

                for (var i = 0; i < ((dataOffset - 2) / headerEntrySize); i++)
                {
                    var dhe = new DaxFileHeaderEntry
                                  {
                                      Id = reader.ReadByte(),
                                      Offset = reader.ReadInt32(),
                                      RawSize = reader.ReadUInt16(),
                                      CompressedSize = reader.ReadUInt16()
                                  };
                    headers.Add(dhe);
                }

                _blocks = new List<DaxFileBlock>(headers.Count);

                foreach (var dhe in headers)
                {
                    byte[] compressedBytes;
                    if (dhe.RawSize <= 0)
                    {
                        reader.BaseStream.Seek(dataOffset + dhe.Offset, SeekOrigin.Begin);
                        compressedBytes = reader.ReadBytes(dhe.CompressedSize);
                        _blocks.Add(new DaxFileBlock(FileName, dhe.Id, compressedBytes));
                    }
                    else
                    {
                        var raw = new byte[dhe.RawSize];
                        reader.BaseStream.Seek(dataOffset + dhe.Offset, SeekOrigin.Begin);
                        compressedBytes = reader.ReadBytes(dhe.CompressedSize);
                        if (compressedBytes.Length == 0)
                            continue;
                        decodeCompressedBytes(dhe.CompressedSize, raw, compressedBytes);
                        _blocks.Add(new DaxFileBlock(FileName, dhe.Id, raw));
                    }
                }
            }
        }

        private static void decodeCompressedBytes(int dataLength, IList<byte> outputPtr, IList<byte> inputPtr)
        {
            var inputIndex = 0;
            var outputIndex = 0;

                do
                {
                    var runLength = (sbyte)inputPtr[inputIndex];

                    if (runLength >= 0)
                    {
                        for (var i = 0; i <= runLength; i++)
                        {

                            if (inputIndex + i + 1 >= inputPtr.Count) return; // some files seem to have faulty encoding and run over the limit
                            outputPtr[outputIndex + i] = inputPtr[inputIndex + i + 1];
                        }

                        inputIndex += runLength + 2;
                        outputIndex += runLength + 1;
                    }
                    else
                    {
                        runLength = (sbyte)(-runLength);

                        for (var i = 0; i < runLength; i++)
                        {
                            outputPtr[outputIndex + i] = inputPtr[inputIndex + 1];
                        }

                        inputIndex += 2;
                        outputIndex += runLength;
                    }
                } while (inputIndex < dataLength);

        }

        public void save()
        {
/*            MemoryStream mem = new MemoryStream();
            using (FileStream fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                int data_offset = 0;
                byte[] data = new byte[] { 36, 00 };
                fs.Write(data, offset, data.Length);
                offset += data.Length;


                - for each data block in a DAX file:
                  -loop until whole block is encoded
                    - get count of sequential identical bytes(I've set the max count to 127)
                    - get count of sequential bytes where the next byte differs from the previous one(I've set the max count to 126)
                    - depending on which count is higher, store the control byte + byte to be copied / array of bytes depending on the case

                    -increase offset by count
                  - update the header for this data block with raw size, stored size and data start offset
                        
                fs.Write(data, 0, data.Length);
            }
            System.Windows.Forms.MessageBox.Show("Save complete.");   */
        }
    }
}