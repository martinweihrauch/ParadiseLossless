using PlCompressor.Output.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Statistics
{
    public class Stats
    {
        public ulong Bits { get; set; }
        public uint CountParameterCommands { get; set; }
        public uint CountParameterLengths { get; set; }
        public uint MaxLengthParameter { get; set; }
        public Effectiveness[] CommandEffectiveness { get; set; }
        public Stats()
        {
            Bits = 0;
            MaxLengthParameter = 0;
            CommandEffectiveness = new Effectiveness[16];
            for(var i= 0; i < CommandEffectiveness.Length; i++)
            {
                CommandEffectiveness[i] = new Effectiveness();
            }
        }


        public void CalculateCommandStats(List<FileEntry> entries)
        {
            foreach(FileEntry entry in entries)
            {
                CommandEffectiveness[entry.Command].Frequency++;
                int bitsSaved = 0;
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
                        bitsSaved = 16 - 4;
                        break;

                    case (byte)Command.CloneNorthRepeat:
                    case (byte)Command.CloneWestRepeat:
                        bitsSaved = entry.Parameter * 16 - 1 - entry.ParameterLengthInBit ;
                        break;
                    case (byte)Command.DeltaWestOnce4Bit:
                        bitsSaved = 16 - 4 - 4;
                        break;

                    case (byte)Command.DeltaWestOnce8Bit:
                        bitsSaved = 16 - 4 - 8;
                        break;

                    case (byte)Command.DeltaWestRepeat4Bit:
                        bitsSaved = entry.Parameter * (16 - 4)  - 1 - entry.ParameterLengthInBit;
                        break;

                    case (byte)Command.DeltaWestRepeat8Bit:
                        bitsSaved = entry.Parameter * (16 - 8) - 1 - entry.ParameterLengthInBit;
                        break;

                    case (byte)Command.DeltaNorthOnce4Bit:
                        bitsSaved = 16 - 4 - 4;
                        break;

                    case (byte)Command.DeltaNorthOnce8Bit:
                         bitsSaved = 16 - 4 - 8;

                        break;

                    case (byte)Command.DeltaNorthRepeat4Bit:
                        bitsSaved = entry.Parameter * (16 - 4) - 1 - entry.ParameterLengthInBit;
                        break;

                    case (byte)Command.DeltaNorthRepeat8Bit:
                        bitsSaved = entry.Parameter * (16 - 8) - 1 - entry.ParameterLengthInBit;
                        break;


                    case (byte)Command.Lookup4Bit:
                        bitsSaved = 16 - 4 - 4;
                        break;

                    case (byte)Command.Lookup8Bit:
                        bitsSaved = 16 - 8 - 4;
                        break;

                    case (byte)Command.Lookup12Bit:
                        bitsSaved = 16 - 4 - 12;
                        break;



                    case (byte)Command.Unchanged:
                        bitsSaved = 16 - 16 - 4;
                        break;
                }
                CommandEffectiveness[entry.Command].BitsSaved += bitsSaved;
            }
        }
    }
}
