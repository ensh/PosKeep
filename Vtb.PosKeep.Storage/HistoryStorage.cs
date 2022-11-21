namespace Vtb.PosKeep.Entity.Storage
{
    using System.Collections.Generic;

    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    public class HistoryStorage<KeyType, DataType, ReferenceType> : BaseStorage<KeyType, HD<DataType, ReferenceType>> where DataType : struct where KeyType : struct where ReferenceType : HR
    {
        public HistoryStorage(IBlockStorageFactory<HD<DataType, ReferenceType>> blockStorageFactory) : base(blockStorageFactory) {  }

        public IEnumerable<HD<DataType, ReferenceType>> Items(KeyType key, Timestamp from, Timestamp to, int prev = 0)
        {
            var fromHistory = new HD<DataType, ReferenceType>(from);
            var toHistory = new HD<DataType, ReferenceType>(to);

            return Items(key, fromHistory, toHistory, prev);
        }
    }
}
