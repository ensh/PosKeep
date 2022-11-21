
namespace Vtb.PosKeep.Entity.Storage
{
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    // PSR - PortfolioStateReference
    /// <summary>
    /// PortfolioStateReference
    /// </summary>
    public abstract class PSR : HR { }

    public class PortfolioStateStorage : HistoryStorage<AccountKey, PortfolioState, PSR>
    {
        public PortfolioStateStorage(IBlockStorageFactory<HD<PortfolioState, PSR>> blockStorageFactory) : base(blockStorageFactory)
        {

        }
    }
}
