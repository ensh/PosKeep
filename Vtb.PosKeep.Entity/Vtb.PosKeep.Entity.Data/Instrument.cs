namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Key;

    public struct InstrumentCode : IEquatable<InstrumentCode>
    {
        public readonly int id;
        public InstrumentCode(int id) { this.id = id; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((InstrumentCode)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return id; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(InstrumentCode other) { return id == other.id; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return id.ToString(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(InstrumentCode value) { return value.id; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentCode(int value) { return new InstrumentCode(value); }
    }

    public struct Instrument : IEquatable<Instrument>, IEntityPoolSubject
    {
        private readonly int Number;

        public InstrumentKey ID { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number; } }
        public InstrumentCode IdCode { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_IdCode[Number]; } }
        public string Code { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Code[Number]; } }
        public string Name { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Name[Number]; } }
        public bool IsNull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number == 0; } }

        public Instrument(InstrumentCode id) : this(id, default(string), default(string)) { }

        public Instrument(InstrumentCode id, string code, string name)
        {
            Number = s_IdRegister.GetOrAdd(id, _ =>
            {
                var number = EntityPool<IP>.Next();
                s_IdCode[number] = id;  s_Code[number] = code; s_Name[number] = name;
                return number;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return ID.GetHashCode(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((Instrument)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return string.Concat("Id: ", s_IdCode[Number].ToString(), "Code: ", s_Code[Number]); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Instrument other) { return Number == other.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Instrument x, Instrument y) { return x.Equals(y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Instrument x, Instrument y) { return !x.Equals(y); }

        private static InstrumentCode[] s_IdCode;
        private static string[] s_Code;
        private static string[] s_Name;
        private static ConcurrentDictionary<InstrumentCode, int> s_IdRegister;

        public static void Init(int size)
        {
            s_IdCode = new InstrumentCode[size];
            s_Code = new string[size];
            s_Name = new string[size];
            s_IdRegister = new ConcurrentDictionary<InstrumentCode, int>(4, size);

            Empty = 0;

            EntityPool<IP>.Reset();

            Money = new Instrument(-1, "MONEY", "ДЕНЕЖНЫЕ СРЕДСТВА");
        }

        public static Instrument Empty { get; private set; }
        public static Instrument Money { get; private set; }

        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EntityPool<IP>.LastNumber; }
        }

        private Instrument(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(Instrument value) { return value.Number; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Instrument(int value) { return new Instrument(value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentCode(Instrument value) { return s_IdCode[value.Number]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Instrument(InstrumentCode value) { return s_IdRegister[value]; }

        public void Free() { throw new NotSupportedException(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static IEnumerable<Instrument> Instruments() { return s_IdRegister.Values.Select(id => new Instrument(id)); }
    }
}
