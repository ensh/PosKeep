namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Key;

    public struct TradeInstrument : IEquatable<TradeInstrument>, IEntityPoolSubject
    {
        private readonly int Number;

        public InstrumentKey Instrument { get => s_Instrument[Number]; }
        public CurrencyKey Currency { get => s_Currency[Number]; }

        public static int Create(InstrumentKey instrument, CurrencyKey currency)
        {
            int i = EntityPool<TIP>.Next();
            s_Instrument[i] = instrument; s_Currency[i] = currency;

            var number = s_Trades.GetOrAdd(i, _ => i);

            if (number != i)
                EntityPool<TIP>.Free(i);
            return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => Instrument ^ Currency;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => Equals((TradeInstrument)obj);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(TradeInstrument other) => Instrument == other.Instrument && Currency == other.Currency;

        public override string ToString() => string.Concat(Instrument.ToString(), " : ", Currency.ToString());

        private static InstrumentKey[] s_Instrument;
        private static CurrencyKey[] s_Currency;
        private static ConcurrentDictionary<TradeInstrument, int> s_Trades;

        public static void Init(int size)
        {
            s_Instrument = new InstrumentKey[size];
            s_Currency = new CurrencyKey[size];
            s_Trades = new ConcurrentDictionary<TradeInstrument, int>(4, size);

            Empty = 0;

            EntityPool<TIP>.Reset();
        }

        public static TradeInstrument Empty { get; private set; }
        public static int LastNumber { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => EntityPool<TIP>.LastNumber; }
        private TradeInstrument(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Free() => EntityPool<TIP>.Free(Number);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(TradeInstrument value) { return value.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator TradeInstrument(int value) { return new TradeInstrument(value); }

    }
}
