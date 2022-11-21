namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Key;

    public struct TradeAccount : IEquatable<TradeAccount>, IEntityPoolSubject
    {
        private readonly int Number;

        public AccountKey Account { get => s_Account[Number]; }
        public int Place { get => s_Place[Number]; }

        public static int Create(AccountKey account, int place)
        {
            int i = EntityPool<TAP>.Next();
            s_Account[i] = account; s_Place[i] = place;

            var number = s_Trades.GetOrAdd(i, _ => i);

            if (number != i)
                EntityPool<TAP>.Free(i);
            return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => Account ^ Place;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => Equals((TradeAccount)obj);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(TradeAccount other) => Account == other.Account && Place == other.Place;

        public override string ToString() => string.Concat(Account.ToString(), " : ", Place.ToString());
        
        private static AccountKey[] s_Account;
        private static int[] s_Place;
        private static ConcurrentDictionary<TradeAccount, int> s_Trades;

        public static void Init(int size)
        {
            s_Account = new AccountKey[size];
            s_Place = new int[size];
            s_Trades = new ConcurrentDictionary<TradeAccount, int>(4, size);

            Empty = 0;

            EntityPool<TAP>.Reset();
        }

        public static TradeAccount Empty { get; private set; }
        public static int LastNumber { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => EntityPool<TAP>.LastNumber; }
        private TradeAccount(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Free() => EntityPool<TAP>.Free(Number);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(TradeAccount value) { return value.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator TradeAccount(int value) { return new TradeAccount(value); }

    }
}
