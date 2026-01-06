using System;
using System.Linq;

namespace EddyLib
{
    public static partial class Utilities
    {
        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static bool HasWhiteSpace(string input)
        {
            bool hasWhiteSpace = false;

            foreach (char ch in input)
            {
                if (Char.IsWhiteSpace(ch))
                {
                    hasWhiteSpace = true;
                }
            }
            return hasWhiteSpace;
        }
    }
}
