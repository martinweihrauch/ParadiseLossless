using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Output.Model
{
    public class UnchangedFile
    {
        public ushort Value { get; set; }
        public uint Frequency { get; set; }
        public uint PointerToOriginalFile { get; set; }
        public uint KeyOfFileInOutputFileList { get; set; }
        public int RankInTopNList { get; set; }

        public UnchangedFile()
        {
            RankInTopNList = 999999999;
        }
    }
}
