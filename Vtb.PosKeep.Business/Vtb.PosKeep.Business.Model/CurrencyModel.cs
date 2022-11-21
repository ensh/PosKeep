using System;
using System.Collections.Generic;

using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;

namespace Vtb.PosKeep.Entity.Business.Model
{
    using System.Linq;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Storage;
    
    using AggregatePositionFunc = Func<IEnumerable<HD<int, PR>>, IEnumerable<HD<int, PR>>>;
    using AggregateRatesFunc = Func<IEnumerable<HD<int, RR>>, IEnumerable<HD<int, RR>>>;

    //CPR - ConvertedPositionReference
    /// <summary>
    /// ConvertedPositionReference
    /// </summary>
    public abstract class CPR : HR { }

    public interface ICurrencyRate
    {
        IEnumerable<int> Rates { get; }
        decimal Value { get; }
    }
    public class CurrencyModel
    {
        public class Context
        {
            public readonly PositionKey Position;
            public readonly CurrencyKey Currency;
            public readonly Timestamp From;
            public readonly Timestamp To;
            public readonly int Period;
            public readonly AggregatePositionFunc PositionAggregator;
            public readonly AggregateRatesFunc RatesAggregator;

            public Context(PositionKey position, CurrencyKey currency, Timestamp from, Timestamp to, int period = 0, AggregatePositionFunc positionAggregator = null, AggregateRatesFunc ratesAggregator = null)
            {
                Position = position;
                Currency = currency;
                From = from;
                To = to;
                Period = period;
                PositionAggregator = positionAggregator ?? (positions => positions);
                RatesAggregator = ratesAggregator ?? (quotes => quotes);
            }

            public Context(Timestamp from, Timestamp to, int period = 0, bool withNullPosition = false, AggregatePositionFunc positionAggregator = null, AggregateRatesFunc ratesAggregator = null)
                : this(default(PositionKey), default(CurrencyKey), from, to, period, positionAggregator, ratesAggregator)
            {
            }
        }

        public static IEnumerable<HD<ConvertPosition, CPR>> ConvertedPositions(PositionStorage positionStorage, RateStorage rateStorage, Context context)
        {
            return ConvertedPositions(positionStorage.Items(context.Position, context.From, context.To, -1).OfToken(), rateStorage, context);
        }

        public static IEnumerable<HD<ConvertPosition, CPR>> ConvertedPositions(IEnumerable<HD<int, PR>> positions, RateStorage rateStorage, Context context)
        {
            if (context.Currency == CurrencyKey.Empty || context.Position.Instrument.Currency == context.Currency)
            {
                foreach (var position in context.PositionAggregator(positions))
                {
                    yield return new HD<ConvertPosition, CPR>(position.Timestamp,
                        new ConvertPosition(position, Rate.One));
                }
            }
            else
            {
                if (rateStorage.BaseCurrency == context.Currency)
                {
                    if (!rateStorage.First(context.Position.Instrument.Currency).Data.IsNull)
                    {
                        var ratedContext = new RateModel.Context(context);

                        foreach (var ratedPosition in RateModel.RatedPositions(positions, rateStorage, ratedContext))
                        {
                            yield return new HD<ConvertPosition, CPR>(ratedPosition.Timestamp,
                                new ConvertPosition(ratedPosition.Data.PositionID, ratedPosition.Data.Rate));
                        }
                    }
                }
                else
                {
                    if (!rateStorage.First(context.Position.Instrument.Currency).Data.IsNull &&
                        !rateStorage.First(context.Currency).Data.IsNull)
                    {
                        var positionContext = new RateModel.Context(context);
                        var conversionContext = new RateModel.Context(context.Currency, context.From, context.To);

                        foreach (var convertPosition in RateModel.RatedPositions(positions, rateStorage, positionContext, conversionContext))
                        {
                            yield return convertPosition;
                        }
                    }
                }
            }
        }
    }
}
