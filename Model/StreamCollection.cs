using PlCompressor.Helpers.Model;
using SharpBitStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Model
{
    internal class StreamCollection
    {
        public MemoryStream LookupTable { get; set; }
        public byte[] LookupTableBytes { get; set; }
        public MemoryStream Command { get; set; }
        public MemoryStream Parameter { get; set; }
        public MemoryStream Data { get; set; }
        public BitStream BsLookupTable { get; set; }
        public BitStream BsCommand { get; set; }
        public BitStream BsParameter { get; set; }
        public BitStream BsData { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public StreamCollection()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            LookupTable = new MemoryStream();
            Command = new MemoryStream();
            Parameter = new MemoryStream();
            Data = new MemoryStream();
            BsLookupTable = new BitStream(LookupTable);
            BsCommand = new BitStream(Command);
            BsParameter = new BitStream(Parameter);
            BsData = new BitStream(Data);
        }

        public void BeKindRewind()
        {
            LookupTable.Position = 0;
            Command.Position = 0;
            Parameter.Position = 0;
            Data.Position = 0;
            BsLookupTable.SetPosition(0, 0);
            BsCommand.SetPosition(0, 0);
            BsParameter.SetPosition(0, 0);
            BsData.SetPosition(0, 0);
        }

        public void SplitStreamsIntoCollection(Stream stream, Header header)
        {
            stream.Position = header.HeaderSize; 
            CopyStream(stream, LookupTable, header.LookupTableSize);
            LookupTableBytes = LookupTable.ToArray();
            CopyStream(stream, Command, (int)header.CommandsSize);
            CopyStream(stream, Parameter, (int)header.ParametersSize);
            CopyStream(stream, Data, (int)header.DataSize);
            BeKindRewind();
        }

        public ushort GetUshortFromByteLookUpTableArray(uint index)
        {
            // LITTLE ENDIAN!!
            return (ushort)(LookupTableBytes[2 * index] + (LookupTableBytes[2 * index + 1] << 8));
        }

        private static void CopyStream(Stream input, Stream output, int bytes)
        {
            byte[] buffer = new byte[32768];
            int read;
            while (bytes > 0 &&
                   (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }
    }
}
