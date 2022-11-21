namespace Vtb.PosKeep.Entity.Data
{
    public static class Pair
    {
        public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new Pair<T1, T2>(item1, item2);
        }

        public static Pair<T1, T2> Create<T1, T2>(T1 item1)
        {
            return new Pair<T1, T2>(item1, default(T2));
        }

        public static Pair<T1, T2> Create<T1, T2>(T2 item2)
        {
            return new Pair<T1, T2>(default(T1), item2);
        }
    }

    public struct Pair<T1, T2>: System.IEquatable<Pair<T1, T2>>
    {
        public readonly T1 Item1;
        public readonly T2 Item2;

        public Pair(T1 item1, T2 item2)
        {
            Item1 = item1; Item2 = item2;
        }

        public static implicit operator T1(Pair<T1, T2> p)
        {
            return p.Item1;
        }

        public override bool Equals(object obj)
        {
            return Equals((Pair<T1, T2>)obj);
        }

        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode();
        }

        public bool Equals(Pair<T1, T2> other)
        {
            return Item1.Equals(other.Item1) && Item2.Equals(other.Item2);
        }
    }
}
