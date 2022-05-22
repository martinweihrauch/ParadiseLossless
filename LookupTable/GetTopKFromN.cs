using PlCompressor.ImageManagement;
using PlCompressor.Output.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.LookupTable
{
    public static class GetTopKFromN
    {
        public static List<UnchangedFile> TopNSorted(Dictionary<ushort, UnchangedFile> _unchangedInformation, int n)
        {
            List<UnchangedFile> top = new List<UnchangedFile>(n + 1);
            var toBeRemoved = new List<ushort>();

            for (int i = 0; i < n; i++)
            {
                if (i < _unchangedInformation.Count)
                {
                    var item = _unchangedInformation.ElementAt(i);
                    item.Value.Value = item.Key;
                    if (item.Value.Frequency < 4)
                    {
                        // If this is below 4, then it is cheaper to store the original value, so remove from this list
                        toBeRemoved.Add(item.Key);
                    }
                    else
                    {
                        top.Add(item.Value);
                    }

                }
                else
                {
                    break;
                }
            }

            top.Sort((x, y) => y.Frequency.CompareTo(x.Frequency));

            if (n < _unchangedInformation.Count - 1)
            {
                for (int i = n; i < _unchangedInformation.Count; i++)
                {
                    var item = _unchangedInformation.ElementAt(i);
                    item.Value.Value = item.Key;
                    if (item.Value.Frequency < 4)
                    {
                        toBeRemoved.Add(item.Key);
                        continue;
                    }
                    uint frequency = item.Value.Frequency;
                    int index = top.BinarySearch(item.Value, new ValueComparer());
                    if (index < 0) index = ~index;
                    if (index < n)                    // if (index != 0)
                    {
                        top.Insert(index, item.Value);
                        if(n < top.Count)
                        {
                            top.RemoveAt(n);              // top.RemoveAt(0)
                        }
                    }
                }
            }


            return top;  // return ((IEnumerable<double>)top).Reverse();
        }

        public static IEnumerable<double> TopNSorted2(this IEnumerable<uint> source, int n)
        {
            List<double> top = new List<double>(n + 1);
            using (var e = source.GetEnumerator())
            {
                for (int i = 0; i < n; i++)
                {
                    if (e.MoveNext())
                        top.Add(e.Current);
                    else
                        throw new InvalidOperationException("Not enough elements");
                }
                top.Sort();
                while (e.MoveNext())
                {
                    double c = e.Current;
                    int index = top.BinarySearch(c);
                    if (index < 0) index = ~index;
                    if (index < n)                    // if (index != 0)
                    {
                        top.Insert(index, c);
                        top.RemoveAt(n);              // top.RemoveAt(0)
                    }
                }
            }
            return top;  // return ((IEnumerable<double>)top).Reverse();
        }
    }
   
}
