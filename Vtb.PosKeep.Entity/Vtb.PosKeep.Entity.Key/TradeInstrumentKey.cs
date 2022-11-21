namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class TradeInstrumentKeyReference : HR { }

    // TIR - TradeInstrumentReference
    /// <summary>
    /// TradeInstrumentReference
    /// </summary>
    public abstract class TIR : HR { }

    public static class TradeInstrumentKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TradeInstrumentKey ToTradeInstrumentKey(params int[] ids)
        {
            return new TradeInstrumentKey(TradeInstrument.Create(ids[1].ToInstrumentKey(), ids[0].ToCurrencyKey()));
        }
    }

    public struct TradeInstrumentKey : ISingleKey<int, TradeInstrumentKeyReference>, IEquatable<TradeInstrumentKey>
    {
        private readonly int m_value;
        public TradeInstrumentKey(int value) { m_value = value; }
        public TradeInstrumentKey(InstrumentKey instrument, CurrencyKey currency)
            : this(TradeInstrument.Create(instrument, currency))
        { }
        public TradeInstrumentKey(CurrencyKey currency, InstrumentKey instrument)
            : this(TradeInstrument.Create(instrument, currency))
        { }
        public override string ToString() => ((TradeInstrument)m_value).ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => Equals((TradeInstrumentKey)obj);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(TradeInstrumentKey other) => m_value == other.m_value;

        public InstrumentKey Instrument { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ((TradeInstrument)m_value).Instrument; }
        public CurrencyKey Currency { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ((TradeInstrument)m_value).Currency; }
    }
}
