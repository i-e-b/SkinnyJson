using System;
using System.Linq;

namespace SkinnyJson
{
    /// <summary>
    /// Holds a representation of various numeric types.
    /// Handles casting to target types
    /// </summary>
    internal class WideNumber : IConvertible
    {
        private readonly double _doubleValue;
        private readonly bool   _doubleOk;

        private readonly decimal _decimalValue;
        private readonly bool    _decimalOk;

        private readonly ulong _ulongValue;
        private readonly bool  _ulongOk;

        private readonly long _longValue;
        private readonly bool _longOk;

        private readonly string _original;

        /// <summary>
        /// Try to parse a string as a range of wide number types.
        /// Returns true if at least one type parsed successfully.
        /// </summary>
        public static bool TryParse(string str, out WideNumber result)
        {
            result = new WideNumber(str);
            return result._doubleOk || result._decimalOk || result._ulongOk || result._longOk;
        }

        private WideNumber(string str)
        {
            _original = str;
            _doubleOk = double.TryParse(str, out _doubleValue);
            _decimalOk = decimal.TryParse(str, out _decimalValue);
            _ulongOk = ulong.TryParse(str, out _ulongValue);
            _longOk = long.TryParse(str, out _longValue);
        }

        /// <summary>
        /// Try to cast this WideNumber type to a primitive value.
        /// Returns null if the cast is not supported.
        /// </summary>
        /// <param name="type">Target type</param>
        /// <param name="precisionLoss">Set to true if the cast is from a floating point to a fixed point or integer value; or from a fixed point to integer value</param>
        /// <returns></returns>
        public object? CastTo(Type? type, out bool precisionLoss)
        {
            precisionLoss = false;
            if (type is null) return null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments().FirstOrDefault() ?? throw new Exception("Invalid type definition: nullable wrapper with no internal type defined");

            if (type == typeof(sbyte))
            {
                if (_longOk) return (sbyte)_longValue;
                if (!_decimalOk) return null;
                precisionLoss = true;
                return (sbyte)_decimalValue;
            }

            if (type == typeof(short))
            {
                if (_longOk) return (short)_longValue;
                if (!_decimalOk) return null;
                precisionLoss = true;
                return (short)_decimalValue;
            }

            if (type == typeof(int))
            {
                if (_longOk) return (int)_longValue;
                if (!_decimalOk) return null;
                precisionLoss = true;
                return (int)_decimalValue;
            }

            if (type == typeof(long))
            {
                if (_longOk) return _longValue;
                if (!_decimalOk) return null;
                precisionLoss = true;
                return (long)_decimalValue;
            }

            if (type == typeof(byte))
            {
                if (_ulongOk) return (byte)_ulongValue;
                return null;
            }

            if (type == typeof(ushort))
            {
                if (_ulongOk) return (ushort)_ulongValue;
                return null;
            }

            if (type == typeof(uint))
            {
                if (_ulongOk) return (uint)_ulongValue;
                return null;
            }

            if (type == typeof(ulong))
            {
                if (_ulongOk) return _ulongValue;
                return null;
            }

            if (type == typeof(decimal))
            {
                if (_decimalOk) return _decimalValue;
                return null;
            }

            if (type == typeof(float))
            {
                if (_doubleOk) return (float)_doubleValue;
                if (!_decimalOk) return null;
                precisionLoss = true;
                return (float)_decimalValue;
            }

            if (type == typeof(double))
            {
                if (_doubleOk) return _doubleValue;
                if (!_decimalOk) return null;
                precisionLoss = true;
                return (double)_decimalValue;
            }

            if (type == typeof(string))
            {
                return _original;
            }

            return null;
        }

        public static implicit operator long(WideNumber src)
        {
            return src.ToLong();
        }

        public static implicit operator double(WideNumber src)
        {
            return src.ToDouble();
        }

        public override string ToString() => _original;

        public long ToLong()
        {
            if (_longOk) return _longValue;
            if (_doubleOk) return (long)_doubleValue;
            if (_decimalOk) return (long)_decimalValue;
            throw new Exception("Could not convert numeric value to 'long' type");
        }

        public double ToDouble()
        {
            if (_doubleOk) return _doubleValue;
            if (_decimalOk) return (double)_decimalValue;
            if (_longOk) return _longValue;
            throw new Exception("Could not convert numeric value to 'double' type");
        }

        #region IConvertable

        public TypeCode GetTypeCode()
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean(IFormatProvider? provider)
        {
            if (_longOk) return _longValue != 0;
            if (_doubleOk) return (_doubleValue != 0);
            if (_decimalOk) return (_decimalValue != 0);
            return !string.IsNullOrEmpty(_original);
        }

        public byte ToByte(IFormatProvider? provider)
        {
            if (_longOk) return (byte)_longValue;
            if (_doubleOk) return (byte)_doubleValue;
            if (_decimalOk) return (byte)_decimalValue;
            return 0;
        }

        public char ToChar(IFormatProvider? provider)
        {
            if (_longOk) return (char)_longValue;
            if (_doubleOk) return (char)_doubleValue;
            if (_decimalOk) return (char)_decimalValue;
            return (char)0;
        }

        public DateTime ToDateTime(IFormatProvider? provider)
        {
            if (_longOk) return new DateTime(ticks: _longValue);
            if (_doubleOk) return new DateTime(ticks: (long)_doubleValue);
            if (_decimalOk) return new DateTime(ticks: (long)_decimalValue);
            return DateTime.MinValue;
        }

        public decimal ToDecimal(IFormatProvider? provider)
        {
            if (_longOk) return _longValue;
            if (_doubleOk) return (decimal)_doubleValue;
            if (_decimalOk) return _decimalValue;
            return 0m;
        }

        public double ToDouble(IFormatProvider? provider)
        {
            if (_longOk) return _longValue;
            if (_doubleOk) return _doubleValue;
            if (_decimalOk) return (double)_decimalValue;
            return 0;
        }

        public short ToInt16(IFormatProvider? provider)
        {
            if (_longOk) return (short)_longValue;
            if (_doubleOk) return (short)_doubleValue;
            if (_decimalOk) return (short)_decimalValue;
            return 0;
        }

        public int ToInt32(IFormatProvider? provider)
        {
            if (_longOk) return (int)_longValue;
            if (_doubleOk) return (int)_doubleValue;
            if (_decimalOk) return (int)_decimalValue;
            return 0;
        }

        public long ToInt64(IFormatProvider? provider)
        {
            if (_longOk) return _longValue;
            if (_doubleOk) return (long)_doubleValue;
            if (_decimalOk) return (long)_decimalValue;
            return 0;
        }

        public sbyte ToSByte(IFormatProvider? provider)
        {
            if (_longOk) return (sbyte)_longValue;
            if (_doubleOk) return (sbyte)_doubleValue;
            if (_decimalOk) return (sbyte)_decimalValue;
            return 0;
        }

        public float ToSingle(IFormatProvider? provider)
        {
            if (_longOk) return _longValue;
            if (_doubleOk) return (float)_doubleValue;
            if (_decimalOk) return (float)_decimalValue;
            return 0;
        }

        public string ToString(IFormatProvider? provider)
        {
            return _original;
        }

        public object ToType(Type conversionType, IFormatProvider? provider)
        {
            if (conversionType == typeof(string)) return _original;
            return CastTo(conversionType, out _) ?? throw new Exception($"Cannot cast numeric type to {conversionType.Name}");
        }

        public ushort ToUInt16(IFormatProvider? provider)
        {
            if (_longOk) return (ushort)_longValue;
            if (_doubleOk) return (ushort)_doubleValue;
            if (_decimalOk) return (ushort)_decimalValue;
            return 0;
        }

        public uint ToUInt32(IFormatProvider? provider)
        {
            if (_longOk) return (uint)_longValue;
            if (_doubleOk) return (uint)_doubleValue;
            if (_decimalOk) return (uint)_decimalValue;
            return 0;
        }

        public ulong ToUInt64(IFormatProvider? provider)
        {
            if (_longOk) return (ulong)_longValue;
            if (_doubleOk) return (ulong)_doubleValue;
            if (_decimalOk) return (ulong)_decimalValue;
            return 0;
        }

        #endregion IConvertible
    }
}