using System;
using System.Collections.Generic;

using Vtb.PosKeep.Entity.Key;
using Vtb.PosKeep.Entity.Data;

namespace Vtb.PosKeep.Entity.Business.Model
{
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Storage;

    using AggregatePositionFunc = Func<IEnumerable<HD<int, PR>>, IEnumerable<HD<int, PR>>>;
    using AggregateRatesFunc = Func<IEnumerable<HD<int, RR>>, IEnumerable<HD<int, RR>>>;

    // RPR - RatedPositionReference
    /// <summary>
    /// RatedPositionReference
    /// </summary>
    public abstract class RtPR : HR { }

    public struct RatedPosition
    {
        public readonly int PositionID;
        public readonly ICurrencyRate Rate;

        public RatedPosition(int position_id, ICurrencyRate rate)
        {
            PositionID = position_id; Rate = rate;
        }

        public RatedPosition(int position_id, int Rate)
            : this(position_id, new CurrencyRate(Rate))
        {
        }

        public RatedPosition(HD<int, PR> position, HD<int, RR> Rate)
            : this(position.Data, new CurrencyRate(Rate.Data))
        {
        }

        public RatedPosition(Position position, Rate Rate)
            : this(position, new CurrencyRate(Rate))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Position(RatedPosition qp)
        {
            return qp.PositionID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(RatedPosition qp)
        {
            return qp.Rate.Value;
        }

        public override string ToString()
        {
            return string.Concat("Position: ", ((Position)this).ToString(), ", Rate: ", Rate.Value.ToString());
        }

        private struct CurrencyRate : ICurrencyRate
        {
            public readonly int RateID;

            public decimal Value => ((Rate)RateID).Value;

            public IEnumerable<int> Rates
            {
                get
                {
                    yield return RateID;
                }
            }

            public CurrencyRate(int Rate_id)
            {
                RateID = Rate_id;
            }

            public override string ToString()
            {
                return ((Rate)RateID).ToString();
            }
        }
    }

    public struct ConvertPosition
    {
        public static readonly Rate One = Rate.Create(1.0m);
        public readonly int PositionID;
        public readonly ICurrencyRate CurrencyRate;

        public ConvertPosition(int position, ICurrencyRate rate)
        {
            PositionID = position; CurrencyRate = rate;
        }

        public ConvertPosition(int position, int rate)
        {
            PositionID = position; CurrencyRate = new SingleRate(rate);
        }

        public ConvertPosition(int position, int @base, int rate)
        {
            PositionID = position; CurrencyRate = new ConvertRate(@base, rate);
        }

        public ConvertPosition(HD<int, PR> position, HD<int, QR> pRate, HD<int, QR> cRate)
            : this(position.Data, new ConvertRate(pRate.Data, cRate.Data))
        {
        }

        public ConvertPosition(HD<int, PR> position, HD<int, QR> pRate)
            : this(position.Data, new SingleRate(pRate.Data))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Position(ConvertPosition qp)
        {
            return qp.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(ConvertPosition qp)
        {
            return qp.CurrencyRate?.Value ?? default(decimal);
        }

        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return PositionID;
            }
        }

        public override string ToString()
        {
            return string.Concat("Position: ", ((Position)this).ToString(), ", Rate: ", CurrencyRate.Value.ToString());
        }

        private struct SingleRate : ICurrencyRate
        {
            public readonly int RateID;

            public decimal Value => ((Rate)RateID).Value;

            public IEnumerable<int> Rates
            {
                get
                {
                    yield return RateID;
                }
            }

            public SingleRate(int Rate_id)
            {
                RateID = Rate_id;
            }

            public override string ToString()
            {
                return ((Rate)RateID).ToString();
            }
        }

        private struct ConvertRate : ICurrencyRate
        {
            public readonly int BaseRateID;
            public readonly int RateRateID;

            public decimal Value => ((Rate)BaseRateID).Value / ((Rate)RateRateID).Value;

            public IEnumerable<int> Rates
            {
                get
                {
                    yield return BaseRateID;
                    yield return RateRateID;
                }
            }

            public ConvertRate(int base_Rate_id, int rate_Rate_id)
            {
                BaseRateID = base_Rate_id; RateRateID = rate_Rate_id;
            }

            public override string ToString()
            {
                return string.Concat("[", ((Rate)BaseRateID).ToString(), ", ", ((Rate)RateRateID).ToString(), "]");
            }
        }
    }

    public static class RateModel
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
                RatesAggregator = ratesAggregator ?? (Rates => Rates);
            }

            public Context(CurrencyKey currency, Timestamp from, Timestamp to, int period = 0, AggregatePositionFunc positionAggregator = null, AggregateRatesFunc ratesAggregator = null)
                : this(default(PositionKey), currency, from, to, period, positionAggregator, ratesAggregator)
            {
            }

            public Context(Timestamp from, Timestamp to, int period = 0, AggregatePositionFunc positionAggregator = null, AggregateRatesFunc ratesAggregator = null)
                : this(default(PositionKey), default(CurrencyKey), from, to, period, positionAggregator, ratesAggregator)
            {
            }

            public Context(CurrencyModel.Context context, CurrencyKey currency)
                : this(context.Position, currency, context.From, context.To, context.Period, context.PositionAggregator, context.RatesAggregator)
            {

            }

            public Context(CurrencyModel.Context context)
                : this(context, context.Position.Instrument.Currency)
            {

            }
        }

        public static IEnumerable<HD<RatedPosition, RtPR>> RatedPositions(PositionStorage positionStorage, RateStorage rateStorage, Context context)
        {
            return RatedPositions(positionStorage.Items(context.Position, context.From, context.To, -1).OfToken(), rateStorage, context);
        }

        public static IEnumerable<HD<RatedPosition, RtPR>> RatedPositions(IEnumerable<HD<int, PR>> positions, RateStorage rateStorage, Context context)
        {
            return RatedPositions(positions, rateStorage.Items(context.Currency, context.From, context.To, context.Period).OfToken(), context);
        }

        public static IEnumerable<HD<RatedPosition, RtPR>> RatedPositions(IEnumerable<HD<int, PR>> positions, IEnumerable<HD<int, RR>> rates, Context context)
        {
            return RatedPositions(context.PositionAggregator(positions), context.RatesAggregator(rates));
        }

        public static IEnumerable<HD<ConvertPosition, CPR>> RatedPositions(PositionStorage positionStorage, RateStorage rateStorage, Context positionContext, Context rateContext)
        {
            return RatedPositions(RatedPositions(positionStorage, rateStorage, positionContext),
                rateStorage.Items(rateContext.Currency, rateContext.From, rateContext.To, rateContext.Period)
                    .Select(rt => new HD<RatedPosition, RR>(rt.Timestamp, new RatedPosition(0, (int)rt.Data))));
        }

        public static IEnumerable<HD<ConvertPosition, CPR>> RatedPositions(IEnumerable<HD<int, PR>> positions, RateStorage RateStorage, Context positionContext, Context rateContext)
        {
            return RatedPositions(
                RatedPositions(positions, RateStorage, positionContext),
                RateStorage.Items(rateContext.Currency, rateContext.From, rateContext.To, rateContext.Period)
                    .Select(rt => new HD<RatedPosition, RR>(rt.Timestamp, new RatedPosition(0, (int)rt.Data))));
        }

        public static IEnumerable<HD<RatedPosition, RtPR>> RatedPositions(IEnumerable<HD<int, PR>> positions, IEnumerable<HD<int, RR>> rates)
        {
            var position = default(last_position);
            foreach (var zipItem in positions.Zip(rates))
            {
                if (position.number != zipItem.Item1.Data)
                    position = new last_position(zipItem.Item1.Data);
                else
                    if (position.quantity == 0)
                    continue;

                yield return new HD<RatedPosition, RtPR>(zipItem.Item2.Timestamp,
                    new RatedPosition(zipItem.Item1, zipItem.Item2));
            }
        }

        public static IEnumerable<HD<ConvertPosition, CPR>> RatedPositions(IEnumerable<HD<RatedPosition, RtPR>> positions,
            IEnumerable<HD<RatedPosition, RR>> rates)
        {
            foreach (var zipItem in positions.Zip(rates))
            {
                yield return new HD<ConvertPosition, CPR>(zipItem.Item2.Timestamp,
                    new ConvertPosition(zipItem.Item1.Data.PositionID, zipItem.Item1.Data.Rate.Rates.First(), zipItem.Item2.Data.Rate.Rates.First()));
            }
        }

        struct last_position
        {
            public readonly int number;
            public readonly decimal quantity;

            public last_position(int pos_number)
            {
                var p = (Position)pos_number;

                number = pos_number;
                quantity = p.Quantity.Value;
            }
        }
    }
}
