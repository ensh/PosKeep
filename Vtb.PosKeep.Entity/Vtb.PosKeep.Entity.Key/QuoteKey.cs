namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class QuoteKeyReference : HR { }

    // QR - QuoteReference
    /// <summary>
    /// QuoteReference
    /// </summary>
    public abstract class QR : HR { }

    public struct QuoteKey : IComplexKey<CurrencyKey, InstrumentKey, QuoteKeyReference>, IEquatable<QuoteKey>
    {
        public readonly CurrencyKey Currency;
        public readonly InstrumentKey Instrument;

        public QuoteKey(CurrencyKey currency, InstrumentKey instrument) { Currency = currency; Instrument = instrument; }
        public QuoteKey(TradeInstrumentKey tradeKey) { Currency = tradeKey.Currency; Instrument = tradeKey.Instrument; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return Currency.GetHashCode() ^ Instrument.GetHashCode(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((QuoteKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return string.Concat("Currency: ", Currency.ToString(), ", Instrument: ", Instrument.ToString()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(QuoteKey other) { return Currency == other.Currency && Instrument == other.Instrument; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator CurrencyKey(QuoteKey value) { return value.Currency; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentKey(QuoteKey value) { return value.Instrument; }
    }
}
