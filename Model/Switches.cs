using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Model
{
    public class Switches
    {
        public int LongParameterLengthInBits { get; set; }
        public int ShortParameterLengthInBits { get; set; }
        public int LongShortSwitchLengthInBits { get; set; }
        public bool CloneWest { get; set; }
        public bool CloneNorth { get; set; }
        public bool DeltaWest { get; set; }
        public bool DeltaNorth { get; set; }
        public bool Dictionary { get; set; }
        public Switches()
        {
            LongShortSwitchLengthInBits = 1;
            LongParameterLengthInBits = 8;
            ShortParameterLengthInBits = 3;
            CloneWest = CloneNorth = DeltaWest = DeltaNorth = Dictionary = true;
        }
    }
}
