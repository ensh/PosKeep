namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Key;

    // RPR - RecalcPositionReference
    /// <summary>
    /// RecalcPositionReference
    /// </summary>
    public abstract class RPR : HR { }

    public enum CostMethod { FIFO, LIFO, WA, MONEY };

    public struct RecalcState : IEquatable<RecalcState>, IComparable<RecalcState>
    {
        public readonly ValueToken Quantity;
        public readonly Timestamp Start;
        public readonly CostMethod Method;

        public RecalcState(ValueToken quantity, Timestamp start, CostMethod method = CostMethod.FIFO)
        {
            Quantity = quantity;
            Start = start;
            Method = method;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(RecalcState other)
        {
            return Start.CompareTo(other.Start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RecalcState other)
        {
            return Method == other.Method && Start.CompareTo(other.Start) == 0 && Quantity == other.Quantity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return Equals((RecalcState) obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Method.GetHashCode() ^ Start.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat("Start: ", Start.ToString(), ", Quantity: ", Quantity.ToString());
        }

        public static RecalcState Default(CostMethod method)
        {
            return new RecalcState(ValueToken.Null, Timestamp.MinValue, method);
        }
    }

    public struct RecalcDeal
    {
        public ValueToken Volume;
        public ValueToken Quantity;
        public decimal Comission;
        public readonly Timestamp From;
        public HD<Deal, DR> Index { get { return From; } }

        public RecalcDeal(HD<Deal, DR> deal) : this(deal.Data.Volume, deal.Data.Quantity, deal.Data.Comission, deal.Timestamp) { }

        public RecalcDeal(ValueToken volume, ValueToken quantity, decimal comission, Timestamp from)
        {
            Volume = volume; Quantity = quantity; Comission = comission; From = from;
        }

        public decimal Price
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Volume.Value / Quantity.Value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RecalcDeal(HD<Deal, DR> deal)
        {
            return new RecalcDeal(deal);
        }

        public override string ToString()
        {
            return string.Concat("Volume: ", Volume.ToString(), ", Quantity: ", Quantity.ToString(), ", Index: ", Index.Timestamp.ToString());
        }
    }

    public struct RecalcResult : IEquatable<RecalcResult>
    {
        public readonly RecalcState State;
        public readonly HD<Position.Recalc, HR> Recalc;

        public RecalcResult(RecalcState state, HD<Position.Recalc, HR> recalc)
        {
            State = state;
            Recalc = recalc;
        }

        public override int GetHashCode()
        {
            return State.GetHashCode() ^ Recalc.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals((RecalcResult) obj);
        }

        public bool Equals(RecalcResult other)
        {
            return State.Equals(other.State) && Recalc.Equals(other.Recalc);
        }

        public static RecalcResult Default(CostMethod method)
        {
            return new RecalcResult(RecalcState.Default(method), new HD<Position.Recalc, HR>(
                Timestamp.MinValue, Position.Recalc.Create()));
        }

    }

    public struct PositionAlgorithmContext
    {
        public readonly CostMethod Method;
        public readonly Func<Timestamp> From; 
        public readonly Func<RecalcResult> OnNextCurrency;
        public readonly Action<RecalcState> OnLast;

        public PositionAlgorithmContext(CostMethod method, Func<bool, RecalcResult> onNext, bool recalcAll, Func<Timestamp> from, Action<RecalcState> onLast)
            : this(method, () => onNext(recalcAll), from, onLast)
        {
        }

        public PositionAlgorithmContext(CostMethod method, Func<bool, RecalcResult> onNext)
            : this(method, onNext, true, null, null)
        {
        }

        public PositionAlgorithmContext(Func<bool, RecalcResult> onNext)
            : this(CostMethod.FIFO, onNext, true, null, null)
        {
        }

        public PositionAlgorithmContext(CostMethod method, Func<RecalcResult> onNext, Func<Timestamp> from, Action<RecalcState> onLast)
        {
            Method = method;
            From = from;
            OnNextCurrency = onNext;
            OnLast = onLast;
        }
    }
}
