namespace Vtb.PosKeep.Entity
{
    using System.Runtime.CompilerServices;

    public class LinkedPropertyStorage : PropertyStorage
    {
        public struct LinkedStorageItem
        {
            public readonly int DataIndex;

            public LinkedStorageItem(int dataIndex)
            {
                DataIndex = dataIndex;
            }
            public int this[LinkedPropertyStorage storage, int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return storage.items[index]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set { storage.items[index] = value; }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator LinkedStorageItem(int dataIndex)
            {
                return new LinkedStorageItem(dataIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator int(LinkedStorageItem item)
            {
                return item.DataIndex;
            }
        }

        private readonly LinkedStorageItem[] items;

        public LinkedPropertyStorage() : base() { }
        public LinkedPropertyStorage(int size) : base(size)
        {
            items = new LinkedStorageItem[size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual LinkedStorageItem Add(int prev)
        {
            var result = Add().DataIndex;
            items[result] = result;

            if (prev > 0)
                items[prev] = result;

            return result;
        }
    }
}
