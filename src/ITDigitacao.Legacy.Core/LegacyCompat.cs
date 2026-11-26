using System;

namespace ITDigitacao.Core
{
    internal static class LegacyCompat
    {
        internal static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;
            for (int i = 0; i < value.Length; i++)
                if (!Char.IsWhiteSpace(value[i])) return false;
            return true;
        }
    }
}
