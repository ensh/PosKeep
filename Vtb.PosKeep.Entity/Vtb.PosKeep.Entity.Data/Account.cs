namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Key;

    public struct Account : IEquatable<Account>
    {
        private readonly int Number;

        public AccountKey ID { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number; } }
        public string Code { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Code[Number]; } }
        public string Name { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Name[Number] ?? s_Code[Number]; } }

        public Account(string code): this (code, default(string)) { }

        public Account(string code, string name)
        {
            Number = s_CodeRegister.GetOrAdd(code, _ => 
            {
                var number = EntityPool<AP>.Next();
                s_Code[number] = code; s_Name[number] = name;
                return number;
            });
        }

        private static string[] s_Code;
        private static string[] s_Name;
        private static ConcurrentDictionary<string, int> s_CodeRegister;

        public static void Init(int size)
        {
            s_Code = new string[size];
            s_Name = new string[size];
            s_CodeRegister = new ConcurrentDictionary<string, int>(4, size);

            EntityPool<AP>.Reset();
        }
        
        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EntityPool<AP>.LastNumber; }
        }
        private Account(int number)
        {
            Number = number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(Account account)
        {
            return account.Number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Account(int accountNumber)
        {
            return new Account(accountNumber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator string(Account account)
        {
            return s_Code[account.Number];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Account(string accountCode)
        {
            return new Account(accountCode);
        }

        public override string ToString()
        {
            return string.Concat("Code: ", s_Code[Number], ", Name: ", s_Name[Number]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((Account)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Account other) { return Number == other.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Account x, Account y) { return x.Equals(y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Account x, Account y) { return !x.Equals(y); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IEnumerable<Account> Accounts() { return s_CodeRegister.Select(a => new Account(a.Value)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int Count() { return s_CodeRegister.Count(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int AccountIdByCode(string code ) { return (s_CodeRegister.TryGetValue(code, out var a))? a : 0; }
    }
}
