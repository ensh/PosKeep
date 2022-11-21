namespace Vtb.PosKeep.Entity.Data
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    // RR - RateReference
    /// <summary>
    /// RateReference
    /// </summary>
    public abstract class RR : HR { }

    public struct Rate : IEntityPoolSubject
    {
        private readonly int Number;
        public decimal Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Value[Number]; } }
        public bool IsNull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number == 0; } }
        public static Rate Empty { get; private set; }
        public static Rate One { get; private set; }

        public static int Create(decimal value)
        {
            int i = EntityPool<RtP>.Next();
            s_Value[i] = value;
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(Rate Rate)
        {
            return Rate.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Rate(decimal Rate)
        {
            return Create(Rate);
        }

        private static decimal[] s_Value;
        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EntityPool<RtP>.LastNumber; }
        }
        public static void Init(int size)
        {
            s_Value = new decimal[size];
            EntityPool<RtP>.Reset();
            Empty = new Rate(0);
            One = Create(1.0m);
        }

        public Rate(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() { EntityPool<RtP>.Free(Number); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Rate Rate) { return Rate.Number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Rate(int RateNumber) { return new Rate(RateNumber); }

        public override string ToString()
        {
            return string.Concat("Value: ", Value.ToString());
        }
    }

    public static class RateUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<int, T> AsToken<T>(this HD<Rate, T> Rate) where T : HR
        {
            return new HD<int, T>(Rate.Timestamp, Rate.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<int, T>> OfToken<T>(this IEnumerable<HD<Rate, T>> hists) where T : HR
        {
            foreach (var hist in hists)
                yield return new HD<int, T>(hist.Timestamp, hist.Data);
        }
    }
}

