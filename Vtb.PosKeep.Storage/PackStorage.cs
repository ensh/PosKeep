namespace Vtb.PosKeep.Entity
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public struct StorageItem<DataType>
    {
        public readonly int Index;
        public readonly DataType Value;

        public StorageItem(DataType value, int index)
        {
            Value = value; Index = index;
        }

        public static implicit operator DataType(StorageItem<DataType> item)
        {
            return item.Value;
        }

        public override string ToString()
        {
            return string.Concat("[", Index.ToString(), "]: ", Value.ToString());
        }
    }

    public class PackStorage<DataType> where DataType : struct
    {
        public struct PackStorageItem<T> where T : struct
        {
            public readonly T Data;

            public PackStorageItem(T data)
            {
                Data = data; Next = 0;
            }
            public int Next
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PackStorageItem<T>(T data)
            {
                return new PackStorageItem<T>(data);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T(PackStorageItem<T> item)
            {
                return item.Data;
            }

            public PackStorageItem<T> Update(T data)
            {
                return new PackStorageItem<T>(data) { Next = Next };
            }
        }

        public static int Size = 1000000;
        private readonly PackStorageItem<DataType>[] items;
        private volatile int Count;

        public PackStorage() : this(Size) { }
        public PackStorage(int size)
        {
            items = new PackStorageItem<DataType>[size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(DataType info, int prev = 0)
        {
            var result = Interlocked.Increment(ref Count);

            items[result] = info;

            if (prev != 0)
                items[prev].Next = result;

            return result;
        }

        public int AddRange(IEnumerable<DataType> dataRange, ref int prev)
        {
            var next = 0;
            using (var dataEnumerator = dataRange.GetEnumerator())
            {
                if (dataEnumerator.MoveNext())
                {
                    items[next = Interlocked.Increment(ref Count)] = dataEnumerator.Current;
                    if (prev != 0)
                    {
                        items[prev].Next = next;
                    }
                    else
                        prev = next;

                    prev = next;

                    while (dataEnumerator.MoveNext())
                    {
                        items[next = Interlocked.Increment(ref Count)] = dataEnumerator.Current;
                        prev = items[prev].Next = next;
                    }
                }
            }

            return next;
        }

        public IEnumerable<KeyValuePair<DataType, int>> AddRange(IEnumerable<DataType> dataRange, int prev)
        {
            var next = 0;
            using (var dataEnumerator = dataRange.GetEnumerator())
            {
                if (dataEnumerator.MoveNext())
                {
                    items[next = Interlocked.Increment(ref Count)] = dataEnumerator.Current;
                    if (prev != 0)
                    {
                        items[prev].Next = next;
                    }
                    else
                        prev = next;

                    yield return new KeyValuePair<DataType, int>(dataEnumerator.Current, prev = next);

                    while (dataEnumerator.MoveNext())
                    {
                        items[next = Interlocked.Increment(ref Count)] = dataEnumerator.Current;
                        yield return new KeyValuePair<DataType, int>(dataEnumerator.Current, prev = items[prev].Next = next);
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<DataType, int>> UpdateRange(IEnumerable<DataType> dataRange, int from)
        {
            using (var dataEnumerator = dataRange.GetEnumerator())
            {
                while (dataEnumerator.MoveNext())
                {
                    var newItem = items[from].Update(dataEnumerator.Current);
                    items[from] = newItem;
                    yield return new KeyValuePair<DataType, int>(dataEnumerator.Current, from);

                    if (0 == (from = newItem.Next))
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<StorageItem<DataType>> GetData(int start)
        {
            while (start != 0)
            {
                var item = items[start];
                yield return new StorageItem<DataType>(item, start);
                start = item.Next;
            }
        }

        public PackStorageItem<DataType> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return items[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value.Next == 0)
                    value.Next = items[index].Next;
                items[index] = value;
            }
        }
    }
}
