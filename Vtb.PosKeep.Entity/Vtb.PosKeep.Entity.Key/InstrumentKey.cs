namespace Vtb.PosKeep.Entity.Key
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public abstract class InstrumentKeyReference : HR { }
    public struct InstrumentKey : ISingleKey<int, InstrumentKeyReference>, IEquatable<InstrumentKey>
    {
        private int m_value;
        public InstrumentKey(int value) { m_value = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((InstrumentKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { var value = (Instrument)m_value; return value.Name ?? value.Code ?? m_value.ToString(); } 
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(InstrumentKey other) { return m_value == other.m_value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentKey(int value) { return (value == 0) ? Empty : new InstrumentKey(value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(InstrumentKey value) { return value.m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator InstrumentKey(InstrumentCode value) { return ((Instrument)value).ID; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Instrument (InstrumentKey value) { return value.m_value; }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Instrument(int id) { return s_registry[id]; }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static IEnumerable<Instrument> Instruments() { return s_registry.Select(i => i.Value); }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int Count() { return s_registry.Values.Count(); }

        public static readonly InstrumentKey Empty = 0;
    }

    public static class InstrumentKeyUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InstrumentKey ToInstrumentKey(this int id)
        {
            return id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMoney(this Instrument instrument)
        {
            return instrument.ID == Instrument.Money.ID;
        }
    }
}
