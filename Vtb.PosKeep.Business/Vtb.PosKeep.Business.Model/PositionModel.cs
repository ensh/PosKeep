using System;
using System.Collections.Generic;

using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;
using Vtb.PosKeep.Entity.Storage;

namespace Vtb.PosKeep.Entity.Business.Model
{
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Vtb.PosKeep.Entity;

    using HRP = HD<Position.Recalc, RPR>;
    using TransformFunc = Func<IEnumerable<HD<Position.Recalc, RPR>>, IEnumerable<HD<Position.Recalc, RPR>>>;
    using PositionGetterFunc = Func<DealStorage, DealKey, PositionAlgorithmContext, IEnumerable<HD<Position.Recalc, RPR>>>;

    public static class PositionModel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyQuantity(this Position.Recalc position, RecalcDeal deal)
        {
            position.Quantity += deal.Quantity;

            switch (deal.Quantity.Type)
            {
                case (byte)ValueTokenType.Buy:
                    position.QuantityBuy += deal.Quantity.Value;
                    position.VolumeBuy += deal.Volume.Value;
                    break;
                case (byte)ValueTokenType.Sell:
                    position.QuantitySell += deal.Quantity.Value;
                    position.VolumeSell += deal.Volume.Value;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyMoney(this Position.Recalc position, RecalcDeal deal)
        {
            var volume = deal.Volume.ToMoney() + deal.Comission.ToOutcome();
            position.Quantity += volume;
            position.Cost += volume;

            switch (volume.Type)
            {
                case (byte)ValueTokenType.Outcome:
                    position.QuantityBuy += volume.Value;
                    break;
                case (byte)ValueTokenType.Income:
                    position.QuantitySell += volume.Value;
                    break;
            }
        }
        private static class MONEY
        {
            public static IEnumerable<HD<Position.Recalc, RPR>> Positions(IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, PositionAlgorithmContext algoContext)
            {
                if (algoContext.Method != CostMethod.MONEY)
                    throw new ArgumentException("Must be MONEY!");

                var currentState = algoContext.OnNextCurrency();
                var currentPosition = currentState.Recalc.Data;

                try
                {
                    // торговые сделки
                    using (var dealEnumerator = deals.ToRecalcDeal().GetEnumerator())
                    {
                        if (dealEnumerator.MoveNext())
                        {
                            do
                            {
                                yield return new HRP(dealEnumerator.Current.From, currentPosition);

                                // вычисляем количество
                                currentPosition.ApplyMoney(dealEnumerator.Current);
                                currentPosition.Comission += dealEnumerator.Current.Comission;

                                yield return new HRP(dealEnumerator.Current.From, currentPosition);
                            } while (dealEnumerator.MoveNext());

                            algoContext.OnLast?.Invoke(default(RecalcState));
                        }
                    }
                }
                finally
                {
                    currentPosition.Free();
                }
            }
        }

        private static class FIFO
        {
            private struct AggregateContext
            {
                public readonly LinkedList<RecalcDeal> DealMemory;

                public AggregateContext(LinkedList<RecalcDeal> dealMemory)
                {
                    DealMemory = dealMemory;
                }

                private ValueToken firstQuantity
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        return (DealMemory.Count > 0) ? DealMemory.First.Value.Quantity : ValueToken.Null;
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    set
                    {
                        var firstDeal = DealMemory.First.Value;
                        DealMemory.First.Value = new RecalcDeal(value * firstDeal.Price, value, 0m, firstDeal.From);
                    }
                }

                private decimal firstPrice
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        return DealMemory.First.Value.Price;
                    }

                }

                private enum CalcState : byte { Ready, Continue }
                public void AddRealProfit(RecalcDeal deal, Position.Recalc currentPosition)
                {
                    for (var calcState = CalcState.Continue; calcState == CalcState.Continue; )
                    {
                        calcState = CalcState.Ready;

                        if (firstQuantity.CoType(deal.Quantity))
                        {
                            // однонаправленная сделка или окончание списания
                            currentPosition.Cost += deal.Volume;
                            DealMemory.AddLast(deal);
                        }
                        else
                        {
                            switch (deal.Quantity.Type)
                            {
                                case (byte)ValueTokenType.Buy:
                                case (byte)ValueTokenType.Sell:
                                    calcState = CalcTradeProfit(ref deal, currentPosition);
                                    break;
                                    // пока не знаем, что с этим делать
                                case (byte)ValueTokenType.Income:
                                    // увеличиваем 
                                    return;
                                case (byte)ValueTokenType.Outcome:
                                    return;
                            }

                        }
                    }                    
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private CalcState CalcTradeProfit(ref RecalcDeal deal, Position.Recalc currentPosition)
                {
                    var firstPrice = new ValueToken(deal.Volume.Type, DealMemory.First.Value.Price);
                    var firstQuantity = this.firstQuantity;

                    if (firstQuantity.Value > deal.Quantity.Value)
                    {
                        // остаток первой сделки в противоположном направлении, больше текущей 
                        var dealVolumeInFirstPrice = deal.Quantity.Value * firstPrice;

                        currentPosition.Cost += dealVolumeInFirstPrice;
                        currentPosition.Profit += !dealVolumeInFirstPrice;
                        currentPosition.Profit += deal.Volume;

                        this.firstQuantity += deal.Quantity;
                    }
                    else
                    {
                        bool MoveNext(LinkedList<RecalcDeal> dealMemory)
                        {
                            dealMemory.RemoveFirst();
                            return dealMemory.Count > 0;
                        }

                        // списываем остаток от противоположной сделки и ищем дальше
                        var dealVolumeInFirstPrice = firstQuantity.Value * firstPrice;
                        var dealPrice = new ValueToken(deal.Volume.Type, deal.Price);

                        currentPosition.Cost += dealVolumeInFirstPrice;
                        currentPosition.Profit += !dealVolumeInFirstPrice;
                        currentPosition.Profit += dealPrice * firstQuantity.Value;

                        var newQuantity = deal.Quantity + firstQuantity;

                        // новую начальную сделку для последующего списания
                        if (MoveNext(DealMemory))
                        {
                            // нашли след. сделку с тем же знаком
                            // переписываем нач. количество
                            //firstQuantity = DealMemory.Current.Quantity;
                            if (newQuantity.IsNull)
                                return CalcState.Ready;
                            else
                            {
                                // продолжаем списание
                                deal = new RecalcDeal(newQuantity.Value * dealPrice, newQuantity, 0m, deal.From); // уменьшаем количество к списанию на следующем шаге
                                return CalcState.Continue;
                            }
                        }
                        else
                        {
                            if (newQuantity.IsNull)
                            {
                                currentPosition.Cost = ValueToken.Null;
                            }
                            else
                            {
                                // переписываем нач, количество
                                deal = new RecalcDeal(newQuantity.Value * dealPrice, newQuantity, 0m, deal.From);
                                DealMemory.AddLast(deal);
                                // изменяем стоимость позиции
                                currentPosition.Cost = deal.Volume;
                            }
                        }
                    }

                    return CalcState.Ready;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static implicit operator RecalcState(AggregateContext ctx)
                {
                    return new RecalcState(ctx.firstQuantity, ctx.DealMemory.First?.Value.From ?? Timestamp.MinValue);
                }
            }

            public static IEnumerable<HD<Position.Recalc, RPR>> Positions(IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, PositionAlgorithmContext algoContext)
            {
                if (algoContext.Method != CostMethod.FIFO)
                    throw new ArgumentException("Must be FIFO!");

                var currentState = algoContext.OnNextCurrency();
                var currentPosition = currentState.Recalc.Data;

                try
                {
                    // торговые сделки
                    using (var dealEnumerator = deals.ToRecalcDeal().GetEnumerator())
                    {
                        if (dealEnumerator.MoveNext())
                        {
                            var context = new AggregateContext(new LinkedList<RecalcDeal>());
                            do
                            {
                                yield return new HRP(dealEnumerator.Current.From, currentPosition);

                                // вычисляем количество
                                currentPosition.ApplyQuantity(dealEnumerator.Current);
                                currentPosition.Comission += dealEnumerator.Current.Comission;

                                // вычисление реализованной прибыли
                                context.AddRealProfit(dealEnumerator.Current, currentPosition);

                                yield return new HRP(dealEnumerator.Current.From, currentPosition);
                            } while (dealEnumerator.MoveNext());

                            algoContext.OnLast?.Invoke(context);
                        }
                    }
                }
                finally
                {
                    currentPosition.Free();
                }
            }
        } // FIFO

        private static class LIFO
        {
            public static IEnumerable<HD<Position.Recalc, RPR>> Positions(IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, PositionAlgorithmContext algoContext)
            {
                if (algoContext.Method != CostMethod.LIFO)
                    throw new ArgumentException("Must be LIFO!");

                throw new NotImplementedException("LIFO");
            }
        } // LIFO

        private static class WA
        {
            public static IEnumerable<HD<Position.Recalc, RPR>> Positions(IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, PositionAlgorithmContext algoContext)
            {
                if (algoContext.Method != CostMethod.WA)
                    throw new ArgumentException("Must be WA!");

                throw new NotImplementedException("WA");
            }
        } // WA

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<KeyValuePair<CurrencyKey, IEnumerable<HD<Deal, DR>>>> GetDealsByCurrency(this DealStorage deals, DealKey dealKey, HD<Deal, DR> start)
        {
            return deals.Items(dealKey, start).GetDealsByCurrency(dealKey.Instrument.Currency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<KeyValuePair<CurrencyKey, IEnumerable<HD<Deal, DR>>>> GetDealsByCurrency(this IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency)
        {
            var dealsByCurrency = new LinkedList<KeyValuePair<CurrencyKey, LinkedList<HD<Deal, DR>>>>();

            LinkedList<HD<Deal, DR>> getCurrencyDeals(CurrencyKey curr, HD<Deal, DR> deal)
            {
                var result = dealsByCurrency.FirstOrDefault(p => p.Key == currency);
                if (result.Key == 0)
                {
                    result = new KeyValuePair<CurrencyKey, LinkedList<HD<Deal, DR>>>(curr, new LinkedList<HD<Deal, DR>>());
                    dealsByCurrency.AddLast(result);
                }
                return result.Value;
            }

            foreach (var deal in deals)
            {
                var currencyDeals = getCurrencyDeals(currency, deal);
                currencyDeals.AddLast(deal);
            }

            return dealsByCurrency.Select(dc => new KeyValuePair<CurrencyKey, IEnumerable<HD<Deal, DR>>>(dc.Key, dc.Value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<RecalcDeal> ToRecalcDeal(this IEnumerable<HD<Deal, DR>> deals)
        {
            return deals.Select(deal => new RecalcDeal(deal));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<Position.Recalc, RPR>> Positions(this DealStorage deals, DealKey dealKey, PositionAlgorithmContext algoContext)
        {
            return deals.Items(dealKey,
                // получить дату самуой поздней посчитанной сделки по инструменту
                (HD<Deal, DR>)algoContext.From()).Positions(dealKey.Instrument.Currency, algoContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HRP> TestPositions(this IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, CostMethod method = CostMethod.FIFO)
        {
            // полный пересчет, для тестов, без агрегации
            using (var position = Positions(deals, currency, new PositionAlgorithmContext(method, (b) => RecalcResult.Default(method))).GetEnumerator())
            {
                if (position.MoveNext())
                {
                    while (position.MoveNext())
                    {
                        yield return position.Current;

                        position.MoveNext();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HRP> Positions(this IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, CostMethod method = CostMethod.FIFO)
        {
            // полный пересчет, для агрегации
            return Positions(deals, currency, new PositionAlgorithmContext(method, (b) => RecalcResult.Default(method)));
        }

        public static void Recalc(this PositionRecalcStorage positionStorage, AccountKey client_id, DealStorage deals, bool recalcAll, TransformFunc transform, CostMethod method = CostMethod.FIFO)
        {
            positionStorage.DoRecalc(client_id, deals, recalcAll, Positions, transform, method);        
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HRP> Positions(this IEnumerable<HD<Deal, DR>> deals, CurrencyKey currency, PositionAlgorithmContext algoContext)
        {
            switch (algoContext.Method)
            {
                case CostMethod.FIFO:
                    return FIFO.Positions(deals, currency, algoContext);
                case CostMethod.LIFO:
                    return LIFO.Positions(deals, currency, algoContext);
                case CostMethod.WA:
                    return WA.Positions(deals, currency, algoContext);
                case CostMethod.MONEY:
                    return MONEY.Positions(deals, currency, algoContext);
                default:
                    return FIFO.Positions(deals, currency, algoContext);
            }
        }
    }
}
