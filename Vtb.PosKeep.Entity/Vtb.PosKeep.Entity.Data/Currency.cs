namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Key;

    public struct Currency : IEquatable<Currency>, IEntityPoolSubject
    {
        private readonly int Number;

        public CurrencyKey ID { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number; } }
        public string DCode { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_DCode[Number]; } }
        public string Code { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Code[Number]; } }
        public string Name { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Name[Number] ?? s_DCode[Number]; } }

        public Currency(string dcode) : this(dcode, default(string), default(string)) { }

        public Currency(string dcode, string code, string name)
        {
            Number = s_DCodeRegister.GetOrAdd(dcode, _ =>
            {
                var number = EntityPool<CP>.Next();
                s_DCode[number] = dcode; s_Code[number] = code; s_Name[number] = name;
                return number;
            });
        }

        private static string[] s_DCode;
        private static string[] s_Code;
        private static string[] s_Name;
        private static ConcurrentDictionary<string, int> s_DCodeRegister;

        public static void Init(int size)
        {
            s_Code = new string[size];
            s_DCode = new string[size];
            s_Name = new string[size];
            s_DCodeRegister = new ConcurrentDictionary<string, int>(4, size);
            s_DCodeRegister.TryAdd("0", Empty = 0);

            EntityPool<CP>.Reset();
        }

        public static Currency Empty { get; private set; }

        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EntityPool<CP>.LastNumber; }
        }

        private Currency(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(Currency currency) { return currency.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Currency(int currencyNumber) { return new Currency(currencyNumber); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator string(Currency currency) { return s_DCode[currency.Number]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Currency(string currencyDCode) { return s_DCodeRegister[currencyDCode]; }

        public void Free() { throw new NotSupportedException(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((Currency)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Currency other) { return Number == other.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Currency x, Currency y) { return x.Equals(y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Currency x, Currency y) { return !x.Equals(y); }

        public override string ToString()
        {
            return string.Concat("DCode: ", s_DCode[Number], ",Code: ", s_Code[Number], ", Name: ", s_Name[Number]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IEnumerable<Currency> Currencys() { return s_DCodeRegister.Values.Select(id => new Currency(id)); }
    }
}
