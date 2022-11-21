namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class DealKeyReference : HR { }

    // DR - DealReference
    /// <summary>
    /// DealReference
    /// </summary>
    public abstract class DR : HR { }

    public struct DealKey : IComplexKey<TradeAccountKey, TradeInstrumentKey, DealKeyReference>, IEquatable<DealKey>
    {
        public readonly TradeAccountKey Account;
        public readonly TradeInstrumentKey Instrument;

        public DealKey(TradeAccountKey trade_acc, TradeInstrumentKey instr_id) { Account = trade_acc; Instrument = instr_id; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => Account.GetHashCode() ^ Account.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => Equals((DealKey)obj);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() =>
            string.Concat("Account: ", Account.ToString(), ", Instrument: ", Instrument.ToString());
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(DealKey other) => Account.GetHashCode() == other.Account.GetHashCode() 
                    && Instrument.GetHashCode() == other.Instrument.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator TradeAccountKey(DealKey value) { return value.Account; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator AccountKey(DealKey value) { return value.Account.Account; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentKey(DealKey value) { return value.Instrument.Instrument; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator CurrencyKey(DealKey value) { return value.Instrument.Currency; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Account AsAccount(DealKey key) { return (Account)key.Account.Account.GetHashCode(); }
    }
}
