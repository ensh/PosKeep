using System;
using System.Collections.Generic;

using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;

namespace Vtb.PosKeep.Entity.Storage
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    using TransformFunc = Func<IEnumerable<HD<Position.Recalc, RPR>>, IEnumerable<HD<Position.Recalc, RPR>>>;
    using HRP = HD<Position.Recalc, RPR>;
    using PositionGetterFunc = Func<DealStorage, DealKey, PositionAlgorithmContext, IEnumerable<HD<Position.Recalc, RPR>>>;

    public class PositionRecalcStorage : PositionStorage
    {
        public PositionRecalcStorage(IBlockStorageFactory<HD<Position, PR>> blockStorageFactory) : base(blockStorageFactory)
        {
            PositionRecalcStates = new ConcurrentDictionary<PositionKey, RecalcState>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void DoOnNewKey(PositionKey key)
        {
            PositionRecalcStates.AddOrUpdate(key, _ => RecalcState.Default(CostMethod.FIFO), (k, state) => state);

            base.DoOnNewKey(key);
        }

        protected ConcurrentDictionary<PositionKey, RecalcState> PositionRecalcStates;

        // работать только через нее!!!!
        public void DoRecalc(AccountKey client_id, DealStorage deals, bool recalcAll,
            PositionGetterFunc positionsGetter, TransformFunc transform, CostMethod method)
        {
            foreach (var dealKey in deals.DealKeys(client_id))
            {
                DoRecalc(dealKey, deals, recalcAll, positionsGetter, transform, method);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoRecalc(DealKey dealKey, DealStorage deals, bool recalcAll, PositionGetterFunc positionsGetter, TransformFunc transform, CostMethod method)
        {
            var new_positions = new LinkedList<KeyValuePair<CurrencyKey, LinkedList<HD<Position, PR>>>>();
            var recalc_positions = new LinkedList<KeyValuePair<CurrencyKey, Position.Recalc>>();

            if (dealKey.Instrument.Instrument == Instrument.Money.ID)
                method = CostMethod.MONEY;


            LinkedList<HD<Position, PR>> newPositions(CurrencyKey currency, Position.Recalc recalc)
            {
                var result = new_positions.FirstOrDefault(p => p.Key == currency);
                if (result.Key == 0)
                {
                    result = new KeyValuePair<CurrencyKey, LinkedList<HD<Position, PR>>>(currency,
                        new LinkedList<HD<Position, PR>>());
                    new_positions.AddLast(result);
                    recalc_positions.AddLast(new KeyValuePair<CurrencyKey, Position.Recalc>(currency, recalc));
                }

                return result.Value;
            }

            var algorithmContext = new PositionAlgorithmContext(method,
                () =>
                {
                    // первая сделка по инструменту и валюте
                    var rs = GetRecalcState(dealKey, recalcAll, method);
                    // сохранить, чтобы почистить пул
                    recalc_positions.AddLast(new KeyValuePair<CurrencyKey, Position.Recalc>(dealKey, rs.Recalc));
                    return rs;
                },
                () => GetFrom(dealKey, recalcAll, method),
                (state) =>
                {
                    // все сделки по инструменту и валюте обработаны
                    var key = new PositionKey(dealKey);
                    PositionRecalcStates.TryGetValue(key, out var before);
                    PositionRecalcStates.TryUpdate(key, state, before);
                });

            try
            {
                foreach (var position in transform(positionsGetter(deals, dealKey, algorithmContext)))
                {
                    var this_positions = newPositions(dealKey.Instrument.Currency, position.Data);
                    this_positions.AddLast(position.As<RPR, PR>());
                }

                foreach (var positions in new_positions)
                {
                    var positionKey = new PositionKey(dealKey);
                    AddRange(positionKey, positions.Value
                        .Prepend(new HD<Position, PR>(positions.Value.First.Value.Timestamp, Position.Empty)));
                }
            }
            finally
            {
                foreach (var recalc in recalc_positions)
                    recalc.Value.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PositionKey GetPositionKey(DealKey dealKey, CurrencyKey currency)
        {
            return new PositionKey(dealKey, new TradeInstrumentKey(currency, dealKey));
        }

        private RecalcResult GetRecalcState(DealKey dealKey, bool recalcAll, CostMethod method)
        {
            if (!recalcAll)
            {
                // если знаем валюту сделки 
                var result = default(RecalcResult);
                var positionKey = new PositionKey(dealKey);
                var startRecalcPosition = default(HD<Position, PR>);

                WithRange(positionKey, range =>
                {
                    if (!range.Item2.Timestamp.IsNull) // здесь только начало расчета
                    {
                        if (!PositionRecalcStates.TryGetValue(positionKey, out var lastRecalcState)
                          || lastRecalcState.Method != method)
                        {
                            lastRecalcState = RecalcState.Default(method);
                        }
                        startRecalcPosition = Items(positionKey, range.Item2, -1).First();
                        result = new RecalcResult(lastRecalcState, startRecalcPosition.As<PR, HR>());
                    }
                });

                return result;
            }
            return RecalcResult.Default(method);
        }

        private Timestamp GetFrom(DealKey dealKey, bool recalcAll, CostMethod method)
        {
            //пересчет при смене метода не реализован!!!
            if (!recalcAll)
            {
                if (AccountInstruments.TryGetValue(dealKey, out var instrumentKeys))
                {
                    // ищем последнюю посчитанную сделку по инструменту и начинаем расчет
                    using (var l = instrumentKeys.ReadLocker())
                    {
                        var lastRecalcState = RecalcState.Default(method);
                        for (int i = 0; i < instrumentKeys.Data.Length; i++)
                        {
                            var positionKey = new PositionKey(dealKey, instrumentKeys.Data[i]);
                            if (PositionRecalcStates.TryGetValue(positionKey, out var state) && state.Method == method &&
                                state.Start.CompareTo(lastRecalcState.Start) > 0)
                            {
                                lastRecalcState = state;
                            }
                        }

                        return lastRecalcState.Start;
                    }
                }
            }
            return Timestamp.MinValue;
        }
    }
}

