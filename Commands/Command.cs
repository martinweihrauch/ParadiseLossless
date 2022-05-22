using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.Output.Model
{
    enum Command { 
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
    }
}
