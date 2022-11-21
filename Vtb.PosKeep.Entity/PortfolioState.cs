namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Collections.Generic;

    using Vtb.PosKeep.Entity.Key;

    public struct PortfolioState
    {
        public readonly Item[] Items;

        public PortfolioState(Item[] items)
        {
            Items = items;
        }

        public struct Item
        {
            public readonly PositionKey PositionKey;
            public readonly Position Position;

            public Item(PositionKey positionKey, int position)
                : this(positionKey, (Position)position)
            {
            }

            public Item(PositionKey positionKey, Position position)
            {
                PositionKey = positionKey;
                Position = position;
            }
        }
    }
}
