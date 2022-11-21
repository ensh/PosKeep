namespace Vtb.PosKeep.Entity.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    public class BasePackStorage<KeyType, DataType> where DataType : struct where KeyType : struct
    {
        protected PackStorage<DataType> Storage;
        private ConcurrentDictionary<KeyType, Range> KeyRanges;

        public BasePackStorage() : this(PackStorage<DataType>.Size) { }

        public BasePackStorage(int size)
        {
            Storage = new PackStorage<DataType>(size);
            KeyRanges = new ConcurrentDictionary<KeyType, Range>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void DoOnNewKey(KeyType key) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddRange(KeyType key, IEnumerable<DataType> data)
        {
            var last = AddRangeEnumerable(key, data).LastOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IEnumerable<KeyValuePair<DataType, int>> AddRangeEnumerable(KeyType key, IEnumerable<DataType> data)
        {
            if (!KeyRanges.TryGetValue(key, out var range))
            {
                if (KeyRanges.TryAdd(key, new Range(0)))
                {
                    DoOnNewKey(key);
                }
            }

            range = KeyRanges[key];
            using (var l = range.WriteLocker())
            {
                range = KeyRanges[key];
                var start = range.Start;
                var next = 0;

                using (var insertValuesEnumerator = Storage.AddRange(data, range.Finish).GetEnumerator())
                {
                    if (insertValuesEnumerator.MoveNext())
                    {
                        if (range.Start == 0)
                            start = insertValuesEnumerator.Current.Value;

                        next = insertValuesEnumerator.Current.Value;
                        yield return insertValuesEnumerator.Current;

                        while (insertValuesEnumerator.MoveNext())
                        {
                            next = insertValuesEnumerator.Current.Value;
                            yield return insertValuesEnumerator.Current;
                        }
                    }
                }
                KeyRanges.TryUpdate(key, new Range(start, next, range.Lock), range);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Add(KeyType key, DataType data)
        {
            AddInternal(key, data);
        }

        protected int AddInternal(KeyType key, DataType data)
        {
            if (!KeyRanges.TryGetValue(key, out var range))
            {
                if (KeyRanges.TryAdd(key, new Range(0)))
                {
                    DoOnNewKey(key);
                }
            }

            range = KeyRanges[key];
            using (var l = range.WriteLocker())
            {
                range = KeyRanges[key];
                var next = Storage.Add(data, range.Finish);
                KeyRanges.TryUpdate(key, new Range((range.Start == 0) ? next : range.Start, next, range.Lock), range);

                return next;
            }
        }
        public IEnumerable<StorageItem<DataType>> Items(KeyType key, int start = 0)
        {
            if (KeyRanges.TryGetValue(key, out var range))
            {
                start = Math.Min(start, range.Finish);
                using (var l = range.ReadLocker())
                {
                    using (var data = Storage.GetData(Math.Max(range.Start, start)).GetEnumerator())
                    {
                        while (data.MoveNext())
                            yield return data.Current;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pair<int, int> ItemsRange(KeyType key)
        {
            if (KeyRanges.TryGetValue(key, out var range))
            {
                using (var l = range.ReadLocker())
                {
                    return new Pair<int, int>(range.Start, range.Finish);
                }
            }
            return default(Pair<int, int>);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WithRange(KeyType key, Action<Pair<int, int>> action)
        {
            if (KeyRanges.TryGetValue(key, out var range))
            {
                using (var l = range.ReadLocker())
                {
                    action(new Pair<int, int>(range.Start, range.Finish));
                }
            }
        }

        private struct Range : IEquatable<Range>, ILockableReaderWriter
        {
            public readonly int Start;
            public readonly int Finish;
            public readonly rwLock Lock;

            public Range(int start = 0) : this(start, start) { }

            public Range(int start, int finish) : this(start, finish, new rwLock()) { }

            public Range(int start, int finish, rwLock rwlock)
            {
                Start = start; Finish = finish; Lock = rwlock;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Range other)
            {
                return Start == other.Start && Finish == other.Finish;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object obj)
            {
                return Equals((Range)obj);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return Start ^ Finish;
            }

            public override string ToString()
            {
                return string.Concat(" Start: " + Start.ToString(), ",\t Finish: ", Finish.ToString());
            }

            #region IReadWriteStorage
            private class ReadLockerImpl : IDisposable
            {
                rwLock m_lock;
                public ReadLockerImpl(Range range)
                {
                    (m_lock = range.Lock).AddRef();
                    ((ReaderWriterLockSlim)m_lock).EnterReadLock();
                }

                public void Dispose()
                {
                    if (m_lock != null)
                    {
                        ((ReaderWriterLockSlim)m_lock).ExitReadLock();
                        m_lock.Release();
                        m_lock = null;
                        GC.SuppressFinalize(this);
                    }
                }
            }

            private class WriteLockerImpl : IDisposable
            {
                rwLock m_lock;
                public WriteLockerImpl(Range range)
                {
                    (m_lock = range.Lock).AddRef();
                    ((ReaderWriterLockSlim)m_lock).EnterWriteLock();
                }

                public void Dispose()
                {
                    if (m_lock != null)
                    {
                        ((ReaderWriterLockSlim)m_lock).ExitWriteLock();
                        m_lock.Release();
                        m_lock = null;
                        GC.SuppressFinalize(this);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public IDisposable ReadLocker() { return new ReadLockerImpl(this); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] public IDisposable WriteLocker() { return new WriteLockerImpl(this); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddRef() { Lock.AddRef(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Release() { Lock.Release(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator ReaderWriterLockSlim(Range range) { return range.Lock; }

            #endregion IReadWriteStorage
        }
    }
}
