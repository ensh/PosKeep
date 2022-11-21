namespace Vtb.PosKeep.Entity.Data
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public enum DealKind : byte
    {
        Trade = 0, Move = 1, Repo = 2, Swap = 4, 
    };

    public struct DealType
    {
        public static readonly DealType Buy = new DealType((ushort)ValueTokenType.Buy);
        public static readonly DealType Sell = new DealType((ushort)ValueTokenType.Sell);
        public static readonly DealType Income = new DealType((int)ValueTokenType.Income + ((int)DealKind.Move<<8));
        public static readonly DealType Outcome = new DealType((int)ValueTokenType.Outcome + ((int)DealKind.Move << 8));
        public static readonly DealType RepoBuy = new DealType((int)ValueTokenType.Buy + ((int)DealKind.Repo << 8));
        public static readonly DealType RepoSell = new DealType((int)ValueTokenType.Sell + ((int)DealKind.Repo << 8));
        public static readonly DealType SwapBuy = new DealType((int)ValueTokenType.Buy + ((int)DealKind.Swap << 8));
        public static readonly DealType SwapSell = new DealType((int)ValueTokenType.Sell + ((int)DealKind.Swap << 8));

        private readonly ushort _type;

        public DealType(ushort type)
        {
            _type = type;
        }

        public DealType(byte type) : this((ushort)type) { }

        public static implicit operator byte (DealType type)
        {
            return (byte)type._type;
        }

        public static implicit operator ushort(DealType type)
        {
            return type._type;
        }

        public static implicit operator ValueTokenType(DealType type)
        {
            return (ValueTokenType)(byte)type._type;
        }

        public static implicit operator DealKind(DealType type)
        {
            return (DealKind)(byte)(type._type >> 8);
        }

        public static implicit operator DealType(byte type)
        {
            return new DealType(type);
        }

        public static implicit operator DealType(ushort type)
        {
            return new DealType(type);
        }

        public DealType Inverse()
        {
            switch ((byte)_type)
            {
                case (byte)ValueTokenType.Buy:
                    return (ushort)((_type ^ (uint)ValueTokenType.Buy) | (uint)ValueTokenType.Sell);
                case (byte)ValueTokenType.Sell:
                    return (ushort)((_type ^ (uint)ValueTokenType.Sell) | (uint)ValueTokenType.Buy);
                case (byte)ValueTokenType.Income:
                    return (ushort)((_type ^ (uint)ValueTokenType.Income) | (uint)ValueTokenType.Outcome);
                case (byte)ValueTokenType.Outcome:
                    return (ushort)((_type ^ (uint)ValueTokenType.Outcome) | (uint)ValueTokenType.Income);
            }

            return 0;
        }

        public override string ToString()
        {
            return string.Concat(((DealKind)this).ToString(), "-", ((ValueTokenType)this).ToString());
        }

    }

    public struct Deal : IEquatable<Deal>, IEntityPoolSubject
    {
        private readonly int Number;
        public string Code { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => s_Code[Number]; }
        public DealType Type { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => s_Type[Number]; }
        public ValueToken Volume { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new ValueToken(s_Type[Number], s_Volume[Number]); }
        public ValueToken Quantity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new ValueToken(s_Type[Number], s_Quantity[Number]); }
        public decimal Comission { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => s_Comission[Number]; }

        public bool IsNull { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Number == 0; } }

        public ValueToken Volume1 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return new ValueToken(s_Type[Number - 1], s_Volume[Number - 1]); } }
        public ValueToken Quantity1 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return new ValueToken(s_Type[Number-1], s_Quantity[Number-1]); } }

        public ValueToken Volume2 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return new ValueToken(s_Type[Number], s_Volume[Number]); } }
        public ValueToken Quantity2 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return new ValueToken(s_Type[Number], s_Quantity[Number]); } }

        public static int Create(string code, DealType type, decimal volume, decimal quantity, decimal comission = 0m)
        {
            int i = EntityPool<DP>.Next();
            s_Code[i] = code;  s_Type[i] = type; s_Volume[i] = volume; s_Quantity[i] = quantity; s_Comission[i] = comission;

            return i; 
        }

        public static int Create(string code, DealType type, decimal volume1, decimal quantity1, decimal volume2, decimal quantity2, decimal comission = 0m)
        {
            int i = EntityPool<DP>.Next(2);

            i--;
            s_Code[i] = code; s_Type[i] = type; s_Volume[i] = volume1; s_Quantity[i] = quantity1; 

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() { return Number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) { return Equals((Deal)obj); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Deal other)
        {
            return Number == other.Number || Code == other.Code;
            //&& Type == other.Type && Volume == other.Volume && Quantity == other.Quantity && Comission == other.Comission );
        }

        public decimal Price
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (s_Quantity[Number] == 0m)
                    return s_Volume[Number];
                return s_Volume[Number] / s_Quantity[Number];
            }
        }

        public override string ToString()
        {
            return string.Concat("Number: ", Number.ToString(), ", Quantity: ", Quantity.ToString(), ", Volume: ", Volume.ToString(), ", Comission: ", Comission.ToString());
        }

        private static string[] s_Code;
        private static DealType[] s_Type;
        private static decimal[] s_Volume;
        private static decimal[] s_Quantity;
        private static decimal[] s_Comission;

        public static void Init(int size)
        {
            s_Code = new string[size];
            s_Type = new DealType[size];
            s_Volume = new decimal[size];
            s_Quantity = new decimal[size];
            s_Comission = new decimal[size];

            EntityPool<DP>.Reset();
        }
        
        public static int LastNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => EntityPool<DP>.LastNumber;
        }
        
        private Deal(int number) { Number = number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free()
        {
            switch (Type >> 8)
            {
                case (int)DealKind.Repo:
                case (int)DealKind.Swap:
                    EntityPool<DP>.Free(Number -1);
                    break;
            }
            EntityPool<DP>.Free(Number);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Deal deal) { return deal.Number; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Deal(int dealNumber) { return new Deal(dealNumber); }
    }
}
