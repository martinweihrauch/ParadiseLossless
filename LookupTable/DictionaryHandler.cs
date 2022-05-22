using PlCompressor.Output.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.LookupTable
{
    internal static class DictionaryHandler
    {
        public static void AddTopListPositionToDictionary(Dictionary<ushort, UnchangedFile> unchanged, List<UnchangedFile> top )
        {
            int counter = 0;

            foreach(var file in top)
            {
                unchanged[file.Value].RankInTopNList = counter;
                counter++;   
            }
        }
    }
}
