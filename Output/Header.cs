using SharpBitStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Helpers.Model
{
    internal class Header
    {
        private ushort MagicWord {get; set;}
        private ushort Version {get; set;}
        public ushort FileType {get; set;}
        public uint SetOfCommands {get; set;}
        public byte HeaderSize { get; set; }
        public uint UncompressedSize { get; set; }
        public uint ImageWidth { get; set; }
        public uint ImageHeight { get; set; }
        public ushort LookupTableSize {get; set;}
        public uint CommandsSize {get; set;}
        public uint NumberOfCommands {get; set;}
        public uint ParametersSize {get; set;}
        public byte ShortParameterLengthInBits {get; set;}
        public byte LongParameterLengthInBits {get; set;}
        public uint DataSize {get; set;}

        public void WriteHeader(Stream stream)
        {
            MagicWord = (ushort)((77 << 8) + 119);
            Version = (ushort)((1 << 8) + 0);
            FileType = 1;
            HeaderSize =  2 + 2 + 2 + 1 + 1 + 4 + 4 + 4 + 2 + 4 + 4 + 4 +4;
            var bs = new BitStream(stream);
            bs.WriteUnsigned(16, MagicWord);
            bs.WriteUnsigned(16, Version);
            bs.WriteUnsigned(16, FileType);
            bs.WriteUnsigned(8, SetOfCommands);
            bs.WriteUnsigned(8, HeaderSize);
            bs.WriteUnsigned(32, UncompressedSize);
            bs.WriteUnsigned(32, ImageWidth);
            bs.WriteUnsigned(32, ImageHeight);
            bs.WriteUnsigned(16, LookupTableSize);
            bs.WriteUnsigned(32, CommandsSize);
            bs.WriteUnsigned(32, NumberOfCommands);
            bs.WriteUnsigned(32, ParametersSize);
            bs.WriteUnsigned(8, ShortParameterLengthInBits);
            bs.WriteUnsigned(8, LongParameterLengthInBits);
            bs.WriteUnsigned(32, DataSize);
        }

        public void GetHeader(Stream stream)
        {
            var bs = new BitStream(stream);
            MagicWord = (ushort)bs.ReadUnsigned(16);
            Version = (ushort)bs.ReadUnsigned(16);
            FileType = (ushort)bs.ReadUnsigned(16); 
            SetOfCommands = (ushort)bs.ReadUnsigned(8);
            HeaderSize = (byte)bs.ReadUnsigned(8);
            UncompressedSize = (uint)bs.ReadUnsigned(32);
            ImageWidth = (uint)bs.ReadUnsigned(32);
            ImageHeight = (uint)bs.ReadUnsigned(32);
            LookupTableSize = (ushort)bs.ReadUnsigned(16);
            CommandsSize = (uint)bs.ReadUnsigned(32);
            NumberOfCommands = (uint)bs.ReadUnsigned(32);
            ParametersSize = (uint)bs.ReadUnsigned(32);
            ShortParameterLengthInBits = (byte)bs.ReadUnsigned(8);
            LongParameterLengthInBits = (byte)bs.ReadUnsigned(8);
            DataSize = (uint)bs.ReadUnsigned(32);
        }

        public bool CheckMagicWord()
        {
            if(MagicWord == (ushort)((77 << 8) + 119))
            {
                return true;
            }
            return false;
        }

    }
}
