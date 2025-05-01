using System.IO.Compression;
using System.IO;
using System.Text;

namespace PierToPierPlugin
{
    public static class GCompressor
    {
        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] array = new byte[4096];
            int count;
            while ((count = src.Read(array, 0, array.Length)) != 0)
            {
                dest.Write(array, 0, count);
            }
        }
        public static byte[] Zip(string str)
        {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                using (MemoryStream memoryStream2 = new MemoryStream())
                {
                    using (GZipStream gzipStream = new GZipStream(memoryStream2, CompressionMode.Compress))
                    {
                        GCompressor.CopyTo(memoryStream, gzipStream);
                    }
                    result = memoryStream2.ToArray();
                }
            }
            return result;
        }
        public static string Unzip(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "";
            }
            string @string;
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                using (MemoryStream memoryStream2 = new MemoryStream())
                {
                    using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        GCompressor.CopyTo(gzipStream, memoryStream2);
                    }
                    @string = Encoding.UTF8.GetString(memoryStream2.ToArray());
                }
            }
            return @string;
        }
    }
}