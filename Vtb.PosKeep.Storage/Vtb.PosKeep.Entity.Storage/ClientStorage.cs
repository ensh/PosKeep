namespace Vtb.PosKeep.Entity.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vtb.PosKeep.Entity.Data;

    public class ClientStorage : BasePackStorage<ClientKey, ClientKey>
    {
        public ClientStorage() : base() { }
        public ClientStorage(int size) : base(size) { }
    }
}
