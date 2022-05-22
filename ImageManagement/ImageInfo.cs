using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor.ImageManagement
{
    public class ImageInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitSize { get; set; }
        public int ImageType { get; set; } // 1 = 8 Bit Grey, 2 = 16 Grey, 3 = RGB
    }
}
