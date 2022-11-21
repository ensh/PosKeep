namespace Vtb.PosKeep.Entity.Data
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public struct Quote : IEntityPoolSubject
    {
        private readonly int Number;
        public decimal Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Value[Number]; } }
        public bool IsNull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number == 0; } }
        public static Quote Empty { get; private set; }

        public static int Create(decimal value)
        {
            int i = EntityPool<QP>.Next();
            s_Value[i] = value;
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(Quote quote)
        {
            return quote.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quote(decimal quote)
        {
            return Create(quote);
        }

        private static decimal[] s_Value;
        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EntityPool<QP>.LastNumber; }
        }
        public static void Init(int size)
        {
            s_Value = new decimal[size];
            EntityPool<QP>.Reset();
            Empty = new Quote(0);
        }

        public Quote(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() { EntityPool<QP>.Free(Number); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Quote quote) { return quote.Number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quote(int quoteNumber) { return new Quote(quoteNumber); }

        public override string ToString()
        {
            return string.Concat("Value: ", Value.ToString());
        }
    }

    public static class QuoteUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<int, T> AsToken<T>(this HD<Quote, T> quote) where T : HR
        {
            return new HD<int, T>(quote.Timestamp, quote.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<int, T>> OfToken<T>(this IEnumerable<HD<Quote, T>> hists) where T : HR
        {
            foreach (var hist in hists)
                yield return new HD<int, T>(hist.Timestamp, hist.Data);
        }
    }
}
