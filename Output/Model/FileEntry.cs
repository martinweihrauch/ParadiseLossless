using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Output.Model
{
    public class FileEntry
    {
        public byte Command { get; set; }
        public byte Parameter { get; set; }
        public ushort Data { get; set; }
        //public uint Pointer { get; set; }
        public bool Unchanged { get; set; }
        public List<int> Deltas { get; set; }
        public int DeltaOffset { get; set; } // To later split deltas into smaller chunks
        public uint ParameterLength3BitElse8Bit { get; set; } // 1 = 3 Bit, 0 = 8 Bit
        public int ParameterLengthInBit { get; set; }  
        
        public FileEntry()
        {
            Command = 0;
            Parameter = 0;
            ParameterLengthInBit = 0;
        }
    }
}
