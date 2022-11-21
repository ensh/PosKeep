namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Runtime.CompilerServices;
    public static class TimestampUtils
    {
        public static readonly DateTime BaseTimestamp = new DateTime(1990, 1, 1, 0, 0, 0);
        //1 января 1970 00:00:00 по UTC (эпохи Unix).
        public static DateTime DateStart = new DateTime(1970, 1, 1, 0, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsFromDateTime(this DateTime value)
        {
            return (int)value.Subtract(BaseTimestamp).TotalSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime DateTimeFromSeconds(this int value)
        {
            return BaseTimestamp.AddSeconds(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime DateTimeFromMilliseconds(this long value)
        {
            return DateStart.AddMilliseconds(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp Upper(this Timestamp time, int period)
        {
            var t = (int)time;
            var rem = t % period;
            return (rem == 0) ? time : (Timestamp)(t - rem + period);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp Down(this Timestamp time, int period)
        {
            var t = (int)time;
            return (t - t % period);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AsUtc(this Timestamp time)
        {
            return (long)((DateTime)time).Subtract(DateStart).TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AsUtc(this DateTime time)
        {
            return (long)time.Subtract(DateStart).TotalMilliseconds;
        }
    }

    [System.Diagnostics.DebuggerDisplay("DateTime = {ToString(),nq}")]
    public struct Timestamp : IEquatable<Timestamp>, IComparable<Timestamp>, IComparable
    {
        Timestamp(int value) { m_value = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Timestamp t)
        {
            return t.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Timestamp(int t)
        {
            return new Timestamp(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Timestamp(DateTime t)
        {
            return new Timestamp(TimestampUtils.SecondsFromDateTime(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DateTime(Timestamp t)
        {
            return TimestampUtils.DateTimeFromSeconds(t.m_value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int operator +(Timestamp x, int y)
        {
            return x.m_value + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int operator -(Timestamp x, Timestamp y)
        {
            return x.m_value - y.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Timestamp x, Timestamp y)
        {
            return x.m_value > y.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Timestamp x, Timestamp y)
        {
            return x.m_value < y.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Timestamp x, Timestamp y)
        {
            return x.m_value >= y.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Timestamp x, Timestamp y)
        {
            return x.m_value <= y.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Timestamp x, Timestamp y)
        {
            return x.m_value == y.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Timestamp x, Timestamp y)
        {
            return x.m_value != y.m_value;
        }

        public override string ToString()
        {
            return TimestampUtils.DateTimeFromSeconds(m_value).ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return m_value;
        }

        public override bool Equals(object obj)
        {
            return Equals((Timestamp)obj);
        }

        int m_value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Timestamp other)
        {
            return m_value == other.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Timestamp other)
        {
            //var xy = m_value - other.m_value;
            //var yx = other.m_value - m_value;
            //return (xy >> 31) | ((uint)yx >> 31);

            return m_value - other.m_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object obj)
        {
            return CompareTo((Timestamp)obj);
        }

        public DateTime Date
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return TimestampUtils.DateTimeFromSeconds(m_value).Date; }
        }

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_value == default(int); }
        }

        public static Timestamp MinValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Timestamp(0);
            }
        }

        public static Timestamp MaxValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Timestamp(int.MaxValue);
            }
        }
    }
}
