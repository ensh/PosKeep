namespace Vtb.PosKeep.Entity
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public abstract class PropertyStorage
    {
        public struct PropertyStorageItem
        {
            public readonly int DataIndex;

            public PropertyStorageItem(int dataIndex)
            {
                DataIndex = dataIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PropertyStorageItem(int dataIndex)
            {
                return new PropertyStorageItem(dataIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator int(PropertyStorageItem item)
            {
                return item.DataIndex;
            }
        }

        public static int Size = 1000000;
        protected volatile int Count;
        private readonly PropertyStorageItem[] items;

        public PropertyStorage() : this(Size) { }
        public PropertyStorage(int size)
        {
            items = new PropertyStorageItem[size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual PropertyStorageItem Add()
        {
            return Interlocked.Increment(ref Count);
        }

        public virtual IEnumerable<PropertyStorageItem> AddRange<T>(IEnumerable<T> dataRange)
        {
            using (var dataEnumerator = dataRange.GetEnumerator())
            {

                while (dataEnumerator.MoveNext())
                {
                    yield return Interlocked.Increment(ref Count);
                }
            }
        }
    }
}
