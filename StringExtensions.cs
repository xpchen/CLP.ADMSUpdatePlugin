using System;

namespace CLP.ADMSUpdatePlugin
{
    public static class StringExtensions
    {
        public static string ToFixedLength(this string input, int length, char paddingChar = ' ')
        {
            if (string.IsNullOrEmpty(input))
                return new string(paddingChar, length);

            return input.Length > length
                ? input.Substring(0, length)
                : input.PadRight(length, paddingChar);
        }
    }
}
