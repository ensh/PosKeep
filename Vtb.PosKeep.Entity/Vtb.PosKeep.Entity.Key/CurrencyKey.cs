namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class CurrencyKeyReference : HR { }

    public struct CurrencyKey : ISingleKey<int, CurrencyKeyReference>, IEquatable<CurrencyKey>
    {
        private short m_value;
        public CurrencyKey(short value) { m_value = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((CurrencyKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return ((Currency)m_value).ToString(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(CurrencyKey other) { return m_value == other.m_value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator CurrencyKey(int value) { return (value == 0) ? Empty : new CurrencyKey((short)value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator CurrencyKey(string value) { return ((Currency)value).ID; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(CurrencyKey value) { return value.m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Currency(CurrencyKey value) { return value.m_value; }

        public static readonly CurrencyKey Empty = 0; 
    }

    public static class CurrencyKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CurrencyKey ToCurrencyKey(this int id)
        {
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CurrencyKey ToCurrencyKey(this string dcode)
        {
            return dcode;
        }
    }
}
