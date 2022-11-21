namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    
    using Vtb.PosKeep.Entity.Data;

    public abstract class AccountKeyReference : HR { }

    // AR - AccountReference
    /// <summary>
    /// AccountReference
    /// </summary>
    public abstract class AR : HR { }

    public struct AccountKey : ISingleKey<int, AccountKeyReference>, IEquatable<AccountKey>
    {
        private int m_value;
        public AccountKey(int value) { m_value = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((AccountKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return ((Account)m_value).ToString(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(AccountKey other) { return m_value == other.m_value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator AccountKey(int value) { return new AccountKey(value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator AccountKey(string value) { return ((Account)value).ID; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(AccountKey value) { return value.m_value; }
    }

    public static class AccountKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AccountKey ToAccountKey(this int id)
        {
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AccountKey ToAccountKey(this string code)
        {
            return code;
        }
    }
}
