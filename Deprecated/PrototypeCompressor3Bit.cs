using PlCompressor.ImageManagement;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor
{
    public class PrototypeCompressor3Bit
    {
        private long _bitsTotal = 0;
        private long _countDeltaWest4 = 0;
        private long _countDeltaWest8 = 0;
        private long _countDeltaNorth4 = 0;
        private long _countDeltaNorth8 = 0;

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
        private List<int> _pointerList;
        private ImageInfo _imageInfo;
        private Dictionary<int, int> _countVariationsOfValues;

        public PrototypeCompressor3Bit()
        {
            _imageInfo = new ImageInfo();
            _countVariationsOfValues = new Dictionary<int, int>();
            _pointerList = new List<int>();
        }

        public long Estimate(byte[] image, ImageInfo imageInfo)
        {
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
            /*
   
            for (int y = 1; y < imageInfo.Height; y += 2)
            {
                Console.WriteLine("\r\n\r\nNEW 2 LINES\r\n");
                for (int x = 0; x < imageInfo.Width; x++)
                {
                    int delta = vs[y * imageInfo.Width + x] - vs[(y - 1) * imageInfo.Width + x];
                    Console.Write(vs[y * imageInfo.Width + x] + "(" + delta + ") | ");
                    //Console.Write(delta + " | ");
                }
                
            }
   
            */
            // Loop through ushort array
            long pointer = 1;
            bool unchangeable = false;
            
            var unchangedList = new Dictionary<int, int>();
            int f = 0;
            long foundPointer = 0;

            while (1 == 1 && (pointer < vs.Length))
            {
                //Console.WriteLine("Pointer: " + pointer);
                //Console.WriteLine(pointer);
                int temp = vs[pointer];

                int tmp2 = vs[pointer] - vs[pointer - 1];
                int tmp3 = (short)(vs[pointer] - vs[pointer - 1]);
                if(tmp2 != tmp3 )
                {
                    f++;
                    //Console.WriteLine("\r\nTmp2: " + tmp2 + "   tmp3: " + tmp3 + "    f: " + f + " pointer: " + pointer);
                }

                if (vs[pointer] == vs[pointer - 1]) // Clone West?
                {
                    unchangeable = false;
                    long lengthCloneWest = LookAheadCloneWest(vs, ref pointer);
                    if(lengthCloneWest == 1)
                    {
                        _countCloneWestOnlyOne++;
                        _bitsTotal += 3;
                    }
                    else
                    {
                        _bitsTotal += 3 + 8;
                    }
                    _countCloneWest += lengthCloneWest;
                }
                
                else if ((pointer > _imageInfo.Width - 1) && (vs[pointer] == vs[pointer - _imageInfo.Width])) //Clone North?
                {
                    unchangeable = false;
                    long lengthCloneNorth = LookAheadCloneNorth(vs, ref pointer);
                    if (lengthCloneNorth == 1)
                    {
                        _countCloneNorthOnlyOne++;
                        _bitsTotal += 3;
                    }
                    else
                    {
                        _bitsTotal += 3 + 8;
                    }

                    if (lengthCloneNorth > _maximumLengthFoundForCloneNorth)
                    {
                        _maximumLengthFoundForCloneNorth = lengthCloneNorth;
                    }
                    _countCloneNorth += lengthCloneNorth;
                }

                else if (vs[pointer] - vs[pointer - 1] <= 128 && vs[pointer] - vs[pointer - 1] >= -128)  // Delta West
                {
                    unchangeable = false;
                    long lengthDeltas;
                    if (vs[pointer] - vs[pointer - 1] <= 8 && vs[pointer] - vs[pointer - 1] >= -8)
                    {
                        lengthDeltas = LookAheadDeltaWest(vs, ref pointer, 4);
                        if (lengthDeltas == 1)
                        {
                            _countDeltaWestOnlyOne++;
                            _bitsTotal += 3 + 4;
                        }
                        else
                        {
                            _bitsTotal += 3 + 8 + lengthDeltas * 4;
                        }
                        _countDeltaWest4 += lengthDeltas;
                    }
                    else
                    {
                        lengthDeltas = LookAheadDeltaWest(vs, ref pointer, 8);
                        if (lengthDeltas == 1)
                        {
                            _countDeltaWestOnlyOne++;
                            _bitsTotal += 3 + 8;

                        }
                        else
                        {
                            _bitsTotal += 3 + 8 + lengthDeltas * 8;
                        }
                        _countDeltaWest8 += lengthDeltas;

                    }
                }

                /*
            

                else if (pointer > _imageInfo.Width - 1  // Delta North
                    && vs[pointer] - vs[pointer - _imageInfo.Width] <= 128
                    && vs[pointer] - vs[pointer - _imageInfo.Width] >= -128)
                {
                    unchangeable = false;
                    long lengthDeltas;
                    if (vs[pointer] - vs[pointer - _imageInfo.Width] <= 8 && vs[pointer] - vs[pointer - _imageInfo.Width] >= -8)
                    {
                        lengthDeltas = LookAheadDeltaNorth(vs, ref pointer, 4);
                        if (lengthDeltas == 1)
                        {
                            _countDeltaNorthOnlyOne++;
                            _bitsTotal += 4 + 4;
                        }
                        else
                        {
                            _bitsTotal += 4 + 8 + lengthDeltas * 4;
                        }
                        _countDeltaNorth4 += lengthDeltas;
                        continue;

                    }
                    else
                    {
                        lengthDeltas = LookAheadDeltaNorth(vs, ref pointer, 8);
                        if (lengthDeltas == 1)
                        {
                            _countDeltaNorthOnlyOne++;
                            _bitsTotal += 4 + 8;

                        }
                        else
                        {
                            _bitsTotal += 4 + 8 + lengthDeltas * 8;
                        }
                        _countDeltaNorth8 += lengthDeltas;
                        continue;

                    }
                }


                /*
                else if ((foundPointer = LookAheadForCloneable(vs, ref pointer)) >= 0) //Cloneable elements further west or north?
                {
                    _countCloneFarAway++;
                    pointer++;
                    unchangeable = false;
                    _bitsTotal += 4 + 4;
                    continue;
                }
                */



                /*
                // One of the 2 ushort bytes is the same
                else if(pointer > _imageInfo.Width &&
                    ((vs[pointer] & 0xFF)  == (vs[pointer - 1] & 0xFF) 
                    || (vs[pointer] >> 0xFF) == (vs[pointer - 1] & 0xFF)
                    || (vs[pointer] & 0xFF) == (vs[pointer - _imageInfo.Width] & 0xFF)
                    || (vs[pointer] >> 0xFF) == (vs[pointer - _imageInfo.Width] & 0xFF)))
                {
                    if((vs[pointer] & 0xFF) == (vs[pointer - 1] & 0xFF) // West
                    || (vs[pointer] >> 0xFF) == (vs[pointer - 1] & 0xFF))
                    {
                        _countCloneByte++;
                        _bitsTotal += 4;
                    }
                    if ((vs[pointer] & 0xFF) == (vs[pointer - _imageInfo.Width] & 0xFF) // North
                    || (vs[pointer] >> 0xFF) == (vs[pointer - _imageInfo.Width] & 0xFF))
                    {
                        _countCloneByte++;
                        _bitsTotal += 4;
                    }

                }
                */
                else
                {
                    //Console.WriteLine("unchanged: " + vs[pointer]);
                    _pointerList.Add((int)pointer);
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
                    // Unchanged data;
                }

                if (!unchangeable && _numberOfUnchangeableValues > 0)
                {
                    _bitsTotal += _numberOfUnchangeableValues * 16;
                    _unchangedTotalSize +=  _numberOfUnchangeableValues * 16;
                    _numberOfUnchangeableValues = 0;
                    
                }
                pointer++;

            }

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

        private long LookAheadForCloneable(ushort[] image, ref long pointer)
        {
            long westPointer = pointer;

            while (westPointer > 0 && (westPointer > pointer - 17))
            {
                long northPointer = westPointer - _imageInfo.Width;

                while (northPointer > 0 && (northPointer > pointer - 17 * _imageInfo.Width))
                {
                    if (image[northPointer] == image[pointer])
                    {
                        return northPointer;
                    }
                    northPointer -= _imageInfo.Width;
                }
                westPointer--;
                if (westPointer > 0 && image[pointer] == image[westPointer])
                {
                    return westPointer;
                }
            }
            return -1; //None found that matches          
        }

        private long LookAheadCloneWest(ushort[] image, ref long pointer)
        {
            long counter = 0;
            while(pointer < image.Length - 1)
            {
                if(image[pointer] == image[pointer - 1])
                {
                    pointer++;
                    counter++;
                }
                else
                {
                    pointer--; //This is for the case if pointer = image.length-1
                    break;
                }
            }
            pointer++;
            return counter;
        }

        private long LookAheadCloneNorth(ushort[] image, ref long pointer)
        {
            long counter = 0;
            while (pointer < image.Length - 1)
            {
                if (image[pointer] == image[pointer - _imageInfo.Width])
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

        private long LookAheadDeltaNorth(ushort[] image, ref long pointer, int bitSize)
        {
            long counter = 0;

            while (pointer < image.Length - 1)
            {
                int delta = image[pointer] - image[pointer - _imageInfo.Width];
                double upperLimit = Math.Pow(2, bitSize - 1);
                double lowerLimit = -1 * Math.Pow(2, bitSize - 1);
                if (delta != 0 && delta <= upperLimit && delta >= lowerLimit)
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

        private long LookAheadDeltaWest(ushort[] image, ref long pointer, int bitSize)
        {
            long counter = 0;

            while (pointer < image.Length - 1)
            {
                int delta = image[pointer] - image[pointer - 1];
                if (delta != 0 && delta <= Math.Pow(2, bitSize - 1) && delta >= -1 * Math.Pow(2, bitSize - 1))
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

        private long LookAheadLookupTable(ushort[] image, ref long pointer)
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

    }
}
