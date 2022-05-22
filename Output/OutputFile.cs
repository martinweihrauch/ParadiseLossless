using PlCompressor.Helpers.Model;
using PlCompressor.ImageManagement;
using PlCompressor.LookupTable;
using PlCompressor.Model;
using PlCompressor.Output.Model;
using PlCompressor.Statistics;

namespace PlCompressor.Output
{
    public class OutputFile
    {
        public List<FileEntry> Entries { get; set; }
        public List<int> IndexToEntriesWithUnchanged { get; set; }
        private StreamCollection _sc;
        private Dictionary<ushort, UnchangedFile> _unchangedInformation;
        private Header _header;
        private List<UnchangedFile> _top2048;
        private Stats _stats { get; set; }
        private const int _bitLengthCommand = 4;
        private const int _bitLengthUnchanged = 16;
        private Switches _switches;
        private ImageInfo _imageInfo;


        public OutputFile(Dictionary<ushort, UnchangedFile> unchangedInformation, Stats stats, Switches switches)
        {
            Entries = new List<FileEntry>();
            IndexToEntriesWithUnchanged = new List<int>();
            _sc = new StreamCollection();
            _unchangedInformation = unchangedInformation;
            _top2048 = new List<UnchangedFile>();
            _stats = stats;
            _switches = switches;
        }

        public void SetImageInfo(ImageInfo imageInfo)
        {
            _imageInfo = imageInfo;
        }

        public void AddEntry(FileEntry entry)
        {
            Entries.Add(entry);
            if (entry.Unchanged)
            {
                IndexToEntriesWithUnchanged.Add(Entries.Count - 1);
            }
        }

        public void FillCommandsOfUnchanged(Dictionary<ushort, UnchangedFile> unchanged)
        {
            /*
                Lookup4Bit, 
                Lookup8Bit, 
                Lookup12Bit,
                Unchanged,
            */
            foreach (int index in IndexToEntriesWithUnchanged)
            {
                var unchangedFile = unchanged[Entries[index].Data];
                Entries[index].Command = unchangedFile.RankInTopNList switch
                {
                    >= 0 and < 16 => (byte)Command.Lookup4Bit,
                    >= 16 and < 272 => (byte)Command.Lookup8Bit,
                    >= 272 and < 2320 => (byte)Command.Lookup12Bit,
                    _ => (byte)Command.Unchanged
                };
            }
        }

        public void CreateLookupTable(Switches switches)
        {
            if (switches.Dictionary)
            {
                _top2048 = GetTopKFromN.TopNSorted(_unchangedInformation, 2048);
            }
            DictionaryHandler.AddTopListPositionToDictionary(_unchangedInformation, _top2048);
        }


        public void BuildOutputFile(Stream outputStream, uint uncompressedSize)
        {
            int tempCounter = 0;
            foreach (FileEntry entry in Entries)
            {
                tempCounter++;
                _sc.BsCommand.WriteUnsigned(4, entry.Command);
                switch (entry.Command)
                {
                    /*
                     *         
                        CloneWestOnce, 
                        CloneWestRepeat, 
                        CloneNorthOnce,
                        CloneNorthRepeat, 
                        DeltaWestOnce4Bit,
                        DeltaWestRepeat4Bit,
                        DeltaWestOnce8Bit, 
                        DeltaWestRepeat8Bit,
                        DeltaNorthOnce4Bit,
                        DeltaNorthRepeat4Bit,
                        DeltaNorthOnce8Bit,
                        DeltaNorthRepeat8Bit,
                        Lookup4Bit, 
                        Lookup8Bit, 
                        Lookup12Bit,
                        Unchanged
                     */

                    
                    case (byte)Command.CloneWestOnce:
                    case (byte)Command.CloneNorthOnce:
                        _stats.Bits += _bitLengthCommand;
                        break;
                    
                    case (byte)Command.CloneNorthRepeat:
                    case (byte)Command.CloneWestRepeat:
                        _sc.BsParameter.WriteUnsigned(1, entry.ParameterLength3BitElse8Bit);
                        _sc.BsParameter.WriteUnsigned(entry.ParameterLengthInBit, entry.Parameter);
                        _stats.Bits += (ulong)(_switches.LongShortSwitchLengthInBits + _bitLengthCommand + entry.ParameterLengthInBit);
                        break;
                    
                    case (byte)Command.DeltaWestOnce4Bit:
                        _sc.BsData.WriteSigned(4, entry.Deltas[0]);
                        _stats.Bits += _bitLengthCommand + 4;
                        break;
                    
                    case (byte)Command.DeltaWestOnce8Bit:
                        _sc.BsData.WriteSigned(8, entry.Deltas[0]);
                        _stats.Bits += _bitLengthCommand + 8;
                        break;
                    
                    case (byte)Command.DeltaWestRepeat4Bit:
                        _sc.BsParameter.WriteUnsigned(1, entry.ParameterLength3BitElse8Bit);
                        _sc.BsParameter.WriteUnsigned(entry.ParameterLengthInBit, entry.Parameter);
                        for (int i = entry.DeltaOffset; i < entry.DeltaOffset + entry.Parameter; i++)
                        {
                            _sc.BsData.WriteSigned(4, entry.Deltas[i]);
                        }
                        _stats.Bits += (ulong)(_switches.LongShortSwitchLengthInBits + _bitLengthCommand + entry.ParameterLengthInBit + entry.Parameter * 4);
                        break;
                    
                    case (byte)Command.DeltaWestRepeat8Bit:
                        _sc.BsParameter.WriteUnsigned(1, entry.ParameterLength3BitElse8Bit);
                        _sc.BsParameter.WriteUnsigned(entry.ParameterLengthInBit, entry.Parameter);
                        for (int i = entry.DeltaOffset; i < entry.DeltaOffset + entry.Parameter; i++)
                        {
                            _sc.BsData.WriteSigned(8, entry.Deltas[i]);
                        }
                        _stats.Bits += (ulong)(_switches.LongShortSwitchLengthInBits + _bitLengthCommand + entry.ParameterLengthInBit + entry.Parameter * 8);
                        break;

                    case (byte)Command.DeltaNorthOnce4Bit:
                        _sc.BsData.WriteSigned(4, entry.Deltas[0]);
                        _stats.Bits += _bitLengthCommand + 4;
                        break;
                    
                    case (byte)Command.DeltaNorthOnce8Bit:
                        _sc.BsData.WriteSigned(8, entry.Deltas[0]);
                        _stats.Bits += _bitLengthCommand + 8;
                        break;
                    
                    case (byte)Command.DeltaNorthRepeat4Bit:
                        _sc.BsParameter.WriteUnsigned(1, entry.ParameterLength3BitElse8Bit);
                        _sc.BsParameter.WriteUnsigned(entry.ParameterLengthInBit, entry.Parameter);
                        for (int i = entry.DeltaOffset; i < entry.DeltaOffset + entry.Parameter; i++)
                        {
                            _sc.BsData.WriteSigned(4, entry.Deltas[i]);
                        }
                        _stats.Bits += (ulong)(_switches.LongShortSwitchLengthInBits + _bitLengthCommand + entry.ParameterLengthInBit + entry.Parameter * 4);
                        break;
                    
                    case (byte)Command.DeltaNorthRepeat8Bit:
                        _sc.BsParameter.WriteUnsigned(1, entry.ParameterLength3BitElse8Bit);
                        _sc.BsParameter.WriteUnsigned(entry.ParameterLengthInBit, entry.Parameter);
                        for (int i = entry.DeltaOffset; i < entry.DeltaOffset + entry.Parameter; i++)
                        {
                            _sc.BsData.WriteSigned(8, entry.Deltas[i]);
                        }
                        _stats.Bits += (ulong)(_switches.LongShortSwitchLengthInBits + _bitLengthCommand + entry.ParameterLengthInBit + entry.Parameter * 8);
                        break;


                    case (byte)Command.Lookup4Bit:
                        _sc.BsData.WriteUnsigned(4, (ulong)_unchangedInformation[entry.Data].RankInTopNList); 
                        _stats.Bits += (ulong)(_bitLengthCommand + 4);
                    break;
                   
                    case (byte)Command.Lookup8Bit:
                        _sc.BsData.WriteUnsigned(8, (ulong)(_unchangedInformation[entry.Data].RankInTopNList - 16));
                        _stats.Bits += (ulong)(_bitLengthCommand + 8);
                        break;
                    
                    case (byte)Command.Lookup12Bit:
                        _sc.BsData.WriteUnsigned(12, (ulong)(_unchangedInformation[entry.Data].RankInTopNList - 272));
                        _stats.Bits += (ulong)(_bitLengthCommand + 12);
                        break;
                   
                    case (byte)Command.Unchanged:
                        _sc.BsData.WriteUnsigned(16, entry.Data);
                        _stats.Bits += (ulong)(_bitLengthCommand + _bitLengthUnchanged);
                        break;

                    default:
                    throw new ArgumentException("Something went wrong with the switch statement in Outputfile");
                    break;
                }

            }
            _stats.Bits += (ulong)(_top2048.Count * 16);

            _sc.Command.Position = 0;
            _sc.Parameter.Position = 0;
            _sc.Data.Position = 0;
            _sc.LookupTable.Position = 0;

            // Header

            _header = new Header()
            {
                LookupTableSize = (ushort)(_top2048.Count * 2),
                CommandsSize = (uint)_sc.Command.Length,
                NumberOfCommands = (uint)Entries.Count,
                ParametersSize = (uint)_sc.Parameter.Length,
                DataSize = (uint)_sc.Data.Length,
                UncompressedSize = uncompressedSize,
                ImageHeight = (uint)_imageInfo.Height,
                ImageWidth = (uint)_imageInfo.Width,
                ShortParameterLengthInBits = (byte)_switches.ShortParameterLengthInBits,
                LongParameterLengthInBits = (byte)_switches.LongParameterLengthInBits
            };

            _header.WriteHeader(outputStream);
            outputStream.Flush();

            // LookupTable
            WriteUshortsToStream(_top2048, _sc.LookupTable);
            byte[]? buffer = _sc.LookupTable.ToArray();
            outputStream.Write(buffer, 0, buffer.Length);
            outputStream.Flush();

            // Commands
            buffer = _sc.Command.ToArray();
            outputStream.Write(buffer, 0, buffer.Length);
            outputStream.Flush();
            
            // Parameters
            buffer = _sc.Parameter.ToArray();
            outputStream.Write(buffer, 0, buffer.Length);
            outputStream.Flush();

            // Data
            buffer = _sc.Data.ToArray();
            outputStream.Write(buffer, 0, buffer.Length);
            outputStream.Flush();


            var commandLength = _sc.Command.Length;
            var parameterLength = _sc.Parameter.Length;
            var dataLength = _sc.Data.Length;

        }

        private void WriteUshortsToStream(List<UnchangedFile> top2048, Stream stream)
        {
            foreach(UnchangedFile u in top2048)
            {
                stream.WriteByte((byte)(u.Value & 0xFF));
                stream.WriteByte((byte)(u.Value >> 8));
            }
        }
    }

}
