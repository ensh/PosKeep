
namespace Vtb.PosKeep.Entity
{
    public interface IComplexKey<KeyType, NextKeyType, ReferenceType> 
        where KeyType : struct
    {
        int GetHashCode();
        bool Equals(object obj);
        string ToString();
    }
}
