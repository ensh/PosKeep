namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    
    using Vtb.PosKeep.Entity.Data;

    public abstract class TradeAccountKeyReference : HR { }

    // TAR - TradeAccountReference
    /// <summary>
    /// TradeAccountReference
    /// </summary>
    public abstract class TAR : HR { }

    public struct TradeAccountKey : ISingleKey<int, TradeAccountKeyReference>, IEquatable<TradeAccountKey>
    {
        private readonly int m_value;
        public TradeAccountKey(int value) { m_value = value; }
        public TradeAccountKey(AccountKey account, int place) : this(TradeAccount.Create(account, place))
        { }
        public TradeAccountKey(int place, AccountKey account) : this(TradeAccount.Create(account, place))
        { }
        public override string ToString() => ((TradeAccount)m_value).ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => Equals((TradeAccountKey)obj);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(TradeAccountKey other) => m_value == other.m_value;

        public AccountKey Account { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ((TradeAccount)m_value).Account; }
        public int Place { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ((TradeAccount)m_value).Place; }
    }
}
