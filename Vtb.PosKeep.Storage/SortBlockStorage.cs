
namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    public class SortBlockStorage<DataType> where DataType : struct, IComparable<DataType>
    {
        public readonly int BlockSize;
        private volatile int m_blockCount; 
        public int BlockCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_blockCount; }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get
            {
                var count = BlockCount - 1;
                switch (count)
                {
                    case -1:
                        return 0;
                    case 0:
                        return m_blocks[0].Count;
                    default:
                        return count * BlockSize + m_blocks[count].Count;
                }
            }
        }
        public Func<IEnumerable<DataType>, IEnumerable<DataType>, IEnumerable<DataType>> MergeFunction { get; private set; }

        public SortBlockStorage(Func<IEnumerable<DataType>, IEnumerable<DataType>, IEnumerable<DataType>> mergeFunction, int blockSize = 100, int blockCount = 10)
        : this(blockSize, blockCount)
        {
            MergeFunction = mergeFunction;
        }

        public SortBlockStorage(int blockSize = 100, int blockCount = 10)
        {
            BlockSize = blockSize;
            m_blockCount = 0;
            m_blocks = new Block<DataType>[blockCount];
            MergeFunction = MergeUtils.Merge<DataType>;
        }

        private volatile Block<DataType>[] m_blocks;

        public IEnumerable<DataType> Items()
        {
            for (int i = 0; i < m_blocks.Length; i++)
            {
                if (m_blocks[i].Count > 0)
                {
                    foreach (var item in m_blocks[i].Items())
                        yield return item;
                }
            }
        }

        public IEnumerable<DataType> Items(int to)
        {
            for (int i = 0; i < m_blocks.Length && 0 < to; i++)
            {
                if (m_blocks[i].Count > 0)
                {
                    foreach (var item in m_blocks[i].Items(to))
                        yield return item;

                    to -= m_blocks[i].Count;
                }
            }
        }

        public IEnumerable<DataType> Items(int from, int to)
        {
            to -= from;
            var i = from / BlockSize;
            from = from % BlockSize;

            if (i < m_blocks.Length && 0 < to && m_blocks[i].Count > 0)
            {
                foreach (var item in m_blocks[i].Items(from % BlockSize, to))
                    yield return item;

                to -= m_blocks[i].Count;
                i++;

                for (; i < m_blocks.Length && 0 < to; i++)
                {
                    if (m_blocks[i].Count > 0)
                    {
                        foreach (var item in m_blocks[i].Items(to))
                            yield return item;

                        to -= m_blocks[i].Count;
                    }
                }
            }
        }

        public IEnumerable<DataType> ItemsFrom(int from)
        {
            var i = from / BlockSize;
            from = from % BlockSize;

            if (i < m_blocks.Length && m_blocks[i].Count > 0)
            {
                foreach (var item in m_blocks[i].ItemsFrom(from % BlockSize))
                    yield return item;

                i++;

                for (; i < m_blocks.Length; i++)
                {
                    if (m_blocks[i].Count > 0)
                    {
                        foreach (var item in m_blocks[i].Items())
                            yield return item;
                    }
                }
            }
        }

        public IEnumerable<DataType> Items(DataType to)
        {
            var search = new Block<DataType>(1); search.Add(to);
            var index = Array.BinarySearch(m_blocks, 0, BlockCount, search, comparer);
            if (index < 0)
                index = ~index;

            for (int i = 0; i < index && i < m_blocks.Length; i++)
            {
                if (m_blocks[i].Count > 0)
                {
                    foreach (var item in m_blocks[i].Items(to))
                        yield return item;
                }
            }
        }

        
        public DataType First
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (BlockCount > 0) ? m_blocks[0][0] : default(DataType);
            }
        }

        public DataType Last
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (BlockCount > 0) ? m_blocks[BlockCount-1].Last : default(DataType);
            }
        }

        public IEnumerable<DataType> Items(DataType from, DataType to, int prev = 0)
        {
            // найти первый блок
            var search = new Block<DataType>(1); search.Add(from);
            var index = Array.BinarySearch(m_blocks, 0, BlockCount, search, comparer);
            var i_from = Math.Max(0, (index < 0) ? (~index) - 1 : index);

            // найти конечный блок
            search[0] = to;
            index = Array.BinarySearch(m_blocks, i_from, BlockCount - i_from, search, comparer);
            var i_to = (index < 0) ? Math.Min(m_blocks.Length -1, ~index) : index;

            // выводим из блоков поочереди
            if (i_from < m_blocks.Length && i_to < m_blocks.Length && m_blocks[i_from].Count > 0)
            {
                foreach (var item in m_blocks[i_from].Items(from, to, prev))
                    yield return item;

                i_from++;

                for (; i_from < m_blocks.Length && i_from < i_to && m_blocks[i_from].Count > 0; i_from++)
                {
                    foreach (var item in m_blocks[i_from].Items(to))
                        yield return item;
                }
            }
        }

        public IEnumerable<DataType> ItemsFrom(DataType from, int prev = 0)
        {
            // найти первый блок
            var search = new Block<DataType>(1); search.Add(from);
            var index = Array.BinarySearch(m_blocks, 0, BlockCount, search, comparer);
            var i_from = Math.Max(0, (index < 0) ? (~index) - 1 : index);

            // выводим из блоков поочереди
            if (i_from < m_blocks.Length && m_blocks[i_from].Count > 0)
            {
                foreach (var item in m_blocks[i_from].ItemsFrom(from, prev))
                    yield return item;

                i_from++;

                for (; i_from < m_blocks.Length && m_blocks[i_from].Count > 0; i_from++)
                {
                    foreach (var item in m_blocks[i_from].Items())
                        yield return item;
                }
            }
        }

        public void CutItems(DataType from)
        {
            // найти первый блок
            var search = new Block<DataType>(1); search.Add(from);
            var index = Array.BinarySearch(m_blocks, 0, BlockCount, search, comparer);
            var i_from = Math.Max(0, (index < 0) ? (~index) - 1 : index);

            // выводим из блоков поочереди
            if (i_from < m_blocks.Length && m_blocks[i_from].Count > 0)
            {
                m_blocks[i_from].Count -= m_blocks[i_from].ItemsFrom(from).Count();
                i_from++;

                for (; i_from < m_blocks.Length && m_blocks[i_from].Count > 0; i_from++)
                {
                    m_blocks[i_from].Count = 0;
                }
            }
        }

        public void AddOrUpdate(IEnumerable<DataType> items)
        {
            void getBlocks(IEnumerator<DataType> enumerator, int from = 0)
            {
                bool @continue = true;
                for ( ; @continue; )
                {
                    var j = 0;
                    var new_block = new Block<DataType>(BlockSize);

                    new_block.AddOrUpdate(enumerator, ref j, ref @continue);
                    if (j > 0)
                        AddCheckSize(from++, new_block);
                }
            }

            using (var itemsEnumerator = items.GetEnumerator())
            {
                if (itemsEnumerator.MoveNext())
                {
                    if (BlockCount == 0)
                    {
                        using (var mergeEnumerator = MergeUtils.Merge(MergeFunction, Enumerable.Empty<DataType>(), itemsEnumerator.GetEnumerable()).GetEnumerator())
                        {
                            getBlocks(mergeEnumerator);
                        }
                    }
                    else
                    {
                        var search = new Block<DataType>(1); search.Add(itemsEnumerator.Current);
                        var index = Array.BinarySearch(m_blocks, 0, BlockCount, search, comparer);
                        var i_from = Math.Max(0, (index < 0) ? (~index) - 1 : index);

                        var enumerables = GetEnumerablesForMerge(i_from, itemsEnumerator.GetEnumerable());

                        using (var mergeEnumerator = MergeUtils.Merge(MergeFunction, enumerables).GetEnumerator())
                        {
                            getBlocks(mergeEnumerator, i_from);
                        }
                    }
                }
            }
        }

        private IEnumerable<DataType>[] GetEnumerablesForMerge(int from, IEnumerable<DataType> new_items)
        {
            var enumerables = new IEnumerable<DataType>[Math.Max(1, BlockCount) - from + 1];
            for (int i = 0; i < enumerables.Length - 1; i++, from++)
            {
                enumerables[i] = m_blocks[from].Items();
            }
            enumerables[enumerables.Length - 1] = new_items;

            return enumerables;
        }

        private void AddCheckSize(int index, Block<DataType> block)
        {
            if (index >= m_blocks.Length)
            {
                var newLength = m_blocks.Length + m_blocks.Length;
                var newBlocks = new Block<DataType>[newLength];

                m_blocks.CopyTo(newBlocks, 0);
                m_blocks = newBlocks;
            }
            m_blocks[index] = block;

            if (index >= m_blockCount)
                m_blockCount = index +1;
        }

        struct Block<T> where T : struct, IComparable<T>
        {
            private volatile T[] m_items;

            public Block(int size = 0)
            {
                Count = 0;
                m_items = new T[size];
            }

            public volatile int Count;

            public T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Count > index) ? m_items[index] : default(T); }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { m_items[index] = value; }
            }
            public T Last
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Count > 0) ? m_items[Count-1] : default(T); }
            }

            public IEnumerable<T> Items()
            {
                for (int i = 0; i < Count && i < m_items.Length; i++)
                    yield return m_items[i];
            }

            public IEnumerable<T> Items(int to)
            {
                for (int i = 0; i < to && i < Count && i < m_items.Length; i++)
                    yield return m_items[i];
            }

            public IEnumerable<T> Items(int from, int to)
            {
                for (int i = from; i < to && i < Count && i < m_items.Length; i++)
                    yield return m_items[i];
            }

            public IEnumerable<T> ItemsFrom(int from)
            {
                for (int i = from; i < Count && i < m_items.Length; i++)
                    yield return m_items[i];
            }

            public IEnumerable<T> Items(T to)
            {
                for (int i = 0; i < Count && i < m_items.Length && m_items[i].CompareTo(to) < 0; i++)
                    yield return m_items[i];
            }

            public IEnumerable<T> Items(T from, T to, int prev = 0)
            {
                var i = Array.BinarySearch<T>(m_items, 0, Count, from, comparer);
                if (i < 0)
                    i = Math.Max(0, (~i)+prev);
                for ( ; i < Count && i < m_items.Length && m_items[i].CompareTo(to) < 0; i++)
                    yield return m_items[i];
            }
            public IEnumerable<T> ItemsFrom(T from, int prev = 0)
            {
                var i = Array.BinarySearch<T>(m_items, 0, Count, from, comparer);
                if (i < 0)
                    i = Math.Max(0, (~i)+prev);

                for (; i < Count && i < m_items.Length; i++)
                    yield return m_items[i];
            }

            public void AddOrUpdate(IEnumerator<T> items, ref int i, ref bool @continue)
            {
                if (@continue = items.MoveNext())
                {
                    do
                    {
                        m_items[i++] = items.Current;
                    } while (i < m_items.Length && (@continue = items.MoveNext()));

                    Count = i;
                }
            }

            public bool Add(T item)
            {
                if (Count >= m_items.Length)
                    return false;

                m_items[Count++] = item;

                return true;
            }

            private static readonly Comparer<T> comparer = new Comparer<T>();
            private class Comparer<TT> : IComparer<TT> where TT : struct, IComparable<TT>
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public int Compare(TT x, TT y)
                {
                    return x.CompareTo(y);
                }
            }
        }

        private static readonly BlockComparer<DataType> comparer = new BlockComparer<DataType>();
        private class BlockComparer<T> : IComparer<Block<T>> where T : struct, IComparable<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(Block<T> x, Block<T> y)
            {
                return x[0].CompareTo(y[0]);
            }
        }
    }
}
