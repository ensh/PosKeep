namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    static class ReaderWriterLockPool
    {
        static Stack<ReaderWriterLockSlim> _locks;

        static readonly int Size = 40;
        static ReaderWriterLockPool()
        {
            _locks = new Stack<ReaderWriterLockSlim>();
            lock (typeof(ReaderWriterLockPool))
                for (int i = 0; i < Size; i++) _locks.Push(new ReaderWriterLockSlim());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReaderWriterLockSlim NextLock()
        {
            lock (typeof(ReaderWriterLockPool))
                return (_locks.Count > 0) ? _locks.Pop() : new ReaderWriterLockSlim();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseLock(ReaderWriterLockSlim l)
        {
            lock (typeof(ReaderWriterLockPool))
                if (_locks.Count < Size)
                    _locks.Push(l);
        }
    }

    public class rwLock
    {
        ReaderWriterLockSlim m_rwlocker;
        int m_lcounter;

        public rwLock() { m_rwlocker = null; m_lcounter = 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddRef()
        {
            if (1 == Interlocked.Increment(ref m_lcounter))
            {
                // поток с первым инкрементом
                var newLocker = ReaderWriterLockPool.NextLock();
                Interlocked.Exchange(ref m_rwlocker, newLocker);
            }
            else
                // ожидаем поток, который сделал первый инкремент
                while (null == Interlocked.CompareExchange(ref m_rwlocker, null, null)) ;

            return m_lcounter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Release()
        {
            var oldLocker = m_rwlocker;
            if (0 == Interlocked.Decrement(ref m_lcounter))
            {
                Interlocked.CompareExchange(ref m_rwlocker, null, oldLocker);
                ReaderWriterLockPool.ReleaseLock(oldLocker);
            }
            return m_lcounter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReaderWriterLockSlim(rwLock l)
        {
            return l.m_rwlocker;
        }
    }

    interface ILockableReaderWriter
    {
        IDisposable ReadLocker();
        IDisposable WriteLocker();
    }
}
