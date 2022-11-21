using System;
using System.Collections.Generic;

namespace Vtb.PosKeep.Server.Controllers
{
    public struct CacheKey : IEquatable<CacheKey>
    {
        public readonly Type ControllerType;
        public readonly object InternalKey;

        public CacheKey(Type controllerType, object internalKey)
        {
            ControllerType = controllerType;
            InternalKey = internalKey;
        }

        public bool Equals(CacheKey other)
        {
            return ControllerType == other.ControllerType && InternalKey.Equals(other.InternalKey);
        }

        public override bool Equals(object obj)
        {
            return base.Equals((CacheKey)obj);
        }

        public override int GetHashCode()
        {
            return ControllerType.GetHashCode() ^ InternalKey.GetHashCode();
        }
    }
}