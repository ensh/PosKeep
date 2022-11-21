namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    public abstract class ClientKeyReference : HR { }

    public struct ClientKey : ISingleKey<int, ClientKeyReference>, IEquatable<ClientKey>
    {
        private int m_value;
        public ClientKey(int value) { m_value = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((ClientKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return s_registry.TryGetValue(m_value, out var value) ? value.ShortName ?? value.Name ?? m_value.ToString() : m_value.ToString(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(ClientKey other) { return m_value == other.m_value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator ClientKey(int value) { return new ClientKey(value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(ClientKey value) { return value.m_value; }

        public static void Register(ClientAccount account) { s_registry[account.ID] = account; }
        public static void Register(int accountID) { var id = accountID.ToString(); s_registry.TryAdd(accountID, new ClientAccount(accountID, id, id)); }
        private static ConcurrentDictionary<ClientKey, ClientAccount> s_registry = new ConcurrentDictionary<ClientKey, ClientAccount>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ClientAccount AsAccount(ClientKey id) { return s_registry[id]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ClientAccount AsAccount(int id) { return s_registry[id]; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IEnumerable<ClientAccount> Accounts() { return s_registry.Values; }
    }

    public static class ClientKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientKey ToClientKey(this int id)
        {
            return id;
        }
    }

    public class ClientAccount : IEquatable<ClientAccount>
    {
        public readonly ClientKey ID;
        public readonly string ShortName;
        public readonly string Name;

        public ClientAccount(int id, string shortName, string name)
        {
            ID = id; ShortName = shortName; Name = name;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return ID.GetHashCode(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((ClientAccount)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return ShortName; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(ClientAccount other) { return ID.Equals(other.ID); }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(ClientAccount x, ClientAccount y) { return x.Equals(y); }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(ClientAccount x, ClientAccount y) { return !x.Equals(y); }
    }
}
