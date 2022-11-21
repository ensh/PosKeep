namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class InstrumentPositionKeyReference : HR { }
    public static class InstrumentPositionKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InstrumentPositionKey ToPositionKey(params int[] ids)
        {
            return new InstrumentPositionKey(ids[0].ToCurrencyKey(), ids[1].ToInstrumentKey());
        }
    }

    public struct InstrumentPositionKey : IComplexKey<InstrumentKey, CurrencyKey, InstrumentPositionKeyReference>, IEquatable<InstrumentPositionKey>
    {
        public readonly CurrencyKey CurrencyID;
        public readonly InstrumentKey InstrumentID;

        public InstrumentPositionKey(CurrencyKey currency_id, InstrumentKey instrument_id) { CurrencyID = currency_id; InstrumentID = instrument_id; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return CurrencyID.GetHashCode() ^ InstrumentID.GetHashCode(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((InstrumentPositionKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return string.Concat("Instrument: ", InstrumentID.ToString(), ", Currency: ", CurrencyID.ToString()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(InstrumentPositionKey other) { return CurrencyID == other.CurrencyID && InstrumentID == other.InstrumentID; }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator (int value) { return new CurrencyKey(value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator CurrencyKey(InstrumentPositionKey value) { return value.CurrencyID; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentKey(InstrumentPositionKey value) { return value.InstrumentID; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(InstrumentPositionKey x, InstrumentPositionKey y) { return x.Equals(y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(InstrumentPositionKey x, InstrumentPositionKey y) { return !x.Equals(y); }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Currency AsCurrency(InstrumentPositionKey key) { return key.CurrencyID; }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Instrument AsInstrument(InstrumentPositionKey key) { return InstrumentKey.AsInstrument(key.InstrumentID); }
    }
}
