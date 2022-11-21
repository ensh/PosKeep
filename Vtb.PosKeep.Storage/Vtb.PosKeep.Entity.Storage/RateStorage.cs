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

    public class RateStorage : BaseStorage<CurrencyKey, HD<Rate, RR>> 
    {
        public RateStorage(IBlockStorageFactory<HD<Rate, RR>> blockStorageFactory, CurrencyKey currency) : base(blockStorageFactory)
        {
            BaseCurrency = currency;
        }

        private CurrencyKey m_baseCurrency;
        public CurrencyKey BaseCurrency
        {
            get => m_baseCurrency;
            set { if (!StorageKeys.Any()) m_baseCurrency = value; }
        }

        public IEnumerable<int> Years(CurrencyKey CurrencyKey)
        {
            var current = First(CurrencyKey).Timestamp.Date.Year;
            var last = Last(CurrencyKey).Timestamp.Date.Year;
            for (; current <= last; current++)
                yield return current;
        }

        public IEnumerable<HD<Rate, RR>> Items(CurrencyKey key, Timestamp from, Timestamp to, int period = 0)
        {
            if (key == BaseCurrency)
            {
                if (period == 0)
                    yield return new HD<Rate, RR>(1, Rate.One);
                else
                {
                    for(Timestamp t = from; t < to; t += period)
                        yield return new HD<Rate, RR>(t, Rate.One);
                }
            }
            else
            {
                var fromHistory = new HD<Rate, RR>(from);
                var toHistory = new HD<Rate, RR>(to);

                foreach (var rate in Items(key, fromHistory, toHistory, 0))
                    yield return rate;
            }
        }

        protected struct Bag<T> : ILockableReaderWriter
        {
            public T[] Data;
            public readonly rwLock Lock;

            public Bag(T[] data) : this(data, new rwLock())
            {
            }

            public Bag(T[] data, rwLock rwlock)
            {
                Data = data;
                Lock = rwlock;
            }

            #region IReadWriteStorage

            private class ReadLockerImpl : IDisposable
            {
                rwLock m_lock;

                public ReadLockerImpl(Bag<T> blockStorage)
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

                public WriteLockerImpl(Bag<T> blockStorage)
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
            public static implicit operator ReaderWriterLockSlim(Bag<T> blockStorage)
            {
                return blockStorage.Lock;
            }

            #endregion IReadWriteStorage
        }
    }
}
