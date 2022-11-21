namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    public abstract class BoardKeyReference : HR { }

    public struct BoardKey : ISingleKey<int, BoardKeyReference>, IEquatable<BoardKey>
    {
        private int m_value;
        public BoardKey(int value) { m_value = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return m_value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((BoardKey)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return s_registry.TryGetValue(m_value, out var value) ? value.ShortName : m_value.ToString(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(BoardKey other) { return m_value == other.m_value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator BoardKey(int value) { return new BoardKey(value); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(BoardKey value) { return value.m_value; }

        public static void Register(Board Boards) { s_registry[Boards.ID] = Boards; }
        private static ConcurrentDictionary<BoardKey, Board> s_registry = new ConcurrentDictionary<BoardKey, Board>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Board AsBoards(int id) { return s_registry[id]; }
    }

    public class Board : IEquatable<Board>
    {
        public readonly BoardKey ID;
        public readonly string ShortName;
        public readonly string Name;

        public Board(int id, string shortName, string name)
        {
            ID = id; ShortName = shortName; Name = name;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return ID.GetHashCode(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((Board)obj); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() { return ShortName; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Board other) { return ID.Equals(other.ID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Board x, Board y) { return x.Equals(y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Board x, Board y) { return !x.Equals(y); }
    }
}
