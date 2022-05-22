using PlCompressor.ImageManagement;
using PlCompressor.LookupTable;
using PlCompressor.LookupTable.Model;
using PlCompressor.Model;
using PlCompressor.Output;
using PlCompressor.Output.Model;
using PlCompressor.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor
{
    public class CompressionAlgorithm
    {
        private long _bitsTotal = 0;
        private Switches _switches;
        private OutputFile _outputFile;
        private Dictionary<ushort, UnchangedFile> _unchangedInformation;
        private ImageInfo _imageInfo;
        private Stats _stats;

        /*
         * Statistics
         * 
         */
        private long _howOftenWasChoiceChanged = 0;
        private long _howManyLengthWasChanged = 0;
        private string _parameterStringClone = "";
        private string _parameterStringDelta = "";
        private long _countDeltaWest4 = 0;
        private long _countDeltaWest8 = 0;
        private long _countDeltaNorth4 = 0;
        private long _countDeltaNorth8 = 0;
        private long _countDeltaWest2Times = 0;
        private long _numberOfUnchangeableValues = 0;
        private long _countCloneNorth = 0;
        private long _countCloneWest = 0;
        private long _countDeltaNorth = 0;
        private long _countCloneWestOnlyOne = 0;
        private long _countCloneNorthOnlyOne = 0;
        private int _countCloneFarAway = 0;
        private int _countDeltaWestOnlyOne = 0;
        private int _countDeltaNorthOnlyOne = 0;
        private long _unchangedTotalSize = 0;
        private int _unchangedCount = 0;
        private long _maximumLengthFoundForCloneNorth = 0;
        private int _minUnchangedValue = 0;
        private int _maxUnchangedValue = 0;
        private double _averageUnchangedValue = 0;
        private double _standardDeviation = 0;
        private int _countCloneByte = 0;
        private Dictionary<int, int> _countVariationsOfValues;

        public CompressionAlgorithm(Switches switches)
        {
            _imageInfo = new ImageInfo();
            _switches = switches;
            _stats = new Stats();
            _unchangedInformation = new Dictionary<ushort, UnchangedFile> ();
            _outputFile = new OutputFile(_unchangedInformation, _stats, _switches);
            _countVariationsOfValues = new Dictionary<int, int>();
        }

        public long Estimate(MemoryStream inputStream, Stream outputStream, ImageInfo imageInfo, Switches switches)
        {
            _outputFile.SetImageInfo(imageInfo);    
            _switches.LongParameterLengthInBits = switches.LongParameterLengthInBits;
            byte[] image = inputStream.ToArray();
            if(switches == null)
            {
                switches = new Switches();
            }
            _imageInfo = imageInfo;
            ushort[] vs = new ushort[(int)Math.Ceiling(((float)image.Length)/2)];

            for (int i = 0, j = 0; i < image.Length; i++)
            {
                if(i == image.Length - 1)
                {
                    // Last byte
                    vs[j] = image[i];
                }
                else
                {
                    vs[j] = BitConverter.ToUInt16(new byte[2] { image[i], image[i + 1] }, 0);
                    i++;
                    j++;
                }
            }

            uint pointer = 0;

            CreateCommandEntries(Command.Unchanged, new LookAheadReturn() { ParameterCount = 1}, vs[pointer]);
            pointer++;

            // Loop through ushort array
            bool unchangeable = false;
            
            var unchangedList = new Dictionary<int, int>();

            while (pointer < vs.Length)
            {

                /*
                 * CLONE WEST
                 */ 

                if (switches.CloneWest && vs[pointer] == vs[pointer - 1]) // Clone West?
                {
                    unchangeable = false;
                    var pointerBeforeLookWest = pointer;
                    var lookWest = LookAheadCloneWest(vs, ref pointer);
                    var pointerAfterLookWest = pointer;
                    pointer = pointerBeforeLookWest;
                    var lookNorth = new LookAheadReturn() { ParameterCount = 0};
                    if (switches.CloneNorth
                    && (pointer > _imageInfo.Width - 1)
                    && (vs[pointer] == vs[pointer - _imageInfo.Width])) //Clone North?
                    {
                        lookNorth = LookAheadCloneNorth(vs, ref pointer);
                    }
                    if(lookWest.ParameterCount >= lookNorth.ParameterCount)
                    {
                        pointer = pointerAfterLookWest;
                        if (lookWest.ParameterCount == 1)
                        {
                            _countCloneWestOnlyOne++;
                            _bitsTotal += 4;
                            CreateCommandEntries(Command.CloneWestOnce, lookWest);
                        }
                        else if (lookWest.ParameterCount == 2)
                        {
                            _bitsTotal += 8;
                            lookWest.ParameterCount = 1;
                            CreateCommandEntries(Command.CloneWestOnce, lookWest);
                            CreateCommandEntries(Command.CloneWestOnce, lookWest);
                        }
                        else
                        {
                            _bitsTotal += 4 + 8;
                            CreateCommandEntries(Command.CloneWestRepeat, lookWest);
                        }
                        _countCloneWest += lookWest.ParameterCount;
                    }
                    else
                    {
                        _howOftenWasChoiceChanged++;
                        _howManyLengthWasChanged += lookNorth.ParameterCount - lookWest.ParameterCount;
                        if (lookNorth.ParameterCount == 1)
                        {
                            _countCloneNorthOnlyOne++;
                            _bitsTotal += 4;
                            CreateCommandEntries(Command.CloneNorthOnce, lookNorth);

                        }
                        else if (lookNorth.ParameterCount == 2)
                        {
                            _bitsTotal += 8;
                            lookNorth.ParameterCount = 1;
                            CreateCommandEntries(Command.CloneNorthOnce, lookNorth);
                            CreateCommandEntries(Command.CloneNorthOnce, lookNorth);
                        }
                        else
                        {
                            _bitsTotal += 4 + 8;
                            CreateCommandEntries(Command.CloneNorthRepeat, lookNorth);
                        }

                        if (lookNorth.ParameterCount > _maximumLengthFoundForCloneNorth)
                        {
                            _maximumLengthFoundForCloneNorth = lookNorth.ParameterCount;
                        }
                        _countCloneNorth += lookNorth.ParameterCount;
                    }
                    continue;
                }

                /*
                 * CLONE NORTH
                 */

                else if (switches.CloneNorth 
                    && (pointer > _imageInfo.Width - 1) 
                    && (vs[pointer] == vs[pointer - _imageInfo.Width])) //Clone North?
                {
                    unchangeable = false;
                    var look = LookAheadCloneNorth(vs, ref pointer);
                    if (look.ParameterCount == 1)
                    {
                        _countCloneNorthOnlyOne++;
                        _bitsTotal += 4;
                        CreateCommandEntries(Command.CloneNorthOnce, look);

                    }
                    else if (look.ParameterCount == 2)
                    {
                        _bitsTotal += 8;
                        look.ParameterCount = 1;
                        CreateCommandEntries(Command.CloneNorthOnce, look);
                        CreateCommandEntries(Command.CloneNorthOnce, look);
                    }
                    else
                    {
                        _bitsTotal += 4 + 8;
                        CreateCommandEntries(Command.CloneNorthRepeat, look);
                    }

                    if (look.ParameterCount > _maximumLengthFoundForCloneNorth)
                    {
                        _maximumLengthFoundForCloneNorth = look.ParameterCount;
                    }
                    _countCloneNorth += look.ParameterCount;
                    continue;
                }

                /*
                * DELTA WEST
                */


                else if (switches.DeltaWest 
                    && vs[pointer] - vs[pointer - 1] <= 128 
                    && vs[pointer] - vs[pointer - 1] >= -128)  // Delta West
                {
                    unchangeable = false;
                    if (vs[pointer] - vs[pointer - 1] <= 8 && vs[pointer] - vs[pointer - 1] >= -8)
                    {
                        uint pointerBeforeLookWest = pointer;
                        var lookWest = LookAheadDeltaWest(vs, ref pointer, 4);
                        uint pointerAfterLookWest = pointer;
                        pointer = pointerBeforeLookWest;
                        var lookNorth = LookAheadDeltaNorth(vs, ref pointer, 4);
                        if (lookWest.ParameterCount < lookNorth.ParameterCount)
                        {
                            pointer = pointerBeforeLookWest;
                        }
                        else
                        {
                            pointer = pointerAfterLookWest;
                            if (lookWest.ParameterCount == 1)
                            {
                                _countDeltaWestOnlyOne++;
                                _bitsTotal += 4 + 4;
                                CreateCommandEntries(Command.DeltaWestOnce4Bit, lookWest);
                            }
                            else if (lookWest.ParameterCount == 2)
                            {
                                lookWest.ParameterCount = 1;
                                CreateCommandEntries(Command.DeltaWestOnce4Bit, lookWest);
                                lookWest.DeltaList[0] = lookWest.DeltaList[1];
                                CreateCommandEntries(Command.DeltaWestOnce4Bit, lookWest);
                            }
                            else
                            {
                                _bitsTotal += 4 + 8 + lookWest.ParameterCount * 4;
                                CreateCommandEntries(Command.DeltaWestRepeat4Bit, lookWest);

                            }
                            _countDeltaWest4 += lookWest.ParameterCount;
                            continue;
                        }

                    }
                    else
                    {
                        uint pointerBeforeLookWest = pointer;
                        var lookWest = LookAheadDeltaWest(vs, ref pointer, 8);
                        uint pointerAfterLookWest = pointer;
                        pointer = pointerBeforeLookWest;
                        var lookNorth = LookAheadDeltaNorth(vs, ref pointer, 8);
                        if (lookWest.ParameterCount < lookNorth.ParameterCount)
                        {
                            pointer = pointerBeforeLookWest;
                        }
                        else
                        {
                            pointer = pointerAfterLookWest;
                            if (lookWest.ParameterCount == 1)
                            {
                                _countDeltaWestOnlyOne++;
                                _bitsTotal += 4 + 8;
                                CreateCommandEntries(Command.DeltaWestOnce8Bit, lookWest);
                            }
                            else if (lookWest.ParameterCount == 2)
                            {
                                lookWest.ParameterCount = 1;
                                CreateCommandEntries(Command.DeltaWestOnce8Bit, lookWest);
                                lookWest.DeltaList[0] = lookWest.DeltaList[1];
                                CreateCommandEntries(Command.DeltaWestOnce8Bit, lookWest);

                            }
                            else
                            {
                                _bitsTotal += 4 + 8 + lookWest.ParameterCount * 8;
                                CreateCommandEntries(Command.DeltaWestRepeat8Bit, lookWest);

                            }
                            _countDeltaWest8 += lookWest.ParameterCount;
                            continue;
                        }
                    }
                }



                /*
                * DELTA NORTH
                */
                            

                if (switches.DeltaNorth 
                    && pointer > _imageInfo.Width - 1  // Delta North
                    && vs[pointer] - vs[pointer - _imageInfo.Width] <= 128
                    && vs[pointer] - vs[pointer - _imageInfo.Width] >= -128)
                {
                    unchangeable = false;
                    if (vs[pointer] - vs[pointer - _imageInfo.Width] <= 8 && vs[pointer] - vs[pointer - _imageInfo.Width] >= -8)
                    {
                        var lookNorth = LookAheadDeltaNorth(vs, ref pointer, 4);

                        //_parameterStringDelta += ", " + look.Parameterlength.ToString();
                        if (lookNorth.ParameterCount == 1)
                        {
                            _countDeltaNorthOnlyOne++;
                            _bitsTotal += 4 + 4;
                            CreateCommandEntries(Command.DeltaNorthOnce4Bit, lookNorth);
                        }
                        else if (lookNorth.ParameterCount == 2)
                        {
                            lookNorth.ParameterCount = 1;
                            CreateCommandEntries(Command.DeltaNorthOnce4Bit, lookNorth);
                            lookNorth.DeltaList[0] = lookNorth.DeltaList[1];
                            CreateCommandEntries(Command.DeltaNorthOnce4Bit, lookNorth);

                        }
                        else
                        {
                            _bitsTotal += 4 + 8 + lookNorth.ParameterCount * 4;
                            CreateCommandEntries(Command.DeltaNorthRepeat4Bit, lookNorth);

                        }
                        _countDeltaNorth4 += lookNorth.ParameterCount;
                        continue;

                    }
                    else
                    {
                        var look = LookAheadDeltaNorth(vs, ref pointer, 8);
                        //_parameterStringDelta += ", " + look.Parameterlength.ToString();
                        if (look.ParameterCount == 1)
                        {
                            _countDeltaNorthOnlyOne++;
                            _bitsTotal += 4 + 8;
                            CreateCommandEntries(Command.DeltaNorthOnce8Bit, look);

                        }
                        else if (look.ParameterCount == 2)
                        {
                            look.ParameterCount = 1;
                            CreateCommandEntries(Command.DeltaNorthOnce8Bit, look);
                            look.DeltaList[0] = look.DeltaList[1];
                            CreateCommandEntries(Command.DeltaNorthOnce8Bit, look);

                        }
                        else
                        {
                            _bitsTotal += 4 + 8 + look.ParameterCount * 8;
                            CreateCommandEntries(Command.DeltaNorthRepeat8Bit, look);

                        }
                        _countDeltaNorth8 += look.ParameterCount;
                        continue;

                    }
                }
                

                else
                {
                    /*
                    * UNCHANGED DATA --> LOOKUP TABLE
                    */
                    var look = new LookAheadReturn() { ParameterCount = 1 };
                    int keyInOutputFile = CreateCommandEntries(Command.Unchanged, look, vs[pointer]);

                    if (_unchangedInformation.ContainsKey(vs[pointer]))
                    {
                        _unchangedInformation[vs[pointer]].Frequency++;
                    }
                    else
                    {
                        _unchangedInformation.Add(vs[pointer], new UnchangedFile()
                        {
                            Frequency = 1,
                            KeyOfFileInOutputFileList = (uint)keyInOutputFile,
                        });
                    }
                   
                    unchangeable = true;
                    _numberOfUnchangeableValues++;
                    _unchangedCount++;

                    if (unchangedList.ContainsKey(vs[pointer]))
                    {
                        unchangedList[vs[pointer]]++;
                    }
                    else
                    {
                        unchangedList.Add(vs[pointer], 1);
                    }

                    var temp2 = vs[pointer];
                    if (_minUnchangedValue == 0)
                    {
                        _minUnchangedValue = temp2;
                    }
                    else if (_minUnchangedValue > temp2)
                    {
                        _minUnchangedValue = temp2;
                    }
                    if (_maxUnchangedValue < temp2)
                    {
                        _maxUnchangedValue = temp2;
                    }

                    if (_countVariationsOfValues.ContainsKey(vs[pointer]))
                    {
                        _countVariationsOfValues[vs[pointer]]++;
                    }
                    else
                    {
                        _countVariationsOfValues.Add(vs[pointer], 1);
                    }
                }

                if (!unchangeable && _numberOfUnchangeableValues > 0)
                {
                    _bitsTotal += _numberOfUnchangeableValues * 16;
                    _unchangedTotalSize += _numberOfUnchangeableValues * 16;
                    _numberOfUnchangeableValues = 0;
                    
                }
                pointer++;

            }
            Console.WriteLine("Unchanged: " + _unchangedTotalSize / 8 / 1024 + "  KB");

            _outputFile.CreateLookupTable(switches);
            _outputFile.FillCommandsOfUnchanged(_unchangedInformation);
            _outputFile.BuildOutputFile(outputStream, (uint)inputStream.Length);
            Console.WriteLine("Size in bytes: " + _stats.Bits / 8);
            Console.WriteLine("Parameter commands with par > 1 used count: " + _stats.CountParameterCommands);
            Console.WriteLine("Parameter length average: " + _stats.CountParameterLengths / _stats.CountParameterCommands);
            Console.WriteLine("Number of Entries: " + _outputFile.Entries.Count);
            Console.WriteLine("Max length of Parameters: " + _stats.MaxLengthParameter);

            _stats.CalculateCommandStats(_outputFile.Entries);
            for(var i = 0; i < _stats.CommandEffectiveness.Length; i++)
            {
                Console.WriteLine("Command: " + Enum.GetName(typeof(Command), i) + " freq: " + _stats.CommandEffectiveness[i].Frequency + " / bytes saved: " + _stats.CommandEffectiveness[i].BitsSaved / 8);
            }


            //DX = 5.240 MB
            //DX = 5.217 MB with Cloning twice repeatedly
            //DX = 5.098 MB after Deltaing twice 

            return _bitsTotal;

            var test = _countVariationsOfValues.OrderByDescending(x => x.Value).ToList<KeyValuePair<int, int>>();

            int totalOfReplacedValues = 0;
            for (int i = 0; i < 256; i++)
            {
                totalOfReplacedValues += test[i].Value;
                if(i == 15)
                {
                    Console.WriteLine("With 4 Bits, you can dictionary: " + totalOfReplacedValues + " values");
                }

            }
            //Console.WriteLine("With 8 Bits, you can dictionary: " + totalOfReplacedValues + " values");

            //_averageUnchangedValue = unchangedList.Average();
            //_standardDeviation = Math.Sqrt(unchangedList.Average(v => Math.Pow(v - _averageUnchangedValue, 2)));
            //int tempCount = unchangedList.Count(x => (x > _averageUnchangedValue - 128 && x < _averageUnchangedValue + 128));
            var testlist = unchangedList.ToList();
            var testlist2 = testlist.OrderByDescending(c => c.Value).ToList();
            int sum = 0;
            int sum2 = 0;
            for(var i = 0; i < 16; i++)
            {
                sum += testlist2[i].Value;
            }
            for (var i = 0; i < 256; i++)
            {
                sum2 += testlist2[i].Value;
            }

            return _bitsTotal;

            /*
            var unchangedToDisk = new byte[_pointerList.Count * 2];
            for (int i = 0; i < unchangedToDisk.Length; i += 2)
            {
                unchangedToDisk[i] = (byte)(vs[_pointerList[i/2]] & 0XFF);
                unchangedToDisk[i + 1] = (byte)(vs[_pointerList[i/2]] >> 8);
            }
            File.WriteAllBytes(@"D:\DICOM_Experiments\output\bytes.bit", unchangedToDisk);


            //int ww = 256;
            int wl = 30;

            var imgNew = new byte[_imageInfo.Width * _imageInfo.Height * 4];

            for(int i = 0; i < vs.Length; i++)
            {
                int val = (short)(vs[i]);


                if (_pointerList.Contains(i))
                {
                    imgNew[i * 4] = (byte)0;
                    imgNew[i * 4 + 1] = (byte)255;
                    imgNew[i * 4 + 2] = (byte)0;
                    imgNew[i * 4 + 3] = (byte)0;

                }
                else if (val >= wl && val < wl + 1000)
                {
                    int grey = vs[i] - wl;
                    imgNew[i * 4] = (byte)grey;
                    imgNew[i * 4 + 1] = (byte)grey;
                    imgNew[i * 4 + 2] = (byte)grey;
                    imgNew[i * 4 + 3] = (byte)0;

                }
                else
                {
                    imgNew[i * 4] = (byte)0;
                    imgNew[i * 4 + 1] = (byte)0;
                    imgNew[i * 4 + 2] = (byte)0;
                    imgNew[i * 4 + 3] = (byte)0;

                }
            }

            SaveBitmap("c:\\temp\\output", _imageInfo.Width, _imageInfo.Height, imgNew);


            return _bitsTotal;
            */
        }



        private void SaveBitmap(string fileName, int width, int height, byte[] imageData)
        {

            unsafe
            {
                fixed (byte* ptr = imageData)
                {

                    using (Bitmap image = new Bitmap(width, height, width * 4,
                                PixelFormat.Format32bppRgb, new IntPtr(ptr)))
                    {

                        image.Save(Path.ChangeExtension(fileName, ".jpg"));
                    }
                }
            }
        }


        private int CreateCommandEntries(Command command, LookAheadReturn look, ushort data = 0)
        {
            if(look.ParameterCount == 1)
            {
                var entry = new FileEntry() {
                    Command = (byte)command,
                    Deltas = look.DeltaList
                };
                if(command == Command.Unchanged)
                {
                    entry.Unchanged = true;
                    entry.Data = data;
                }
                _outputFile.AddEntry(entry);
            }
            else
            {
                if(look.ParameterCount < (Math.Pow(2, _switches.ShortParameterLengthInBits) + 3))
                {
                    _outputFile.AddEntry(new FileEntry()
                    {
                        Command = (byte)(command),
                        Parameter = (byte)(look.ParameterCount),
                        Data = data,
                        Deltas = look.DeltaList,
                        DeltaOffset = 0,
                        ParameterLength3BitElse8Bit = 1,
                        ParameterLengthInBit = _switches.ShortParameterLengthInBits
                    });
                }
                else
                {
                    int combinations = (int)Math.Pow(2, _switches.LongParameterLengthInBits); // eg 256, 128, 64, etc
                    // Parameter Lengths 3-n have to be represented. Minus 2, so that param length 3 = 1,
                    // e. g. 3 - 258 (8 bits) or 3 - 6 (2 bits) or 3 - 18 (4 bits)
                    int loopLength = (int)Math.Ceiling((double)(look.ParameterCount) / combinations);
                    for (var i = 0; i < loopLength; i++)
                    {
                        long parameter = i < loopLength - 1 ? (combinations) : (look.ParameterCount - i * combinations);
                        _outputFile.AddEntry(new FileEntry()
                        {
                            Command = (byte)(command),
                            Parameter = (byte)parameter,
                            Data = data,
                            Deltas = look.DeltaList,
                            DeltaOffset = (int)(i * (combinations)),
                            ParameterLength3BitElse8Bit = 0,
                            ParameterLengthInBit = _switches.LongParameterLengthInBits
                        });
                    }
                }
                if (_stats.MaxLengthParameter < look.ParameterCount) _stats.MaxLengthParameter = (uint)look.ParameterCount;
                _stats.CountParameterCommands++;
                _stats.CountParameterLengths += (uint)look.ParameterCount;
            }
            return _outputFile.Entries.Count() - 1;
        }

        private long LookAheadForCloneable(ushort[] image, ref uint pointer)
        {
            long westPointer = pointer;

            while (westPointer > 0 && (westPointer > pointer - 17))
            {
                long northPointer = westPointer - (long)_imageInfo.Width;

                while (northPointer > 0 && (northPointer > pointer - 17 * _imageInfo.Width))
                {
                    if (image[northPointer] == image[pointer])
                    {
                        return northPointer;
                    }
                    northPointer -= (long)_imageInfo.Width;
                }
                westPointer--;
                if (westPointer > 0 && image[pointer] == image[westPointer])
                {
                    return westPointer;
                }
            }
            return -1; //None found that matches          
        }

        private static LookAheadReturn LookAheadCloneWest(ushort[] image, ref uint pointer)
        {
            var look = new LookAheadReturn();
            while (pointer < image.Length - 1)
            {
                if(image[pointer] == image[pointer - 1])
                {
                    pointer++;
                    look.ParameterCount++;
                }
                else
                {
                    pointer--; //This is for the case if pointer = image.length-1
                    break;
                }
            }
            pointer++;
            return look;
        }

        private LookAheadReturn LookAheadCloneNorth(ushort[] image, ref uint pointer)
        {
            var look = new LookAheadReturn();
            while (pointer < image.Length - 1)
            {
                if (image[pointer] == image[pointer - _imageInfo.Width])
                {
                    pointer++;
                    look.ParameterCount++;
                }
                else
                {
                    pointer--;
                    break;
                }
            }
            pointer++;
            return look;
        }

        private LookAheadReturn LookAheadDeltaNorth(ushort[] image, ref uint pointer, int bitSize)
        {
            var look = new LookAheadReturn();
            if(pointer < _imageInfo.Width || (!(image[pointer] - image[pointer - _imageInfo.Width] <= 128
                    && image[pointer] - image[pointer - _imageInfo.Width] >= -128)))
            {
                return new LookAheadReturn() { ParameterCount = 0 };
            }

            while (pointer < image.Length - 1)
            {
                int delta = image[pointer] - image[pointer - _imageInfo.Width];
                double upperLimit = Math.Pow(2, bitSize - 1);
                double lowerLimit = -1 * Math.Pow(2, bitSize - 1);
                if (delta != 0 && delta <= upperLimit && delta >= lowerLimit)
                {
                    pointer++;
                    look.ParameterCount++;
                    look.DeltaList.Add(delta);
                }
                else
                {
                    pointer--;
                    break;
                }
            }
            pointer++;
            return look;
        }

        private static LookAheadReturn LookAheadDeltaWest(ushort[] image, ref uint pointer, int bitSize)
        {
            var look = new LookAheadReturn();

            while (pointer < image.Length - 1)
            {
                int delta = image[pointer] - image[pointer - 1];
                if (delta != 0 && delta <= Math.Pow(2, bitSize - 1) && delta >= -1 * Math.Pow(2, bitSize - 1))
                {
                    pointer++;
                    look.ParameterCount++;
                    look.DeltaList.Add(delta);
                }
                else
                {
                    pointer--;
                    break;
                }
            }
            pointer++;
            return look;
        }

        /*
        private LookAheadReturn LookAheadLookupTable(ushort[] image, ref uint pointer)
        {
            long counter = 0;
            while (pointer < image.Length - 1)
            {
                if (image[pointer] == image[pointer - 1])
                {
                    pointer++;
                    counter++;
                }
                else
                {
                    pointer--;
                    break;
                }
            }
            pointer++;
            return counter;
        }
        */
    }
}
