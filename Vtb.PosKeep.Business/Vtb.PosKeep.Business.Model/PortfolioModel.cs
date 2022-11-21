using System;
using System.Collections.Generic;

using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;

namespace Vtb.PosKeep.Entity.Business.Model
{
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Storage;

    using AggregatePositionFunc = Func<IEnumerable<HD<int, PR>>, IEnumerable<HD<int, PR>>>;
    using PositionEntry = KeyValuePair<int, Position>;

    using PositionToken = HD<int, PR>;
    using PositionValue = HD<Position, PR>;
    using PositionTokensEntry = KeyValuePair<PositionKey, HD<int, PR>[]>;
    using PositionsEntry = KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>;
    using DatingsEntry = KeyValuePair<PositionKey, IEnumerable<HD<int, DtPR>>>;
    using ConvertedEntry = KeyValuePair<PositionKey, IEnumerable<HD<ConvertPosition, CPR>>>;

    public class Portfolio
    {
        public readonly PositionTokensEntry[] Positions;
        public readonly DatingsEntry[] Dating;
        public readonly ConvertedEntry[] Quotes;

        public Portfolio(PositionTokensEntry[] positions, DatingsEntry[] positionsDating, ConvertedEntry[] positionsQuote)
        {
            Positions = positions;
            Dating = positionsDating;
            Quotes = positionsQuote;
        }

        public HD<int, PR>[][] FreePositions
        {
            get
            {
                return Positions.Select(p => p.Value).ToArray();
            }
        }

        public IEnumerable<HD<int, DtPR>>[] FreeDating
        {
            get
            {
                return Dating.Select(dt => dt.Value).ToArray();
            }
        }

        public IEnumerable<HD<ConvertPosition, CPR>>[] FreeQuotes
        {
            get
            {
                return Quotes.Select(q => q.Value).ToArray();
            }
        }
    }

    public static class PortfolioModel
    {
        public class Context
        {
            public readonly AccountKey Account;
            public readonly int Place;
            public readonly CurrencyKey Currency;
            public readonly Timestamp From;
            public readonly Timestamp To;
            public readonly int Period;

            public Context(AccountKey client, int place, CurrencyKey currency, Timestamp from, Timestamp to, int period)
            {
                Account = client;
                Place = place;
                Currency = currency;
                From = from;
                To = to;
                Period = period;
            }
        }

        public static void Recalc(this PortfolioStateStorage portfolioStorage, AccountKey client_id, PositionStorage positionStorage, Timestamp from, Timestamp to)
        {
            var positions = positionStorage.Items(client_id, from, to).ToArray();
            using (var states = GetPortfolioState(positions).GetEnumerator())
            {
                while (states.MoveNext())
                {
                    portfolioStorage.Add(client_id, states.Current);
                }
            }
        }

        public static Portfolio GetPortfolioState(PositionStorage positionStorage, RateStorage rateStorage, Context context)
        {
            var positions = ((context.Place == 0) ? positionStorage.Items(context.Account, context.From, context.To) :
                    positionStorage.Items(new TradeAccountKey(context.Account, context.Place), context.From, context.To))
                .Select(ps => new PositionTokensEntry(ps.Key, ps.Value.Select(p=> new PositionToken(p.Timestamp, p.Data))
                    .ToArray()))
                .ToArray();
            
            // ленивая загрузка
            var position_dates = positions
                .Select(ps =>
                {
                    PositionDatesModel.Context ctx = new PositionDatesModel.Context
                        (ps.Key, context.From, context.To, context.Period, pss => pss.AggregatePeriod(context.Period));

                    return new DatingsEntry(ps.Key, PositionDatesModel.GetPositionDates(ps.Value, ctx));
                }).ToArray();

            // ленивая загрузка
            var position_conversion = positions/*.Where(ps => ps.Key.InstrumentID.InstrumentID != Instrument.Money.ID 
             || ps.Key.InstrumentID.CurrencyID != context.Currency )*/
                .Select(ps =>
                {
                    CurrencyModel.Context ctx = new CurrencyModel.Context
                        (ps.Key, context.Currency, context.From, context.To, context.Period, pss => pss.AggregatePeriod(context.Period), rss => rss.AggregatePeriod(context.Period));

                    return new ConvertedEntry(ps.Key, CurrencyModel.ConvertedPositions(ps.Value, rateStorage, ctx));
                }).ToArray();


            return new Portfolio(positions, position_dates, position_conversion);
        }

        private static PortfolioState CreatePortfolioState(IDictionary<int, Position> positions, Func<int, PositionKey> keyGetter)
        {
            var items = new PortfolioState.Item[positions.Count];
            int i = 0;
            foreach (var position in positions)
            {
                var item = new PortfolioState.Item(keyGetter(position.Key), position.Value);

                if (item.Position.Quantity.Value == 0)
                    positions.Remove(position.Key);
                items[i++] = item;
            }

            return new PortfolioState(items);
        }

        public static IEnumerable<HD<PortfolioState, PSR>> GetPortfolioState(PositionsEntry[] instrumentSeries)
        {
            var currentPositions = new Dictionary<int, Position>(instrumentSeries.Length + 1);
            var series = instrumentSeries.Select(p => p.Value).ToArray();

            var states = series.AggregateHistory(
                start => currentPositions[start.Data.Key] = start.Data.Value,
                start => currentPositions[start.Data.Key] = start.Data.Value,
                // с побочным эффектом (удалением) для оптимизации
                time => new HD<PortfolioState, PSR>(time, 
                    CreatePortfolioState(currentPositions, i => instrumentSeries[i].Key)));

            return states;
        }

        public static IEnumerable<HD<ValueToken, HR>> ProfitAggregate(this IEnumerable<HD<int, DtPR>> positionDatings)
        {
            int prev = 0;
            var profit = default(ValueToken);
            var time = default(Timestamp);

            return positionDatings.AggregateHistory(
                item =>
                {
                    if (prev != item.Data)
                    {
                        var position = (Position)(prev = item.Data);
                        profit = position.Profit;
                    }
                    time = item.Timestamp;
                },
                () => new HD<ValueToken, HR>(time, profit));
        }

        public static IEnumerable<HD<ValueToken, HR>> ProfitAggregate(this IEnumerable<HD<int, DtPR>>[] series)
        {
            
            var currentIds = new int[series.Length + 1];
            var profit = default(ValueToken);

            var a = default(Action<HD<KeyValuePair<int, int>, DtPR>>);
            if (series.Length == 1)
            {
                var currentId = 0;
                a = pos =>
                {
                    if (currentId != pos.Data.Value)
                    {
                        currentId = pos.Data.Value;
                        var position = (Position)currentId;
                        profit = position.Profit;
                    }
                };
            }
            else
            {
                a = pos =>
                {
                     if (currentIds[pos.Data.Key] != pos.Data.Value)
                     {
                         currentIds[pos.Data.Key] = pos.Data.Value;
                         var position = (Position)pos.Data.Value;

                         profit = 0m.ToNull();
                         for (int i = 0; i < currentIds.Length; i++)
                         {
                             if (currentIds[i] != 0)
                                profit = profit + ((Position) currentIds[i]).Profit;
                         }
                     }
                };
            }
            return series.AggregateHistory(a, a, time => new HD<ValueToken, HR>(time, profit), false);
        }

        public static IEnumerable<HD<ValueToken, HR>> CostAggregate(this IEnumerable<HD<int, DtPR>> positionDatings)
        {
            int prev = 0;
            var cost = default(ValueToken);
            var time = default(Timestamp);

            return positionDatings.AggregateHistory(
                item =>
                {
                    if (prev != item.Data)
                    {
                        var position = (Position)(prev = item.Data);
                        cost = position.Cost;
                    }
                    time = item.Timestamp;
                },
                () => new HD<ValueToken, HR>(time, cost));
        }

        public static IEnumerable<HD<ValueToken, HR>> CostAggregate(this IEnumerable<HD<int, DtPR>>[] series)
        {
            var cost = default(ValueToken);

            var a = default(Action<HD<KeyValuePair<int, int>, DtPR>>);
            if (series.Length == 1)
            {
                var current = default(last_position);
                a = pos =>
                {
                    if (current.number != pos.Data.Value)
                        current = new last_position(pos.Data.Value);

                    cost = current.Cost;
                };

                return series.AggregateHistory(a, a, time => new HD<ValueToken, HR>(time, cost), false);
            }
            else
            {
                var currents = new last_position[series.Length + 1];
                a = pos =>
                {
                    var position = currents[pos.Data.Key];
                    if (position.number != pos.Data.Value)
                        position = currents[pos.Data.Key] = new last_position(pos.Data.Value);

                    cost = cost + position.Cost;
                };

                return series.AggregateHistory(pos => { cost = default(ValueToken); a(pos); }, a, 
                    time => new HD<ValueToken, HR>(time, cost), false);
            }            
        }

        public static IEnumerable<HD<ValueToken, HR>> ValueAggregate(this IEnumerable<HD<ConvertPosition, CPR>> positionQuotes)
        {
            var position = default(last_position);
            var value = default(ValueToken);
            var time = default(Timestamp);

            return positionQuotes.AggregateHistory(
                item =>
                {
                    if (position.number != item.Data.PositionID)
                        position = new last_position(item.Data.PositionID);

                    time = item.Timestamp;
                    if (position.quantity != 0)
                        value = position.Quantity * item.Data.CurrencyRate.Value;
                },
                () => new HD<ValueToken, HR>(time, value));
        }

        public static IEnumerable<HD<ValueToken, HR>> ValueAggregate(this IEnumerable<HD<ConvertPosition, CPR>>[] series)
        {
            var value = default(ValueToken);

            var a = default(Action<HD<KeyValuePair<int, ConvertPosition>, CPR>>);
            if (series.Length == 1)
            {
                var current = default(last_position);
                a = pos =>
                {
                    if (current.number != pos.Data.Value.PositionID)
                        current = new last_position(pos.Data.Value.PositionID);

                    if (current.quantity != 0)
                    {
                        value = current.Quantity * pos.Data.Value.CurrencyRate.Value;
                    }
                };
                return series.AggregateHistory(a, a, time => new HD<ValueToken, HR>(time, value), false);
            }
            else
            {
                var currents = new last_position[series.Length + 1];
                a = pos =>
                {
                    var position = currents[pos.Data.Key];
                    if (position.number != pos.Data.Value.PositionID)
                        position = currents[pos.Data.Key] = new last_position(pos.Data.Value.PositionID);

                    if (position.quantity != 0)
                    {
                        value += position.Quantity * pos.Data.Value.CurrencyRate.Value;
                    }
                };
                return series.AggregateHistory(pos => { value = 0.ToNull(); a(pos); }, a, time => new HD<ValueToken, HR>(time, value), false);
            }
        }

        private static decimal PositionValue(last_position position, decimal quote)
        {
            if (position.quantity != 0)
            {
                switch (position.Quantity.Type)
                {
                    case (int)ValueTokenType.Buy:
                    case (int)ValueTokenType.Income:
                        return (quote * position.quantity - position.cost);
                    case (int)ValueTokenType.Sell:
                        return (position.cost - quote * position.quantity);
                }
            }

            return 0m;
        }

        public static IEnumerable<PositionReportLine> PositionCurve(this IEnumerable<HD<ConvertPosition, CPR>> positionQuotes, int place_id, TradeInstrumentKey instrument)
        {
            using (var quoteEnumerator = positionQuotes.GetEnumerator())
            {
                if (quoteEnumerator.MoveNext())
                {
                    var begin = quoteEnumerator.Current;
                    var empty = new HD<ConvertPosition, CPR>(default(Timestamp), default(ConvertPosition));
                    var reportLine = new PositionReportLine(place_id, instrument, empty, begin);
                    yield return reportLine;

                    if (reportLine.quantity == 0)
                        begin = empty;

                    while(quoteEnumerator.MoveNext())
                    {
                        reportLine = new PositionReportLine(place_id, instrument, begin, quoteEnumerator.Current);
                        yield return reportLine;
                        begin = (reportLine.quantity == 0) ? empty : quoteEnumerator.Current;
                    }
                }
            }
        }

        public static IEnumerable<HD<decimal, HR>> ValueCurve(this IEnumerable<HD<ConvertPosition, CPR>> positionQuotes)
        {
            var position = default(last_position);
            var value = default(decimal);
            var time = default(Timestamp);

            return positionQuotes.AggregateHistory(
                item =>
                {
                    if (position.number != item.Data.PositionID)
                        position = new last_position(item.Data.PositionID);

                    time = item.Timestamp;
                    if (position.quantity != 0)
                        value = 100m * PositionValue(position, item.Data.CurrencyRate.Value) / position.cost;
                },
                () => new HD<decimal, HR>(time, value));
        }

        public static IEnumerable<HD<decimal, HR>> ValueCurve(this IEnumerable<HD<ConvertPosition, CPR>>[] series)
        {   
            var volume = default(decimal);
            var volumeCost = default(decimal);

            HD<decimal, HR> get (Timestamp time)
            {
                return new HD<decimal, HR>(time, (volumeCost != 0) ? 100m * volume / volumeCost : 0);
            }

            var append = default(Action<HD<KeyValuePair<int, ConvertPosition>, CPR>>);
            if (series.Length == 1)
            {
                var current = default(last_position);
                append = pos =>
                {
                    if (current.number != pos.Data.Value.PositionID)
                        current = new last_position(pos.Data.Value.PositionID);

                    if (current.quantity != 0)
                    {
                        volume = PositionValue(current, pos.Data.Value.CurrencyRate.Value);
                        volumeCost = current.cost;
                    }
                };

                return series.AggregateHistory(append, append, get, false);
            }
            else
            {
                var currentPositions = new last_position[series.Length + 1];
                var currentVolumes = new decimal[series.Length + 1];
                append = pos =>
                {
                    //var position = currentPositions[pos.Data.Key];

                    var i = pos.Data.Key;
                   
                    if (currentPositions[i].number != pos.Data.Value.PositionID)
                    {
                        volumeCost -= currentPositions[i].cost;
                        currentPositions[i] = new last_position(pos.Data.Value.PositionID);
                        volumeCost += currentPositions[i].cost;
                    }

                    volume -= currentVolumes[i];
                    currentVolumes[i] = PositionValue(currentPositions[i], pos.Data.Value.CurrencyRate.Value);
                    volume += currentVolumes[i];
                };

                return series.AggregateHistory(append, append, get, false);
            }
        }

        struct last_position
        {
            public readonly int number;
            public readonly ValueToken Quantity;
            public readonly ValueToken Profit;
            public readonly ValueToken Cost;

            public last_position(int pos_number)
            {
                var p = (Position)pos_number;

                number = pos_number;
                Quantity = p.Quantity;
                Profit = p.Profit;
                Cost = p.Cost;
            }

            public decimal cost
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Cost.Value; }
            }

            public decimal quantity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Quantity.Value; }
            }
        }
    }
}
