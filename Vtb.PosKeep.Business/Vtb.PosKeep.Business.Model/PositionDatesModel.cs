using System;
using System.Collections.Generic;

using Vtb.PosKeep.Entity.Key;

namespace Vtb.PosKeep.Entity.Business.Model
{
    using System.Linq;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Storage;

    using HistoryData = HD<int, HR>;
    using AggregatePositionFunc = Func<IEnumerable<HD<int, PR>>, IEnumerable<HD<int, PR>>>;

    //DtR - DateReference
    /// <summary>
    /// DateReference
    /// </summary>
    public abstract class DtR : HR { }

    // DtPR - DatedPositionReference
    /// <summary>
    /// DatedPositionReference
    /// </summary>
    public abstract class DtPR : HR { }

    public static class PositionDatesModel
    {
        public class Context
        {
            public readonly PositionKey Position;
            public readonly Timestamp From;
            public readonly Timestamp To;
            public readonly AggregatePositionFunc Aggregator;
            public readonly int Period;

            public Context(PositionKey position, Timestamp from, Timestamp to, int period, AggregatePositionFunc aggregator = null)
            {
                Position = position;
                From = from;
                To = to;
                Period = period;
                Aggregator = aggregator ?? (positions => positions);
            }

            public Context(Timestamp from, Timestamp to, int period, AggregatePositionFunc aggregator = null)
                : this(default(PositionKey), from, to, period, aggregator)
            {
            }
        }

        public static IEnumerable<HD<int, DtR>> GetDates(Timestamp from, Timestamp to, int period)
        {
            return Enumerable.Range(0, (to.GetHashCode() - from.GetHashCode()) / period)
                .Select(t => new HD<int, DtR>(from.GetHashCode() + t * period, t));
        }

        public static IEnumerable<HD<int, DtPR>> GetPositionDates(PositionStorage positionStorage, Context context)
        {
            return GetPositionDates(positionStorage.Items(context.Position, context.From, context.To, -1), context);
        }

        public static IEnumerable<HD<int, DtPR>> GetPositionDates(IEnumerable<HD<Position, PR>> positions, Context context)
        {
            return GetPositionDates(positions.OfToken(), context);
        }

        public static IEnumerable<HD<int, DtPR>> GetPositionDates(IEnumerable<HD<int, PR>> positions, Context context)
        {
            return GetPositionDates(context.Aggregator(positions), GetDates(context.From, context.To, context.Period));
        }

        public static IEnumerable<HD<int, DtPR>> GetPositionDates(IEnumerable<HD<int, PR>> positions, IEnumerable<HD<int, DtR>> dates)
        {
            var position = default(last_position);
            foreach (var zipItem in positions.Zip(dates))
            {
                if (position.number != zipItem.Item1.Data)
                    position = new last_position(zipItem.Item1.Data);

                if (position.number.IsEmpty() || position.quantity != 0m || position.profit != 0m)
                    yield return new HD<int, DtPR>(zipItem.Item2.Timestamp, position.number);
            }
        }

        struct last_position
        {
            public readonly int number;
            public readonly decimal quantity;
            public readonly decimal profit;

            public last_position(int pos_number)
            {
                var p = (Position)pos_number;

                number = pos_number;
                quantity = p.Quantity.Value;
                profit = p.Profit.Value;
            }
        }
    }
}
