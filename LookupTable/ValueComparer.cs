using PlCompressor.Output.Model;

namespace PlCompressor.LookupTable
{
 
    public class ValueComparer : IComparer<UnchangedFile>
    {
        public int Compare(UnchangedFile x, UnchangedFile y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return x.Frequency == y.Frequency ? 0 :
                        x.Frequency > y.Frequency ? 1 : -1;
        }
    }
 
}