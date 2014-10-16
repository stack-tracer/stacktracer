using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    public static class Algorithms
    {
        public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
        //public static readonly HashAlgorithm SHA1 = new SHA1Managed();
        //public static readonly HashAlgorithm SHA256 = new SHA256Managed();
        //public static readonly HashAlgorithm SHA384 = new SHA384Managed();
        //public static readonly HashAlgorithm SHA512 = new SHA512Managed();
        //public static readonly HashAlgorithm RIPEMD160 = new RIPEMD160Managed();


        public static string GetChecksum(string filePath, HashAlgorithm algorithm)
        {
            using (var stream = new BufferedStream(File.OpenRead(filePath), 100000))
            {
                return GetChecksum(algorithm, stream);
            }
        }

        public static string GetChecksum(HashAlgorithm algorithm, Stream stream)
        {
            byte[] hash = algorithm.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
}
