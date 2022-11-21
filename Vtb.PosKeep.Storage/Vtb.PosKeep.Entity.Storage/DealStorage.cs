namespace Vtb.PosKeep.Entity.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    public class DealStorage : HistoryStorage<DealKey, Deal, DR>
    {
        public DealStorage(IBlockStorageFactory<HD<Deal, DR>> storageFactory) : base(storageFactory)
        {
            AccountInstruments = new ConcurrentDictionary<TradeAccountKey, StorageBag<TradeInstrumentKey>>();
            AccountPlaces = new ConcurrentDictionary<AccountKey, StorageBag<int>>();
            DealCodes = new ConcurrentDictionary<string, KeyValuePair<DealKey, Deal>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void DoOnNewKey(DealKey key)
        {
            AccountPlaces.AddOrUpdate(key.Account.Account, _ => new StorageBag<int>(new [] { key.Account.Place } ),
            (k, places) =>
            {
                using (var l = places.WriteLocker())
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

            AccountInstruments.AddOrUpdate(key.Account, _ => new StorageBag<TradeInstrumentKey>(new [] { key.Instrument} ),
            (k, instruments) =>
            {
                using (var l = instruments.WriteLocker())
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
        protected override IEnumerable<HD<Deal, DR>> DoOnBeforeUpdate(DealKey key, IEnumerable<HD<Deal, DR>> deals)
        {
            foreach (var deal in deals)
            {
                DealCodes.TryAdd(deal.Data.Code, new KeyValuePair<DealKey, Deal>(key, deal.Data));
                yield return deal;
            }
        }

        private ConcurrentDictionary<TradeAccountKey, StorageBag<TradeInstrumentKey>> AccountInstruments;
        private ConcurrentDictionary<AccountKey, StorageBag<int>> AccountPlaces;
        private ConcurrentDictionary<string, KeyValuePair<DealKey, Deal>> DealCodes;

        private static readonly TradeInstrumentKey[] emptyInstruments = new TradeInstrumentKey[0];
        private static readonly DealKey[] emptyDeals = new DealKey[0];

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
                        if (AccountInstruments.TryGetValue(tkey, out var instruments))
                        {
                            using (var instrumentLock = instruments.ReadLocker())
                            {
                                foreach (var ikey in instruments.Data)
                                    yield return result(tkey, ikey);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TradeInstrumentKey> Instruments(AccountKey client)
        {
            return Instruments(client, (tkey, ikey) => ikey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<DealKey> DealKeys(AccountKey client)
        {
            return Instruments(client, (tkey, ikey) => new DealKey(tkey, ikey));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ClientFirstTime(AccountKey AccountKey)
        {
            var result = Timestamp.MaxValue;
            foreach (var dealKey in DealKeys(AccountKey))
            {
                result = Math.Min(result, Items(dealKey, 0).First().Timestamp);
            }

            return result;
        }

    }
}
