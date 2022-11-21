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

    public class QuoteStorage : HistoryStorage<QuoteKey, Quote, QR>
    {
        public QuoteStorage(IBlockStorageFactory<HD<Quote, QR>> blockStorageFactory) : base(blockStorageFactory)
        {
            InstrumentCurrencies = new ConcurrentDictionary<InstrumentKey, Bag<CurrencyKey>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void DoOnNewKey(QuoteKey key)
        {
            InstrumentCurrencies.AddOrUpdate(key.Instrument, _ => new Bag<CurrencyKey>(new [] {key.Currency }),
            (k, currencies) =>
            {
                using (var l = currencies.WriteLocker())
                {
                    if (!currencies.Data.Contains(key.Currency))
                    {
                        var data = new CurrencyKey[currencies.Data.Length + 1];
                        Array.Copy(currencies.Data, data, currencies.Data.Length);
                        data[currencies.Data.Length] = key.Currency;
                        return new Bag<CurrencyKey>(data, currencies.Lock);
                    }

                    return currencies;
                }
            });
        }

        protected ConcurrentDictionary<InstrumentKey, Bag<CurrencyKey>> InstrumentCurrencies;
        
        private static readonly CurrencyKey[] emptyCurrencies = new CurrencyKey[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<CurrencyKey> Currencies(InstrumentKey instrument)
        {
            if (InstrumentCurrencies.TryGetValue(instrument, out var currencies))
            {
                using (var l = currencies.ReadLocker())
                {
                    foreach(var i in currencies.Data)
                        yield return i;
                }
            }
        }

        public IEnumerable<int> Years (QuoteKey quoteKey)
        {
            var current = First(quoteKey).Timestamp.Date.Year;
            var last = Last(quoteKey).Timestamp.Date.Year;
            for ( ; current <= last; current++)
                yield return current;
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
                    ((ReaderWriterLockSlim) m_lock).EnterReadLock();
                }

                public void Dispose()
                {
                    if (m_lock != null)
                    {
                        ((ReaderWriterLockSlim) m_lock).ExitReadLock();
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
                    ((ReaderWriterLockSlim) m_lock).EnterWriteLock();
                }

                public void Dispose()
                {
                    if (m_lock != null)
                    {
                        ((ReaderWriterLockSlim) m_lock).ExitWriteLock();
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
