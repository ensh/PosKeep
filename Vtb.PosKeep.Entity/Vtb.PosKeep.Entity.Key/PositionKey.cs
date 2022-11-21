namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class PositionKeyReference : HR { }

    // PR - PositionReference
    /// <summary>
    /// PositionReference
    /// </summary>
    public abstract class PR : HR { }

    public static class PositionKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PositionKey ToPositionKey(params int[] ids)
        {
            return new PositionKey(new TradeAccountKey(ids[0].ToAccountKey(), ids[1]), new TradeInstrumentKey(ids[2].ToCurrencyKey(), ids[3].ToInstrumentKey()));
        }
    }

    public struct PositionKey : IComplexKey<TradeAccountKey, TradeInstrumentKey, PositionKeyReference>, IEquatable<PositionKey>
    {
        public readonly TradeAccountKey Account;
        public readonly TradeInstrumentKey Instrument;

        public PositionKey(DealKey dealKey) : this(dealKey.Account, dealKey.Instrument) { }
        public PositionKey(TradeAccountKey client_id, TradeInstrumentKey instr_id) { Account = client_id; Instrument = instr_id; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return Account.GetHashCode() ^ Instrument.GetHashCode(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((PositionKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return string.Concat("Account: ", Account.ToString(), ", Instrument: ", Instrument.ToString()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionKey other) =>
            Account.GetHashCode() == other.Account.GetHashCode() && Instrument.GetHashCode() == other.Instrument.GetHashCode();

        public static PositionKey Create(params int[] ids)
        {
            return new PositionKey(new TradeAccountKey(TradeAccount.Create(ids[0], ids[1])), 
                new TradeInstrumentKey(TradeInstrument.Create(ids[3].ToInstrumentKey(), ids[2].ToCurrencyKey())));
        }

        public static PositionKey Create(TradeAccountKey trade_acc, TradeInstrumentKey instrument)
        {
            return new PositionKey(trade_acc, instrument);
        }
    }
}
