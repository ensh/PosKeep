namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class BlockedStorage<DataType> where DataType : struct
    {
        public readonly int BlockSize;
        public virtual int BlockCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
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

        public DataType this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (BlockCount > 0)
                {
                    var blockIndex = index / BlockSize;

                    return m_blocks[blockIndex][index % BlockSize];
                }

                return default(DataType);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                // корректно работает только обновление
                if (BlockCount > 0)
                {
                    var blockIndex = index / BlockSize;
                      
                    if (blockIndex < BlockCount)
                        m_blocks[blockIndex][index % BlockSize] = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<DataType> Items()
        {
            if (BlockCount > 0)
            {
                for (int i = 0; i < m_blocks.Length; i++)
                {
                    var block = m_blocks[i];
                    for (int j = 0; j < block.Count; j++)
                        yield return block[j];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<DataType> Items(int from, int to = int.MaxValue)
        {
            if (BlockCount > 0)
            {
                var blockIndex = from / BlockCount;
                var blockCount = Math.Min(BlockCount, 1 + (to - from) / BlockSize);

                for (int i = blockIndex; i < m_blocks.Length && i < blockCount; i++, to -= BlockSize)
                {
                    var block = m_blocks[i];
                    for (int j = 0; j < block.Count && j < to; j++)
                        yield return block[j];
                }
            }
        }        

        public BlockedStorage(int blockSize = 100, int blockCount = 10)
        {
            BlockSize = blockSize;
            m_blocks = new Block<DataType>[blockCount];

            m_blocks[0] = new Block<DataType>(BlockSize);
            BlockCount = 1;
        }

        public void AddOrUpdate(IEnumerable<DataType> items)
        {
            using (var itemsEnumerator = items.GetEnumerator())
            {
                if (itemsEnumerator.MoveNext())
                {
                    using (var forAddEnumerator = itemsEnumerator.GetEnumerable().GetEnumerator())
                    {
                        while (m_blocks[BlockCount-1].Append(forAddEnumerator))
                        {
                            CheckResize(++BlockCount);
                        }
                    }
                }
            }
        }

        private void CheckResize(int count)
        {
            var length = m_blocks.Length;
            if (count > length)
            {
                var newLength = m_blocks.Length + m_blocks.Length;
                var newBlocks = new Block<DataType>[newLength];

                m_blocks.CopyTo(newBlocks, 0);
                m_blocks = newBlocks;
            }

            if (length < m_blocks.Length)
            {
                for (int i = length; i < m_blocks.Length && i < count; i++)
                    m_blocks[i] = new Block<DataType>(BlockSize);                
            }
            else
                m_blocks[count - 1] = new Block<DataType>(BlockSize);
        }

        private Block<DataType>[] m_blocks;

        struct Block<T> where T : struct
        {
            private T[] m_items;
            private Func<T[]> CreateItemArray;

            public Block(int size = 0)
            {
                Count = 0;
                CreateItemArray = () => new T[size];
                m_items = null;
            }

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private set;
            }
            public T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return (Count > 0) ? m_items[index] : default(T); }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set { m_items[index] = value; }
            }
            public T Last
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return (Count > 0) ? m_items[Count - 1] : default(T); }
            }

            public bool Append(IEnumerator<T> items)
            {
                var i = Count;
                var next = true;

                if (m_items == null)
                {
                    m_items = CreateItemArray();
                    CreateItemArray = null;
                }

                if (i < m_items.Length)
                {
                    while (i < m_items.Length && (next = items.MoveNext()))
                    {
                        m_items[i++] = items.Current;
                    } 

                    Count = i;
                }

                return next;
            }
        }
    }
}
