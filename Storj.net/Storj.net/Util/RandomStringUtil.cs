using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class RandomStringUtil
    {
        const int NAME_LENGTH = 48;
        const int CHALLENGE_LENGTH = 32;
        static RandomNumberGenerator random = RandomNumberGenerator.Create();

        [DebuggerStepThrough]
        internal static string GenerateRandomName()
        {
            return GenerateRandomString(NAME_LENGTH);
        }

        [DebuggerStepThrough]
        internal static string GenerateRandomChallengeString()
        {
            return BitConverter.ToString(Encoding.ASCII.GetBytes(GenerateRandomString(CHALLENGE_LENGTH))).Replace("-", "").ToLower().Substring(0, CHALLENGE_LENGTH);
        }

        [DebuggerStepThrough]
        private static string GenerateRandomString(int length)
        {
            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            input += input.ToUpper();

            var chars = Enumerable.Range(0, length)
                                   .Select(x => input[new Random(GetRandomNumber()).Next(0, input.Length)]);

            return new string(chars.ToArray());
        }

        private static int GetRandomNumber()
        {
            byte[] number = new byte[4];
            random.GetBytes(number);
            return BitConverter.ToInt32(number, 0);
        }
    }
}
