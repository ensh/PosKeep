namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Linq;

    using Vtb.PosKeep.Entity.Data;

    // HR - HistoryReference
    /// <summary>
    /// HistoryReference
    /// </summary>
    public abstract class HR { }

    // HD - HistoricalData
    public struct HD<DataType, HistoryType> : IComparable<HD<DataType, HistoryType>> where DataType : struct where HistoryType : HR
    {
        public readonly Timestamp Timestamp;
        public readonly DataType Data;

        public HD(Timestamp timestamp, DataType data)
        {
            Timestamp = timestamp; Data = data;
        }

        public HD(Timestamp timestamp)
            : this (timestamp, default(DataType))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(HD<DataType, HistoryType> other)
        {
            return Timestamp.CompareTo(other.Timestamp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo<OtherHistoryType>(HD<DataType, OtherHistoryType> other) where OtherHistoryType : HR
        {
            return Timestamp.CompareTo(other.Timestamp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo<OtherDataType, OtherHistoryType>(HD<OtherDataType, OtherHistoryType> other) where OtherDataType : struct where OtherHistoryType : HR
        {
            return Timestamp.CompareTo(other.Timestamp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DataType(HD<DataType, HistoryType> hdata)
        {
            return hdata.Data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HD<DataType, HistoryType>(Timestamp timestamp)
        {
            return new HD<DataType, HistoryType>(timestamp, default(DataType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HD<DataType, HistoryType> SetMoment(Timestamp moment)
        {
            return new HD<DataType, HistoryType>(moment, Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HD<DataType, HistoryType> Replace(DataType data)
        {
            return new HD<DataType, HistoryType>(Timestamp, data);
        }

        public override string ToString()
        {
            return string.Concat("Moment: ", Timestamp.ToString(), ", Data: ", Data.ToString());
        }
    }

    public static class Historical
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<TData, T>> NormalizePeriod<TData, T>(this IEnumerable<HD<TData, T>> history, int period) where TData : struct where T : HR
        {
            foreach (var hdata in history)
                yield return hdata.SetMoment(hdata.Timestamp.Upper(period));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<TData, T>> AggregatePeriod<TData, T>(this IEnumerable<HD<TData, T>> history, int period) where TData : struct where T : HR
        {
            using (var aggregateHistoryEnumerator = history.NormalizePeriod(period).GetEnumerator())
            {
                if (aggregateHistoryEnumerator.MoveNext())
                {
                    var current = aggregateHistoryEnumerator.Current;

                    while (aggregateHistoryEnumerator.MoveNext())
                    {
                        if (current.Timestamp < aggregateHistoryEnumerator.Current.Timestamp)
                            yield return current;

                        current = aggregateHistoryEnumerator.Current;
                    }

                    yield return current;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, T> Create<TData, T>(DateTime moment, TData state) where TData : struct where T : HR
        {
            return new HD<TData, T>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, T> Create<TData, T>(Timestamp moment, TData state) where TData : struct where T : HR
        {
            return new HD<TData, T>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, T> AsHistory<TData, T>(this DateTime moment, TData state) where TData : struct where T : HR
        {
            return new HD<TData, T>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, T> AsHistory<TData, T>(this Timestamp moment, TData state) where TData : struct where T : HR
        {
            return new HD<TData, T>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, T> AsHistory<TData, T>(this TData state, DateTime moment) where TData : struct where T : HR
        {
            return new HD<TData, T>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HD<TData, T> AsHistory<TData, T>(this TData state, Timestamp moment) where TData : struct where T : HR
        {
            return new HD<TData, T>(moment, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<int, TResult>> CastIterator<TSource, TResult>(this IEnumerable<HD<int, TSource>> source) where TSource : HR where TResult : HR
        {
            foreach (var obj in source) yield return new HD<int, TResult>(obj.Timestamp, obj.Data);
        }

        public static IEnumerable<Pair<HD<T1, R1>, HD<T2, R2>>> Zip<T1, T2, R1, R2>(this IEnumerable<HD<T1, R1>> leftList, IEnumerable<HD<T2, R2>> rightList) where T1 : struct where T2 : struct where R1 : HR where R2 : HR
        {
            using (var leftEnumerator = leftList.GetEnumerator())
            {
                using (var rightEnumerator = rightList.GetEnumerator())
                {
                    bool next1 = leftEnumerator.MoveNext(), next2 = rightEnumerator.MoveNext();
                    if (!next1 || !next2)
                        yield break;

                    var leftCurrent = leftEnumerator.Current;
                    var rightCurrent = rightEnumerator.Current;

                    while (leftEnumerator.Current.CompareTo(rightEnumerator.Current) > 0 && rightEnumerator.MoveNext()) ;

                    IEnumerator nextEnumerator = leftEnumerator;
                    do
                    {
                        if (leftEnumerator.Current.CompareTo(rightEnumerator.Current) <= 0)
                        {
                            leftCurrent = leftEnumerator.Current;
                            rightCurrent = rightEnumerator.Current;
                            nextEnumerator = leftEnumerator;

                            yield return new Pair<HD<T1, R1>, HD<T2, R2>>(leftCurrent, rightCurrent);

                            if (!rightEnumerator.MoveNext())
                                yield break;
                        }
                        else
                        {
                            rightCurrent = rightEnumerator.Current;
                            nextEnumerator = rightEnumerator;
                            yield return new Pair<HD<T1, R1>, HD<T2, R2>>(leftCurrent, rightCurrent);
                        }

                    } while (nextEnumerator.MoveNext());

                    if (nextEnumerator == leftEnumerator)
                    {
                        nextEnumerator = rightEnumerator;
                        do
                        {
                            yield return new Pair<HD<T1, R1>, HD<T2, R2>>(leftCurrent, (HD<T2, R2>)nextEnumerator.Current);
                        } while (nextEnumerator.MoveNext());
                    }
                    else
                    {
                        nextEnumerator = leftEnumerator;
                        do
                        {
                            yield return new Pair<HD<T1, R1>, HD<T2, R2>>((HD<T1, R1>)nextEnumerator.Current, rightCurrent);
                        } while (nextEnumerator.MoveNext());
                    }
                }
            }
        }

        public static IEnumerable<HD<TResult, TResultReferance>> AggregateHistory<TSource, TSourceReference, TResult, TResultReferance>(
            this IEnumerable<HD<TSource, TSourceReference>>[] series, 
            Action<HD<KeyValuePair<int, TSource>, TSourceReference>> start,
            Action<HD<KeyValuePair<int, TSource>, TSourceReference>> append, 
            Func<Timestamp, HD<TResult, TResultReferance>> get, 
            bool skip = true)
            where TSource : struct where TSourceReference : HR where TResult : struct where TResultReferance : HR
        {
            var beginTime = default(Timestamp);

            return series.AggregateHistory(
                item => item.Timestamp != beginTime,
                item => { beginTime = item.Timestamp; start(item); }, 
                append, 
                () => get(beginTime))
           .Skip((skip) ? 1 : 0);
        }

        public static IEnumerable<HD<TResult, TResultReferance>> AggregateHistory<TSource, TSourceReference, TResult, TResultReferance>(
            this IEnumerable<HD<TSource, TSourceReference>>[] series, 
            Func<HD<KeyValuePair<int, TSource>, TSourceReference>, bool> need,
            Action<HD<KeyValuePair<int, TSource>, TSourceReference>> start, 
            Action<HD<KeyValuePair<int, TSource>, TSourceReference>> append,
            Func<HD<TResult, TResultReferance>> get)
            where TSource : struct where TSourceReference : HR where TResult : struct where TResultReferance : HR
        {
            return MergeUtils.Merge(series.Select(
                    (enumerable, enumerableIndex) => enumerable
                        .Select(item => new HD<KeyValuePair<int, TSource>, TSourceReference>(
                            item.Timestamp, new KeyValuePair<int, TSource>(enumerableIndex, item.Data))))
                        .ToArray()
                    )
                    .AggregateHistory(need, start, append, get);
        }


        public static IEnumerable<HD<TResult, TResultReferance>> AggregateHistory<TSource, TSourceReference, TResult, TResultReferance>(
            this IEnumerable<HD<TSource, TSourceReference>> series, 
            Func<HD<TSource, TSourceReference>, bool> need,
            Action<HD<TSource, TSourceReference>> start, 
            Action<HD<TSource, TSourceReference>> append,
            Func<HD<TResult, TResultReferance>> get)
            where TSource : struct where TSourceReference : HR where TResult : struct where TResultReferance : HR
        {
            using (var items = series.GetEnumerator())
            {
                if (items.MoveNext())
                {
                    var current = default(HD<TSource, TSourceReference>);

                    if (need(current = items.Current))
                    {
                        start(current);
                        yield return get();
                    }
                    else
                        start(current);

                    while (items.MoveNext())
                    {
                        if (need(current = items.Current))
                        {
                            yield return get();
                            start(current);
                        }
                        else
                            append(current);
                    }

                    yield return get();
                }
            }
        }

        public static IEnumerable<HD<TResult, TResultReferance>> AggregateHistory<TSource, TSourceReference, TResult, TResultReferance>(
            this IEnumerable<HD<TSource, TSourceReference>>[] series, 
            Action<HD<KeyValuePair<int, TSource>, TSourceReference>> append,
            Func<HD<TResult, TResultReferance>> get)
            where TSource : struct where TSourceReference : HR where TResult : struct where TResultReferance : HR
        {
            return MergeUtils.Merge(series.Select(
                (enumerable, enumerableIndex) => enumerable
                    .Select(item => new HD<KeyValuePair<int, TSource>, TSourceReference>(
                        item.Timestamp, new KeyValuePair<int, TSource>(enumerableIndex, item.Data))))
                    .ToArray()
                )
                .AggregateHistory(append, get);
        }

        public static IEnumerable<HD<TResult, TResultReferance>> AggregateHistory<TSource, TSourceReference, TResult, TResultReferance>(
            this IEnumerable<HD<TSource, TSourceReference>> series, 
            Action<HD<TSource, TSourceReference>> append,
            Func<HD<TResult, TResultReferance>> get)
            where TSource : struct where TSourceReference : HR where TResult : struct where TResultReferance : HR
        {
            using (var items = series.GetEnumerator())
            {
                while (items.MoveNext())
                {
                    append(items.Current);
                    yield return get();
                }
            }
        }
    }
}
