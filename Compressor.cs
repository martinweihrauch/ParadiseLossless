using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlCompressor
{
    internal static class Compressor
    {
        public static Stream Start(Stream image)
        {
            var buffer = new byte[1];
            int readBytes;
            while((readBytes = image.Read(buffer, 0, 1)) > 0) //
            {
                
            }

            return image;
        }
    }
}
