namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public interface IEntityPoolSubject
    {
        void Free();
    }

    //EP - EntityPool
    /// <summary>
    /// EntityPool
    /// </summary>
    public abstract class EP { }

    //AP - AccountPool
    /// <summary>
    /// AccountPool
    /// </summary>
    public abstract class AP : EP { }

    //TAP - TradeAccountPool
    /// <summary>
    /// TradeAccountPool
    /// </summary>
    public abstract class TAP : EP { }

    //DP - DealPool
    /// <summary>
    /// DealPool
    /// </summary>
    public abstract class DP : EP { }

    //DKP - DealKeyPool
    /// <summary>
    /// DealKeyPool
    /// </summary>
    public abstract class DKP : EP { }

    //QP - QuotePool
    /// <summary>
    /// QuotePool
    /// </summary>
    public abstract class QP : EP { }

    //IP - InstrumentPool
    /// <summary>
    /// InstrumentPool
    /// </summary>
    public abstract class IP : EP { }

    //TIP - TradeInstrumentPool
    /// <summary>
    /// TradeInstrumentPool
    /// </summary>
    public abstract class TIP : EP { }

    //FP - FlowPool
    /// <summary>
    /// FlowPool
    /// </summary>
    public abstract class FP : EP { }

    //СP - СurrencyPool
    /// <summary>
    /// CurrencyPool
    /// </summary>
    public abstract class CP : EP { }

    //PP - PositionPool
    /// <summary>
    /// PositionPool
    /// </summary>
    public abstract class PP : EP { }

    //RP - RecalcPool
    /// <summary>
    /// RecalcPool
    /// </summary>
    public abstract class ReP : EP { }

    //RtP - RatePool
    /// <summary>
    /// RatePool
    /// </summary>
    public abstract class RtP : EP { }

    //CtP - ContextPool
    /// <summary>
    /// ContextPool
    /// </summary>
    public abstract class CtP : EP { }

    public class EntityPool<T> where T : EP
    {
        static EntityPool() { Numbers = new ConcurrentStack<int>(); }
        private static volatile int CurrentNumber;
        private static ConcurrentStack<int> Numbers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next()
        {
            if (Numbers.TryPop(out var result))
                return result;
            else
                return Interlocked.Increment(ref CurrentNumber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next(int n)
        {
            return Interlocked.Add(ref CurrentNumber, n);
        }

        public static void Reset(int start = 0)
        {
            Numbers.Clear();
            Interlocked.Exchange(ref CurrentNumber, start);
        }

        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Interlocked.CompareExchange(ref CurrentNumber, 0, 0); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(int number)
        {
            Numbers.Push(number);
        }
    }
}
