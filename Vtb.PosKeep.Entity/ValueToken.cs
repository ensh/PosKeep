namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Runtime.CompilerServices;

    public static class ValueTokenUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToBuy(this decimal value)
        {
            return new ValueToken((byte)ValueTokenType.Buy, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToSell(this decimal value)
        {
            return new ValueToken((byte)ValueTokenType.Sell, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToBuy(this int value)
        {
            return new ValueToken((byte)ValueTokenType.Buy, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToSell(this int value)
        {
            return new ValueToken((byte)ValueTokenType.Sell, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToIncome(this decimal value)
        {
            return new ValueToken((byte)ValueTokenType.Income, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToOutcome(this decimal value)
        {
            return new ValueToken((byte)ValueTokenType.Outcome, value);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static ValueToken ToCommiss(this decimal value)
        //{
        //    return new ValueToken((byte)ValueTokenType.Commiss, value);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToNull(this decimal value)
        {
            return new ValueToken((byte)ValueTokenType.Null, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToIncome(this int value)
        {
            return new ValueToken((byte)ValueTokenType.Income, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToOutcome(this int value)
        {
            return new ValueToken((byte)ValueTokenType.Outcome, value);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static ValueToken ToCommiss(this int value)
        //{
        //    return new ValueToken((byte)ValueTokenType.Commiss, value);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken ToNull(this int value)
        {
            return new ValueToken((byte)ValueTokenType.Null, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken Reverse(this ValueToken value)
        {
            return !value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken SetValue(this ValueToken token, int value)
        {
            return new ValueToken(token.Type, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken SetValue(this ValueToken token, decimal value)
        {
            return new ValueToken(token.Type, value);
        }
    }

    public enum ValueTokenType: byte { Null = 0, Buy = 1, Sell = 2, Income = 4, Outcome = 8, Money = 16,
    };

    public struct ValueToken : IEquatable<ValueToken>
    {
        public readonly byte Type;
        public readonly decimal Value;

        public static ValueToken Null
        {
            get
            {
                return new ValueToken((byte)ValueTokenType.Null, 0);
            }
        }

        public ValueToken(byte buy, decimal value)
        {
            Type = buy; Value = value;
        }

        public ValueToken(ValueToken value)
        {
            Type = value.Type; Value = value.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ValueToken other)
        {
            return (Value == 0 && other.Value == 0) || (Type == other.Type && Value == other.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return Equals((ValueToken)obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Type;
        }

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value == 0 || Type == (byte)ValueTokenType.Null;
            }
        }

        public override string ToString()
        {
            if (Value != 0)
            {
                var stype = "";
                switch (Type)
                {
                    case (byte)ValueTokenType.Buy:
                        stype = "<Buy>";
                        break;
                    case (byte)ValueTokenType.Sell:
                        stype = "<Sell>";
                        break; 
                    case (byte)ValueTokenType.Income:
                        stype = "<In>";
                        break;
                    case (byte)ValueTokenType.Outcome:
                        stype = "<Out>";
                        break;
                }
                return string.Concat(stype, Value.ToString());
            }
            return string.Concat("0");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken operator +(ValueToken x, ValueToken y)
        {
            if (x.Type == y.Type)
                return new ValueToken(x.Type, x.Value + y.Value);
            if (x.IsNull)
                return y;
            else
                if (y.IsNull)
                    return x;

            if (y.Value > x.Value)
            {
                var t = y;
                y = x;
                x = t;
            }

            return new ValueToken(x.Type, (x.CoType(y)) ? x.Value + y.Value : x.Value - y.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken operator *(ValueToken x, decimal y)
        {
            return new ValueToken(x.Type, x.Value * y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken operator *(decimal x, ValueToken y) 
        {
            return new ValueToken(y.Type, y.Value * x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken operator /(ValueToken x, decimal y)
        {
            return new ValueToken(x.Type, x.Value / y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal operator /(ValueToken x, ValueToken y)
        {
            return x.Value / y.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ValueToken x, ValueToken y)
        {
            return x.Equals(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ValueToken x, ValueToken y)
        {
            return !x.Equals(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator decimal(ValueToken value)
        {
            return value.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueToken operator !(ValueToken token)
        {
            switch (token.Type)
            {
                case (byte)ValueTokenType.Buy:
                    return new ValueToken((byte)ValueTokenType.Sell, token.Value);
                case (byte)ValueTokenType.Sell:
                    return new ValueToken((byte)ValueTokenType.Buy, token.Value);
                case (byte)ValueTokenType.Income:
                    return new ValueToken((byte)ValueTokenType.Outcome, token.Value);
                case (byte)ValueTokenType.Outcome:
                    return new ValueToken((byte)ValueTokenType.Income, token.Value);
                default:
                    return token;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CoType(ValueToken token)
        {
            return CoType(token.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CoType(byte tokenType)
        {
            return IsNull || Type == tokenType ||
                (Type == (byte)ValueTokenType.Buy && tokenType == (byte)ValueTokenType.Income) ||
                (Type == (byte)ValueTokenType.Sell && tokenType == (byte)ValueTokenType.Outcome);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueToken ToMoney()
        {
            switch (Type)
            {
                case (byte)ValueTokenType.Buy:
                    return new ValueToken((byte)ValueTokenType.Outcome, Value);
                case (byte)ValueTokenType.Sell:
                    return new ValueToken((byte)ValueTokenType.Income, Value);
                default:
                    return this;
            }
        }

        public decimal ForCost
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (Type)
                {
                    case (byte)ValueTokenType.Buy:
                    case (byte)ValueTokenType.Outcome:
                        return Value;
                    case (byte)ValueTokenType.Sell:
                    case (byte)ValueTokenType.Income:
                        return -Value;
                    default:
                        return Value;
                }
            }
        }

        public decimal ForProfit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (Type)
                {
                    case (byte)ValueTokenType.Buy:
                    case (byte)ValueTokenType.Outcome:
                        return -Value;
                    case (byte)ValueTokenType.Sell:
                    case (byte)ValueTokenType.Income:
                        return Value;
                    default:
                        return Value;
                }
            }
        }
    }
}
