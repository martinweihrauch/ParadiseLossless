using PlCompressor.Helpers.Model;
using PlCompressor.Model;
using PlCompressor.Output.Model;
using SharpBitStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor
{
    public static class Decompressor
    {
        public static void Decompress(Stream inputStream, Stream outputStream)
        {
            var bsIn = new BitStream(inputStream);
            var sc = new StreamCollection();
            ushort[] output;
            uint outputPointer = 0;

            var header = new Header();
            header.GetHeader(inputStream);
            if (!header.CheckMagicWord())
            {
                throw new Exception("This is not a file compressed with Paradise Lossless!");
            }

            output = new ushort[header.UncompressedSize / 2];
            sc.SplitStreamsIntoCollection(inputStream, header);

            for (var i = 0; i < header.NumberOfCommands; i++)
            {
                var command = (uint)sc.BsCommand.ReadUnsigned(4);
                uint bitLength3 = 0;
                int commandRepetitions = 0;
                ushort tempVal = 0;
                uint index = 0;

                switch (command)
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



                    case (uint)Command.CloneWestOnce:
                        output[outputPointer] = output[outputPointer - 1];
                        outputPointer++;
                        break;

                    case (uint)Command.CloneWestRepeat:
                        bitLength3 = (uint)sc.BsParameter.ReadUnsigned(1);
                        commandRepetitions = bitLength3 == 1 ? (int)sc.BsParameter.ReadUnsigned(header.ShortParameterLengthInBits) + 3
                                                             : (int)sc.BsParameter.ReadUnsigned(header.LongParameterLengthInBits) + 1;
                        tempVal = output[outputPointer - 1];
                        for (int rep = 0; rep < commandRepetitions; rep++)
                        {
                            output[outputPointer++] = tempVal;
                        }
                        break;

                    case (uint)Command.CloneNorthOnce:
                        output[outputPointer] = output[outputPointer - header.ImageWidth];
                        outputPointer++;
                        break;

                    case (uint)Command.CloneNorthRepeat:
                        bitLength3 = (uint)sc.BsParameter.ReadUnsigned(1);
                        commandRepetitions = bitLength3 == 1 ? (int)sc.BsParameter.ReadUnsigned(header.ShortParameterLengthInBits) + 3
                                                             : (int)sc.BsParameter.ReadUnsigned(header.LongParameterLengthInBits) + 1;
                        tempVal = output[outputPointer - header.ImageWidth];
                        for (int rep = 0; rep < commandRepetitions; rep++)
                        {
                            output[outputPointer++] = tempVal;
                        }
                        break;  

                    case (uint)Command.DeltaWestOnce4Bit:
                        var deltaTemp = sc.BsData.ReadSigned(4);
                        output[outputPointer] = (ushort)((output[outputPointer - 1] + deltaTemp));
                        outputPointer++;
                        break;

                    case (uint)Command.DeltaWestOnce8Bit:
                        output[outputPointer] = (ushort)((output[outputPointer - 1] + sc.BsData.ReadSigned(8)));
                        outputPointer++;
                        break;

                    case (uint)Command.DeltaWestRepeat4Bit:
                        bitLength3 = (uint)sc.BsParameter.ReadUnsigned(1);
                        commandRepetitions = bitLength3 == 1 ? (int)sc.BsParameter.ReadUnsigned(header.ShortParameterLengthInBits) + 3
                                                             : (int)sc.BsParameter.ReadUnsigned(header.LongParameterLengthInBits) + 1;
                        for (int rep = 0; rep < commandRepetitions; rep++)
                        {
                            int delta = (int)sc.BsData.ReadSigned(4);
                            output[outputPointer] = (ushort)(output[outputPointer - 1] + delta);
                            outputPointer++;
                        }
                        break;
                        
                    case (uint)Command.DeltaWestRepeat8Bit:
                        bitLength3 = (uint)sc.BsParameter.ReadUnsigned(1);
                        commandRepetitions = bitLength3 == 1 ? (int)sc.BsParameter.ReadUnsigned(header.ShortParameterLengthInBits) + 3
                                                             : (int)sc.BsParameter.ReadUnsigned(header.LongParameterLengthInBits) + 1;
                        for (int rep = 0; rep < commandRepetitions; rep++)
                        {
                            int delta = (int)sc.BsData.ReadSigned(8);
                            output[outputPointer] = (ushort)(output[outputPointer - 1] + delta);
                            outputPointer++;
                        }
                        break;

                    case (uint)Command.DeltaNorthOnce4Bit:
                        output[outputPointer] = (ushort)((output[outputPointer - header.ImageWidth] + sc.BsData.ReadSigned(4)));
                        outputPointer++;
                        break;

                    case (uint)Command.DeltaNorthOnce8Bit:
                        output[outputPointer] = (ushort)((output[outputPointer - header.ImageWidth] + sc.BsData.ReadSigned(8)));
                        outputPointer++;
                        break;

                    case (uint)Command.DeltaNorthRepeat4Bit:
                        bitLength3 = (uint)sc.BsParameter.ReadUnsigned(1);
                        commandRepetitions = bitLength3 == 1 ? (int)sc.BsParameter.ReadUnsigned(header.ShortParameterLengthInBits) + 3
                                                             : (int)sc.BsParameter.ReadUnsigned(header.LongParameterLengthInBits) + 1;
                        for (int rep = 0; rep < commandRepetitions; rep++)
                        {
                            int delta = (int)sc.BsData.ReadSigned(4);
                            output[outputPointer] = (ushort)(output[outputPointer - header.ImageWidth] + delta);
                            outputPointer++;
                        }
                        break;

                    case (uint)Command.DeltaNorthRepeat8Bit:
                        bitLength3 = (uint)sc.BsParameter.ReadUnsigned(1);
                        commandRepetitions = bitLength3 == 1 ? (int)sc.BsParameter.ReadUnsigned(header.ShortParameterLengthInBits) + 3
                                                             : (int)sc.BsParameter.ReadUnsigned(header.LongParameterLengthInBits) + 1;
                        for (int rep = 0; rep < commandRepetitions; rep++)
                        {
                            int delta = (int)sc.BsData.ReadSigned(8);
                            output[outputPointer] = (ushort)(output[outputPointer - header.ImageWidth] + delta);
                            outputPointer++;
                        }
                        break;

                    case (uint)Command.Lookup4Bit:
                        index = (ushort)sc.BsData.ReadUnsigned(4);
                        if (index > 176)
                        {
                            CreateProblemList(output, outputPointer);
                            Console.WriteLine("Problem");
                        }
                        output[outputPointer] = sc.GetUshortFromByteLookUpTableArray(index);
                        outputPointer++;
                        break;

                    case (uint)Command.Lookup8Bit:
                        index = (ushort)(sc.BsData.ReadUnsigned(8) + 16);
                        if (index > 176)
                        {
                            CreateProblemList(output, outputPointer);

                            Console.WriteLine("Problem");
                        }
                        output[outputPointer] = sc.GetUshortFromByteLookUpTableArray(index);
                        outputPointer++;
                        break;
                    
                    case (uint)Command.Lookup12Bit:
                        index = (ushort)(sc.BsData.ReadUnsigned(12) + 272);
                        if (index > 176)
                        {
                            CreateProblemList(output, outputPointer);
                            Console.WriteLine("Problem");
                        }
                        output[outputPointer] = sc.GetUshortFromByteLookUpTableArray(index);
                        outputPointer++;
                        break;

                    case (uint)Command.Unchanged:
                        output[outputPointer] = (ushort)sc.BsData.ReadUnsigned(16);
                        outputPointer++;
                        break;

                    default:
                    throw new Exception("There is sth wrong in Decompressor!");
                }
            }


            for (int i = 0; i < output.Length; i++)
            {
                outputStream.WriteByte((byte)(output[i] & 0xFF));
                outputStream.WriteByte((byte)(output[i] >> 8));
            }

        }

        public static void CreateProblemList(ushort[] output, uint outputPointer)
        {

            string tempVar = "";
            for (var temp = outputPointer - 100; temp < outputPointer; temp++)
            {
                tempVar += "\r\n" + output[temp];
            }

        }

    }
}
