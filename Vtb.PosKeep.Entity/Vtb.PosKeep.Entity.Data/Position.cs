namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Key;

    public struct Position : IEntityPoolSubject
    {
        private readonly int Number;
        public ValueToken Cost { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Cost[Number]; } }
        public decimal VolumeBuy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_VolumeBuy[Number]; } }
        public decimal VolumeSell { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_VolumeSell[Number]; } }
        public ValueToken Profit { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Profit[Number]; } }
        public ValueToken Quantity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Quantity[Number]; } }
        public decimal QuantityBuy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_QuantityBuy[Number]; } }
        public decimal QuantitySell { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_QuantitySell[Number]; } }
        public decimal Comission { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Comission[Number]; } }
        public bool IsNull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number == 0; } }

        public static int Create(ValueToken cost, ValueToken profit, ValueToken quantity)
        {
            int i = EntityPool<PP>.Next();
            s_Profit[i] = profit; s_Cost[i] = cost; s_Quantity[i] = quantity; 

            return i;
        }

        private static int Create(Recalc recalc)
        {
            int i = EntityPool<PP>.Next(), j = recalc;
            s_Profit[i] = s_Profit[j]; s_Cost[i] = s_Cost[j]; s_Quantity[i] = s_Quantity[j];
            s_QuantityBuy[i] = s_QuantityBuy[j]; s_QuantitySell[i] = s_QuantitySell[j]; s_Comission[i] = s_Comission[j];
            s_VolumeBuy[i] = s_VolumeBuy[j]; s_VolumeSell[i] = s_VolumeSell[j];

            return i;
        }

        public static int Create(ValueToken cost, ValueToken quantity)
        {
            return Create(cost, ValueToken.Null, quantity);
        }

        public static int Create(object position)
        {
            return PropertyStorage<PP>.Create(EntityPool<PP>.Next(), position);
        }

        private static int s_MaxRecalcPool;
        private static ValueToken[] s_Cost;

        private static decimal[] s_VolumeBuy;
        private static decimal[] s_VolumeSell;

        private static ValueToken[] s_Profit;
        private static ValueToken[] s_Quantity;

        private static decimal[] s_QuantityBuy;
        private static decimal[] s_QuantitySell;

        private static decimal[] s_Comission;
       
        public static Position Empty { get; private set; }
        public static void Init(int size, int rsize = 100)
        {
            s_Cost = new ValueToken[size];

            s_VolumeBuy = new decimal[size];
            s_VolumeSell = new decimal[size];

            s_Profit = new ValueToken[size];
            s_Quantity = new ValueToken[size];

            s_QuantityBuy = new decimal[size];
            s_QuantitySell = new decimal[size];
            s_Comission = new decimal[size];

            s_MaxRecalcPool = rsize;
            EntityPool<PP>.Reset();
            EntityPool<ReP>.Reset();

            Empty = new Position(EntityPool<PP>.Next(s_MaxRecalcPool));

            var storages = new[] { "s_Profit", "s_Cost", "s_VolumeBuy", "s_VolumeSell", "s_Quantity", "s_QuantityBuy", "s_QuantitySell", "s_Comission", };

            PropertyStorage<PP>.Init(typeof(Position), storages);
            PropertyStorage<ReP>.Init(typeof(Position), storages);
        }

        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EntityPool<PP>.LastNumber - s_MaxRecalcPool; }
        }

        private Position(int number) { Number = number; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() { EntityPool<PP>.Free(Number); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Position position) { return position.Number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Position(int positionNumber) { return new Position(positionNumber); }

        public override string ToString()
        {
            return string.Concat(
                "Cost: ", s_Cost[Number].ToString(), 
                ", Profit: ", s_Profit[Number].ToString(), 
                ", Quantity: ", s_Quantity[Number].ToString(), 
                " ");
        }

        public struct Recalc
        {
            private readonly int Number;
            public ValueToken Cost { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Cost[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_Cost[Number] = value; }
            }
            public decimal VolumeBuy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_VolumeBuy[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_VolumeBuy[Number] = value; }
            }
            public decimal VolumeSell { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_VolumeSell[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_VolumeSell[Number] = value; }
            }
            public ValueToken Profit { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Profit[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_Profit[Number] = value; }
            }
            public ValueToken Quantity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Quantity[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_Quantity[Number] = value; }
            }
            public decimal QuantityBuy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_QuantityBuy[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_QuantityBuy[Number] = value; }
            }
            public decimal QuantitySell { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_QuantitySell[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_QuantitySell[Number] = value; }
            }
            public decimal Comission { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return s_Comission[Number]; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)] set { s_Comission[Number] = value; }
            }
            public bool IsNull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number == 0; } }

            public static int Create(object position)
            {
                var i = Next();

                s_Profit[i] = s_Cost[i] = s_Quantity[i] = ValueToken.Null;
                s_QuantityBuy[i] = s_QuantitySell[i] = s_Comission[i] = s_VolumeBuy[i] = s_VolumeSell[i] = 0;

                return PropertyStorage<ReP>.Create(i, position);
            }

            public static int Create()
            {
                return Create(ValueToken.Null, ValueToken.Null, ValueToken.Null);
            }

            public static int Create(ValueToken cost, ValueToken quantity)
            {
                return Create(cost, ValueToken.Null, quantity);
            }

            public static int Create(ValueToken cost, ValueToken profit, ValueToken quantity)
            {
                var i = Next();
                s_Profit[i] = profit; s_Cost[i] = cost; s_Quantity[i] = quantity;
                s_QuantityBuy[i] = s_QuantitySell[i] = s_Comission[i] = s_VolumeBuy[i] = s_VolumeSell[i] = 0;

                return i;
            }

            private static int Create(Position position)
            {
                int i = Next(), j = position;
                s_Profit[i] = s_Profit[j]; s_Cost[i] = s_Cost[j]; s_Quantity[i] = s_Quantity[j];
                s_QuantityBuy[i] = s_QuantityBuy[j]; s_QuantitySell[i] = s_QuantitySell[j]; s_Comission[i] = s_Comission[j];
                s_VolumeBuy[i] = s_VolumeBuy[j]; s_VolumeSell[i] = s_VolumeSell[j];

                return i;
            }

            private static int Create(Recalc recalc)
            {
                int i = Next(), j = recalc;
                s_Profit[i] = s_Profit[j]; s_Cost[i] = s_Cost[j]; s_Quantity[i] = s_Quantity[j];
                s_QuantityBuy[i] = s_QuantityBuy[j]; s_QuantitySell[i] = s_QuantitySell[j]; s_Comission[i] = s_Comission[j];
                s_VolumeBuy[i] = s_VolumeBuy[j]; s_VolumeSell[i] = s_VolumeSell[j];

                return i;
            }

            public void ResetAggregate()
            {
                QuantityBuy = 0; QuantitySell = 0; VolumeBuy = 0; VolumeSell = 0; Comission = 0;
            }

            public void CopyTo(Recalc recalc)
            {
                int i = recalc, j = this;
                s_Profit[i] = s_Profit[j]; s_Cost[i] = s_Cost[j]; s_Quantity[i] = s_Quantity[j];
                s_QuantityBuy[i] = s_QuantityBuy[j]; s_QuantitySell[i] = s_QuantitySell[j]; s_Comission[i] = s_Comission[j];
                s_VolumeBuy[i] = s_VolumeBuy[j]; s_VolumeSell[i] = s_VolumeSell[j];
            }

            public static int LastNumber
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return EntityPool<ReP>.LastNumber; }
            }

            private Recalc(int number) { Number = number; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int Next()
            {
                int i = 0, j = 10;
                while ((i = EntityPool<ReP>.Next()) >= s_MaxRecalcPool)
                {
                    // попали сюда если пул весь занят, сначала подождем, если не поможет - кидаем ошибку 
                    EntityPool<ReP>.Next(-1); // уменьшим счетчик пула
                    if (j > 1)
                    {
                        // пробуем переключать контекст, может какой-то поток освободит пул
                        Thread.Sleep(0);
                        j--;
                    }
                    else
                    {
                        if (j > 0)
                        {
                            //  подождем еще раз
                            Thread.Sleep(10);
                            j--;
                        }
                        else
                        {
                            throw new TimeoutException();
                        }
                    }
                }
                return i;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Free() { EntityPool<ReP>.Free(Number); }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator int(Recalc recalc) { return recalc.Number; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Recalc(int positionNumber) { return new Recalc(positionNumber); }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Position(Recalc recalc)
            {
                return Position.Create(recalc);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Recalc(Position position) { return new Recalc(position); }

            public override string ToString()
            {
                return string.Concat(
                    "Cost: ", Cost.ToString(),
                    ", Profit: ", Profit.ToString(),
                    ", Quantity: ", Quantity.ToString(),
                    " ");
            }
        }
    }

    public static class PositionUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, PR> AsPositionHistory<TData>(this DateTime moment, TData state) where TData : struct
        {
            return new HD<TData, PR>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, PR> AsPositionHistory<TData>(this Timestamp moment, TData state) where TData : struct
        {
            return new HD<TData, PR>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, PR> AsPositionHistory<TData>(this TData state, DateTime moment) where TData : struct
        {
            return new HD<TData, PR>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, PR> AsPositionHistory<TData>(this TData state, Timestamp moment) where TData : struct
        {
            return new HD<TData, PR>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<int, T> AsToken<T>(this HD<Position, T> hist) where T : HR
        {
            return new HD<int, T>(hist.Timestamp, hist.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<int, T>> OfToken<T>(this IEnumerable<HD<Position, T>> hists) where T : HR
        {
            foreach(var hist in hists)
                yield return new HD<int, T>(hist.Timestamp, hist.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<Position, HistoryTypeOut> As<HistoryTypeIn, HistoryTypeOut>(this HD<Position.Recalc, HistoryTypeIn> hdata) where HistoryTypeIn : HR where HistoryTypeOut : HR
        {
            return new HD<Position, HistoryTypeOut>(hdata.Timestamp, hdata.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<Position.Recalc, HistoryTypeOut> As<HistoryTypeIn, HistoryTypeOut>(this HD<Position, HistoryTypeIn> hdata) where HistoryTypeIn : HR where HistoryTypeOut : HR
        {
            return new HD<Position.Recalc, HistoryTypeOut>(hdata.Timestamp, hdata.Data);
        }
    }

    public static class PositionFactoryUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this int positionNumber)
        {
            return positionNumber == Position.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this Position position)
        {
            return position == Position.Empty;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PositionFactory SetDefaultCurrency(this PositionFactory factory, CurrencyKey currency)
        {
            return new PositionFactory
            {
                DefaultCost = factory.DefaultCost,
                DefaultProfit = factory.DefaultProfit,
                DefaultQuantity = factory.DefaultQuantity,
                DefaultCurrency = currency,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PositionFactory SetDefaultProfit(this PositionFactory factory, ValueToken profit)
        {
            return new PositionFactory
            {
                DefaultCost = factory.DefaultCost,
                DefaultProfit = profit,
                DefaultQuantity = factory.DefaultQuantity,
                DefaultCurrency = factory.DefaultCurrency,
            };
        }
    }

    public class PositionFactory
    {
        public ValueToken DefaultCost;
        public ValueToken DefaultProfit;
        public ValueToken DefaultQuantity;
        public CurrencyKey DefaultCurrency;

        public static PositionFactory SuccessShortPositionFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new PositionFactory
                {
                    DefaultCost = 0.ToIncome(),
                    DefaultProfit = 0.ToIncome(),
                    DefaultQuantity = 0.ToSell(),
                };
            }
        }

        public static PositionFactory UnSuccessShortPositionFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new PositionFactory
                {
                    DefaultCost = 0.ToIncome(),
                    DefaultProfit = 0.ToOutcome(),
                    DefaultQuantity = 0.ToSell(),
                };
            }
        }

        public static PositionFactory SuccessLongPositionFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new PositionFactory
                {
                    DefaultCost = 0.ToOutcome(),
                    DefaultProfit = 0.ToIncome(),
                    DefaultQuantity = 0.ToBuy(),
                };
            }
        }

        public static PositionFactory UnSuccessLongPositionFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new PositionFactory
                {
                    DefaultCost = 0.ToOutcome(),
                    DefaultProfit = 0.ToOutcome(),
                    DefaultQuantity = 0.ToBuy(),
                };
            }
        }

        public static PositionFactory IncomeMoneyPositionFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new PositionFactory
                {
                    DefaultCost = ValueToken.Null,
                    DefaultProfit = ValueToken.Null,
                    DefaultQuantity = ValueToken.Null,
                };
            }
        }

        public static PositionFactory OutcomeMoneyPositionFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new PositionFactory
                {
                    DefaultCost = ValueToken.Null,
                    DefaultProfit = ValueToken.Null,
                    DefaultQuantity = ValueToken.Null,
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Position Create(decimal cost, decimal profit, decimal quantity)
        {
            return Position.Create(
                DefaultCost.SetValue(cost),
                DefaultProfit.SetValue(profit),
                DefaultQuantity.SetValue(quantity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Position Create(decimal profit, decimal quantity)
        {
            return Position.Create(
                DefaultCost,
                DefaultProfit.SetValue(profit),
                DefaultQuantity.SetValue(quantity));
        }
    }
}