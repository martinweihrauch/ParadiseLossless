namespace PlCompressor
{
    public class Lp
    {
        public Lp()
        {

        }

        public Stream Compress(Stream image)
        {
            // Write Magic Word + Header
            // Create arrays for 1. commands, 2. parameters, 3. data
            Compressor.Start(image);
            return image;
            /*
                byte source = 0xAD;
                var hiNybble = (source & 0xF0) >> 4; //Left hand nybble = A
                var loNyblle = (source & 0x0F);      //Right hand nybble = D
            */
        }


        public Stream Decompress(Stream image)
        {
            Decompressor.Decompress((MemoryStream)image, new MemoryStream());
            return image;

        }

    }
}