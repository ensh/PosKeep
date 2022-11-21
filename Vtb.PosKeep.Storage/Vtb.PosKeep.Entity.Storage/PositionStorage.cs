namespace Vtb.PosKeep.Entity.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    public partial class PositionStorage : HistoryStorage<PositionKey, Position, PR>
    {
        public PositionStorage(IBlockStorageFactory<HD<Position, PR>> blockStorageFactory) : base(blockStorageFactory)
        {
            AccountInstruments = new ConcurrentDictionary<TradeAccountKey, StorageBag<TradeInstrumentKey>>();
            AccountPlaces = new ConcurrentDictionary<AccountKey, StorageBag<int>>();

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void DoOnNewKey(PositionKey key)
        {
            AccountPlaces.AddOrUpdate(key.Account.Account,
                _ => new StorageBag<int>(new[] { key.Account.Place }),
                (k, places) =>
                {
                    using (var placesLock = places.WriteLocker())
                    {
                        if (!places.Data.Contains(key.Account.Place))
                        {
                            var data = new int[places.Data.Length + 1];
                            Array.Copy(places.Data, data, places.Data.Length);
                            data[places.Data.Length] = key.Account.Place;
                            return new StorageBag<int>(data, places.Lock);
                        }

                        return places;
                    }
                });


            AccountInstruments.AddOrUpdate(key.Account,
                _ => new StorageBag<TradeInstrumentKey>(new[] { key.Instrument }),
                (k, instruments) =>
                {
                    using (var instrumentsLock = instruments.WriteLocker())
                    {
                        if (!instruments.Data.Contains(key.Instrument))
                        {
                            var data = new TradeInstrumentKey[instruments.Data.Length + 1];
                            Array.Copy(instruments.Data, data, instruments.Data.Length);
                            data[instruments.Data.Length] = key.Instrument;
                            return new StorageBag<TradeInstrumentKey>(data, instruments.Lock);
                        }

                        return instruments;
                    }
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override IEnumerable<HD<Position, PR>> DoOnBeforeUpdate(PositionKey key, IEnumerable<HD<Position, PR>> data)
        {
            var from = data.First(); // фейковый элемент, для даты начала

            // удалить все, что позже from
            // почистить индексы
            CutItems(key, from, p => p.Data.Free());

            return data.Skip(1); // пропустить элемент
        }

        protected ConcurrentDictionary<TradeAccountKey, StorageBag<TradeInstrumentKey>> AccountInstruments;
        protected ConcurrentDictionary<AccountKey, StorageBag<int>> AccountPlaces;

        private static readonly TradeInstrumentKey[] emptyInstruments = new TradeInstrumentKey[0];
        private static readonly PositionKey[] emptyPositions = new PositionKey[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<TKey> Instruments<TKey>(AccountKey client, Func<TradeAccountKey, TradeInstrumentKey, TKey> result) where TKey : struct
        {
            if (AccountPlaces.TryGetValue(client, out var places))
            {
                using (var placeLock = places.ReadLocker())
                {
                    foreach (var p in places.Data)
                    {
                        var tkey = new TradeAccountKey(client, p);
                        foreach (var rvalue in Instruments(tkey, result))
                            yield return rvalue;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<TKey> Instruments<TKey>(TradeAccountKey tkey, Func<TradeAccountKey, TradeInstrumentKey, TKey> result) where TKey : struct
        {
            if (AccountInstruments.TryGetValue(tkey, out var instruments))
            {
                using (var l = instruments.ReadLocker())
                {
                    foreach (var ikey in instruments.Data)
                        yield return result(tkey, ikey);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TradeInstrumentKey> Instruments(AccountKey client)
        {
            return Instruments(client, (tkey, ikey) => ikey);            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TradeInstrumentKey> Instruments(TradeAccountKey tradeAccountKey)
        {
            return Instruments(tradeAccountKey, (tkey, ikey) => ikey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<PositionKey> PositionKeys(AccountKey client)
        {
            return Instruments(client, (tkey, ikey) => PositionKey.Create(tkey, ikey));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<PositionKey> PositionKeys(TradeAccountKey tradeAccountKey)
        {
            return Instruments(tradeAccountKey, (tkey, ikey) => PositionKey.Create(tkey, ikey));
        }

        public IEnumerable<KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>> Items(AccountKey key,
            Timestamp from, Timestamp to)
        {
            foreach (var posKey in PositionKeys(key))
            {
                yield return new KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>(posKey,
                    Items(posKey, from, to, -1));
            }
        }

        public IEnumerable<KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>> Items(AccountKey key, Timestamp from)
        {
            foreach (var posKey in PositionKeys(key))
            {
                yield return new KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>(posKey,
                    Items(posKey, from, Timestamp.MaxValue, -1));
            }
        }

        public IEnumerable<KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>> Items(TradeAccountKey key,
            Timestamp from, Timestamp to)
        {
            foreach (var posKey in PositionKeys(key))
            {
                yield return new KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>(posKey,
                    Items(posKey, from, to, -1));
            }
        }

        public IEnumerable<KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>> Items(TradeAccountKey key, Timestamp from)
        {
            foreach (var posKey in PositionKeys(key))
            {
                yield return new KeyValuePair<PositionKey, IEnumerable<HD<Position, PR>>>(posKey,
                    Items(posKey, from, Timestamp.MaxValue, -1));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ClientFirstTime(AccountKey accountKey)
        {
            var result = Timestamp.MaxValue;
            foreach (var positionKey in PositionKeys(accountKey))
            {
                result = Math.Min(result, First(positionKey).Timestamp);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ClientLastTime(AccountKey accountKey)
        {
            var result = Timestamp.MinValue;
            foreach (var positionKey in PositionKeys(accountKey))
            {
                result = Math.Max(result, Last(positionKey).Timestamp);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ClientFirstTime(TradeAccountKey tradeAccountKey)
        {
            var result = Timestamp.MaxValue;
            foreach (var positionKey in PositionKeys(tradeAccountKey))
            {
                result = Math.Min(result, First(positionKey).Timestamp);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ClientLastTime(TradeAccountKey tradeAccountKey)
        {
            var result = Timestamp.MinValue;
            foreach (var positionKey in PositionKeys(tradeAccountKey))
            {
                result = Math.Max(result, Last(positionKey).Timestamp);
            }

            return result;
        }
    }

    public static class PositionStorageUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<HD<Position.Recalc, T>> Aggregate<T>(this IEnumerable<HD<Position.Recalc, T>> positions, int period) where T : HR
        {
            using (var aggregatePositionEnumerator = positions.NormalizePeriod(period).GetEnumerator())
            {
                var current = new HD<Position.Recalc, T>(Timestamp.MaxValue, default(Position.Recalc));
                if (aggregatePositionEnumerator.MoveNext())
                {
                    do
                    {
                        // готовы вычислить новую позицию, но состояние предыдущее
                        if (current.Timestamp < aggregatePositionEnumerator.Current.Timestamp)
                        {
                            yield return current; // предыдущее состояние
                            // сброс агрегации
                            aggregatePositionEnumerator.Current.Data.ResetAggregate();
                        }

                        aggregatePositionEnumerator.MoveNext(); // пересчитали позицию
                        current = aggregatePositionEnumerator.Current; // запомнить состояние для следующего шага

                    } while (aggregatePositionEnumerator.MoveNext());
                }
                yield return current;
            }
        }
    }
}
