namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Data;

    public static class MergeUtils
    {
        public static IEnumerable<T> Merge<T>(this IEnumerable<T> leftList, IEnumerable<T> rightList) where T : struct, IComparable<T>
        {
            using (var leftEnumerator = leftList.GetEnumerator())
            {
                using (var rightEnumerator = rightList.GetEnumerator())
                {
                    bool leftNext = leftEnumerator.MoveNext(), rightNext = rightEnumerator.MoveNext();
                    if (leftNext || rightNext)
                    {
                        if (!leftNext && rightNext)
                        {
                            do
                            {
                                yield return rightEnumerator.Current;
                            }
                            while (rightEnumerator.MoveNext());
                        }
                        else
                        if (!rightNext && leftNext)
                        {
                            do
                            {
                                yield return leftEnumerator.Current;
                            }
                            while (leftEnumerator.MoveNext());

                            yield break;
                        }
                        else
                        {
                            IEnumerator<T> nextEnumerator = leftEnumerator;
                            do
                            {
                                if (leftEnumerator.Current.CompareTo(rightEnumerator.Current) <= 0)
                                    nextEnumerator = leftEnumerator;
                                else
                                    nextEnumerator = rightEnumerator;

                                yield return nextEnumerator.Current;

                            } while (nextEnumerator.MoveNext());

                            if (nextEnumerator == leftEnumerator)
                                nextEnumerator = rightEnumerator;
                            else
                                nextEnumerator = leftEnumerator;

                            do
                            {
                                yield return nextEnumerator.Current;
                            } while (nextEnumerator.MoveNext());
                        }
                    }
                }
            }
        }

        public static IEnumerable<Pair<T, T>> Zip<T>(this IEnumerable<T> leftList, IEnumerable<T> rightList) where T : struct, IComparable<T>
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
                    IEnumerator<T> nextEnumerator = leftEnumerator;
                    do
                    {
                        if (leftEnumerator.Current.CompareTo(rightEnumerator.Current) <= 0)
                        {
                            leftCurrent = leftEnumerator.Current;
                            rightCurrent = rightEnumerator.Current;
                            nextEnumerator = leftEnumerator;

                            yield return new Pair<T, T>(leftCurrent, rightCurrent);
                        }
                        else
                        {
                            rightCurrent = rightEnumerator.Current;
                            nextEnumerator = rightEnumerator;
                            yield return new Pair<T, T>(leftCurrent, rightCurrent);
                        }

                    } while (nextEnumerator.MoveNext());

                    if (nextEnumerator == leftEnumerator)
                    {
                        nextEnumerator = rightEnumerator;
                        do
                        {
                            yield return new Pair<T, T>(leftCurrent, nextEnumerator.Current);
                        } while (nextEnumerator.MoveNext());
                    }
                    else
                    {
                        nextEnumerator = leftEnumerator;
                        do
                        {
                            yield return new Pair<T, T>(nextEnumerator.Current, rightCurrent);
                        } while (nextEnumerator.MoveNext());
                    }
                }
            }
        }

        public static IEnumerable<T> DistinctMerge<T>(IEnumerable<T> leftList, IEnumerable<T> rightList) where T : struct, IComparable<T>
        {
            using (var leftEnumerator = leftList.GetEnumerator())
            {
                using (var rightEnumerator = rightList.GetEnumerator())
                {
                    T distinctValue = default(T);
                    bool leftNext = leftEnumerator.MoveNext(), rightNext = rightEnumerator.MoveNext();
                    if (leftNext || rightNext)
                    {
                        if (!leftNext && rightNext)
                        {
                            do
                            {
                                if (distinctValue.CompareTo(rightEnumerator.Current) != 0)
                                    yield return distinctValue = rightEnumerator.Current;
                            }
                            while (rightEnumerator.MoveNext());
                        }
                        else
                        if (!rightNext && leftNext)
                        {
                            do
                            {
                                if (distinctValue.CompareTo(leftEnumerator.Current) != 0)
                                    yield return distinctValue = leftEnumerator.Current;
                            }
                            while (leftEnumerator.MoveNext());

                            yield break;
                        }
                        else
                        {
                            IEnumerator<T> nextEnumerator = leftEnumerator;
                            do
                            {
                                if (leftEnumerator.Current.CompareTo(rightEnumerator.Current) <= 0)
                                    nextEnumerator = leftEnumerator;
                                else
                                    nextEnumerator = rightEnumerator;

                                if (distinctValue.CompareTo(nextEnumerator.Current) != 0)
                                    yield return distinctValue = nextEnumerator.Current;

                            } while (nextEnumerator.MoveNext());

                            if (nextEnumerator == leftEnumerator)
                                nextEnumerator = rightEnumerator;
                            else
                                nextEnumerator = leftEnumerator;

                            do
                            {
                                if (distinctValue.CompareTo(nextEnumerator.Current) != 0)
                                    yield return distinctValue = nextEnumerator.Current;
                            } while (nextEnumerator.MoveNext());
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> Merge<T>(params IEnumerable<T>[] lists) where T : struct, IComparable<T>
        {
            IEnumerable<T> result = lists[0];
            for (int i = 1; i < lists.Length; i++)
            {
                result = Merge(result, lists[i]);
            }

            return result;
        }

        public static IEnumerable<T> Merge<T>(Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<T>> mergeFunction, params IEnumerable<T>[] lists) where T : struct, IComparable<T>
        {
            IEnumerable<T> result = lists[0];
            for (int i = 1; i < lists.Length; i++)
            {
                result = mergeFunction(result, lists[i]);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> GetEnumerable<T>(this IEnumerator<T> enumerator)
        {
            yield return enumerator.Current;
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

}
