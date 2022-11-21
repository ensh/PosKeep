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

    public interface IBlockStorageFactory<T> where T : struct, IComparable<T>
    {
        SortBlockStorage<T> Create();
        SortBlockStorage<T> Create(int blockSize, int blockCount);
    }

    public class BaseStorage<KeyType, DataType> where DataType : struct, IComparable<DataType> where KeyType : struct
    {
        private ConcurrentDictionary<KeyType, StorageBlock<DataType>> StorageBlocks;
        private IBlockStorageFactory<DataType> StorageFactory;

        public BaseStorage(IBlockStorageFactory<DataType> blockStorageFactory )
        {
            StorageFactory = blockStorageFactory;
            StorageBlocks = new ConcurrentDictionary<KeyType, StorageBlock<DataType>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void DoOnNewKey(KeyType key) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual IEnumerable<DataType> DoOnBeforeUpdate(KeyType key, IEnumerable<DataType> data) { return data; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddRange(KeyType key, IEnumerable<DataType> data)
        {
            if (!StorageBlocks.TryGetValue(key, out var storage))
            {
                if (StorageBlocks.TryAdd(key, storage = new StorageBlock<DataType>(StorageFactory.Create())))
                {
                    DoOnNewKey(key);
                }
                else
                    StorageBlocks.TryGetValue(key, out storage);
            }

            using (var l = storage.WriteLocker())
            {
                data = DoOnBeforeUpdate(key, data);
                storage.Data.AddOrUpdate(data);
            }
        }

        protected void CutItems(KeyType key, DataType from, Action<DataType> free)
        {
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                if (free is Action<DataType>)
                {
                    foreach (var item in storage.Data.ItemsFrom(from))
                        free(item);
                }
                storage.Data.CutItems(from);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddRange(KeyType key, ICollection<DataType> data, int ratio = 100)
        {
            if (!StorageBlocks.TryGetValue(key, out var storage))
            {
                if (StorageBlocks.TryAdd(key, storage = new StorageBlock<DataType>(
                    StorageFactory.Create(data.Count/ratio, ratio + ratio >> 1))))
                {
                    DoOnNewKey(key);
                }
                else
                    StorageBlocks.TryGetValue(key, out storage);
            }

            using (var l = storage.WriteLocker())
            {
                storage.Data.AddOrUpdate(data);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Add(KeyType key, params DataType[] data)
        {
            AddRange(key, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<DataType> Items(KeyType key, int start = 0)
        {
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                using (var l = storage.ReadLocker())
                {
                    if (start == 0)
                    {
                        foreach (var item in storage.Data.Items())
                            yield return item;
                    }
                    else
                    {
                        foreach (var item in storage.Data.ItemsFrom(start))
                            yield return item;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<DataType> Items(KeyType key, DataType start, int prev = 0)
        {
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                using (var l = storage.ReadLocker())
                {
                    foreach (var item in storage.Data.ItemsFrom(start, prev))
                        yield return item;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<DataType> Items(KeyType key, DataType from, DataType to, int prev = 0)
        {
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                using (var l = storage.ReadLocker())
                {
                    foreach (var item in storage.Data.Items(from, to, prev))
                        yield return item;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WithRange(KeyType key, Action<Pair<DataType, DataType>> action)
        {
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                using (var l = storage.ReadLocker())
                {
                    action(new Pair<DataType, DataType>(storage.Data.First, storage.Data.Last));
                }
            }
        }

        public DataType First(KeyType key) 
        {            
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                using (var l = storage.ReadLocker())
                {
                    return storage.Data.Items().FirstOrDefault();
                }
            }

            return default(DataType);
        }

        public DataType Last(KeyType key) 
        {            
            if (StorageBlocks.TryGetValue(key, out var storage))
            {
                using (var l = storage.ReadLocker())
                {
                    return storage.Data.Last;
                }
            }

            return default(DataType);
        }

        public IEnumerable<KeyType> StorageKeys
        {
            get => StorageBlocks.Select(sb => sb.Key);
        }

        private struct StorageBlock<T> : ILockableReaderWriter where T : struct, IComparable<T>
        {
            public readonly SortBlockStorage<T> Data;
            public readonly rwLock Lock;

            public StorageBlock(SortBlockStorage<T> data) : this(data, new rwLock()) { }

            public StorageBlock(SortBlockStorage<T> data, rwLock rwlock)
            {
                Data = data; Lock = rwlock;
            }

            #region IReadWriteStorage
            private class ReadLockerImpl : IDisposable
            {
                volatile rwLock m_lock;
                public ReadLockerImpl(StorageBlock<T> blockStorage)
                {
                    (m_lock = blockStorage.Lock).AddRef();
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
                volatile rwLock m_lock;
                public WriteLockerImpl(StorageBlock<T> blockStorage)
                {
                    (m_lock = blockStorage.Lock).AddRef();
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator ReaderWriterLockSlim(StorageBlock<T> blockStorage) { return blockStorage.Lock; }

            #endregion IReadWriteStorage
        }
    }
}
