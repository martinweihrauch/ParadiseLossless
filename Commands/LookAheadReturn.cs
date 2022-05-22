using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.LookupTable.Model
{
    internal class LookAheadReturn
    {
        public long ParameterCount { get; set; }
        public List<int> DeltaList { get; set; }

        public LookAheadReturn()
        {
            DeltaList = new List<int>();
            ParameterCount = 0;
        }
    }
    
}
