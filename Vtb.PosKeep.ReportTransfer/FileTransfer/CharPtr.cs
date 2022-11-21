namespace Vtb.PosKeep.ReportTransfer.FileProcessing
{
    using System.Runtime.CompilerServices;

    public struct CharPtr
    {
        private char[] c;
        private int current;

        private CharPtr(char[] c, int current = 0) { this.c = c; this.current = current; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CharPtr(char [] value) => new CharPtr(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CharPtr(string value) => value.ToCharArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator char(CharPtr value) => (value) ? value.c[value.current] : default(char);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(CharPtr value) => value.c != null && value.c.Length > value.current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharPtr operator ++(CharPtr value) => new CharPtr(value.c, value.current +1);               
    }

    public static class CharPtrUtil
    {
        public static bool CheckByPattern(this CharPtr s, CharPtr p)
        {
            CharPtr rs = default(CharPtr), rp = default(CharPtr);
            while (true)
            {
                if (p == '*') { rs = s; rp = ++p; }
                else if (!s) return !p;
                else if (s == (char)p || p == '?') { s = ++s; p = ++p; }
                else if (rs) { s = ++rs; p = rp; }
                else return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckByPattern(this string str, string pattern)
        {
            return CheckByPattern((CharPtr)str, pattern);
        }

        public static bool CheckByPattern(this string str, string[] patterns)
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                if (CheckByPattern(str, patterns[i]))
                    return true;
            }

            return false;
        }
    }
}
