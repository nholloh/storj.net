using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class RandomStringUtil
    {
        const int NAME_LENGTH = 48;
        const int CHALLENGE_LENGTH = 32;

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
            Random random = new Random();

            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            input += input.ToUpper();

            var chars = Enumerable.Range(0, length)
                                   .Select(x => input[random.Next(0, input.Length)]);

            //otherwise multiple randoms will result in the same string
            Thread.Sleep(50);

            return new string(chars.ToArray());
        }
    }
}
