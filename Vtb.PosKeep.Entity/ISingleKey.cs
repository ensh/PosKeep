namespace Vtb.PosKeep.Entity
{
    public interface ISingleKey<KeyType, ReferenceType>  where KeyType : struct
    {
        int GetHashCode();
        bool Equals(object obj);
        string ToString();
    }
}
