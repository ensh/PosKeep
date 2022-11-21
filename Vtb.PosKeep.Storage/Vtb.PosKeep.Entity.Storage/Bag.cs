namespace Vtb.PosKeep.Entity.Storage
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public struct StorageBag<T> : ILockableReaderWriter
    {
        public readonly T[] Data;
        public readonly rwLock Lock;

        public StorageBag(T[] data) : this(data, new rwLock())
        {
        }

        public StorageBag(T[] data, rwLock rwlock)
        {
            Data = data;
            Lock = rwlock;
        }

        #region IReadWriteStorage

        private class ReadLockerImpl : IDisposable
        {
            rwLock m_lock;

            public ReadLockerImpl(StorageBag<T> blockStorage)
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
            rwLock m_lock;

            public WriteLockerImpl(StorageBag<T> blockStorage)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable ReadLocker()
        {
            return new ReadLockerImpl(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable WriteLocker()
        {
            return new WriteLockerImpl(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRef()
        {
            Lock.AddRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Lock.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReaderWriterLockSlim(StorageBag<T> blockStorage)
        {
            return blockStorage.Lock;
        }

        #endregion IReadWriteStorage
    }
}
