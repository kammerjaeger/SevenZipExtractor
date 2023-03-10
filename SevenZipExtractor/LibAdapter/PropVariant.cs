using SharpGen.Runtime.Win32;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace SevenZipExtractor.LibAdapter
{
    public static class VariantExtension
    {
        public static void Clear(this PropVariant variant)
        {
            switch ((VarEnum)variant.ElementType)
            {
                case VarEnum.VT_EMPTY:
                    break;

                case VarEnum.VT_NULL:
                case VarEnum.VT_I2:
                case VarEnum.VT_I4:
                case VarEnum.VT_R4:
                case VarEnum.VT_R8:
                case VarEnum.VT_CY:
                case VarEnum.VT_DATE:
                case VarEnum.VT_ERROR:
                case VarEnum.VT_BOOL:
                //case VarEnum.VT_DECIMAL:
                case VarEnum.VT_I1:
                case VarEnum.VT_UI1:
                case VarEnum.VT_UI2:
                case VarEnum.VT_UI4:
                case VarEnum.VT_I8:
                case VarEnum.VT_UI8:
                case VarEnum.VT_INT:
                case VarEnum.VT_UINT:
                case VarEnum.VT_HRESULT:
                case VarEnum.VT_FILETIME:
                    variant.ElementType = VariantElementType.Empty;
                    break;

                default:
                    SevenZipHandle.PropVariantClear(ref variant);
                    variant.Type = VariantType.Default;
                    variant.ElementType = VariantElementType.Empty;
                    break;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct VariantValue
        {
            public struct CurrencyLowHigh
            {
                public uint LowValue;

                public int HighValue;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct CurrencyValue
            {
                [FieldOffset(0)]
                public CurrencyLowHigh LowHigh;

                [FieldOffset(0)]
                public long longValue;
            }

            public struct RecordValue
            {
                public IntPtr RecordInfo;

                public IntPtr RecordPointer;
            }

            [FieldOffset(0)]
            public byte byteValue;

            [FieldOffset(0)]
            public sbyte signedByteValue;

            [FieldOffset(0)]
            public ushort ushortValue;

            [FieldOffset(0)]
            public short shortValue;

            [FieldOffset(0)]
            public uint uintValue;

            [FieldOffset(0)]
            public int intValue;

            [FieldOffset(0)]
            public ulong ulongValue;

            [FieldOffset(0)]
            public long longValue;

            [FieldOffset(0)]
            public float floatValue;

            [FieldOffset(0)]
            public double doubleValue;

            [FieldOffset(0)]
            public IntPtr pointerValue;

            [FieldOffset(0)]
            public CurrencyValue currencyValue;

            [FieldOffset(0)]
            public RecordValue recordValue;
        }

        [FieldOffset(0)]
        private ushort vt;

        [FieldOffset(2)]
        private ushort reserved1;

        [FieldOffset(4)]
        private ushort reserved2;

        [FieldOffset(6)]
        private ushort reserved3;

        [FieldOffset(8)]
        private VariantValue variantValue;

        public VariantElementType ElementType
        {
            get
            {
                return (VariantElementType)(vt & 0xFFFu);
            }
            set
            {
                vt = (ushort)(vt & 0xF000u | (uint)value);
            }
        }

        public VariantType Type
        {
            get
            {
                return (VariantType)(vt & 0xF000u);
            }
            set
            {
                vt = (ushort)(vt & 0xFFFu | (uint)value);
            }
        }

        /// <summary>
        /// Transforms a Guid btrs string to a Guid object
        /// </summary>
        /// <param name="variant">Prop variant to convert</param>
        public static unsafe explicit operator Guid(PropVariant variant) {
            if (variant.ElementType != VariantElementType.BinaryString) {
                throw new InvalidCastException("Wrong format");
            }

            if (SevenZipHandle.SysStringByteLen(variant.variantValue.pointerValue) == 16) {
                return new Guid(new ReadOnlySpan<byte>((void*) variant.variantValue.pointerValue, 16));
            } else {
                throw new ArgumentOutOfRangeException("BSTR string is not 16 byte long");
            }
        }

        public static unsafe explicit operator ReadOnlySpan<byte>(PropVariant variant) {
            if (variant.ElementType == VariantElementType.Empty) {
                return default(ReadOnlySpan<byte>);
            } else if (variant.ElementType != VariantElementType.BinaryString) {
                throw new InvalidCastException("Wrong format");
            }
            return new ReadOnlySpan<byte>((void*)variant.variantValue.pointerValue, (int) SevenZipHandle.SysStringByteLen(variant.variantValue.pointerValue));
        }

        public unsafe object? Value
        {
            get
            {
                switch (Type)
                {
                    case VariantType.Default:
                        switch (ElementType)
                        {
                            case VariantElementType.Empty:
                            case VariantElementType.Null:
                                return null;
                            case VariantElementType.Blob:
                                {
                                    byte[] array16 = new byte[(int)variantValue.recordValue.RecordInfo];
                                    if (array16.Length != 0)
                                    {
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<byte>(array16), array16.Length);
                                    }

                                    return array16;
                                }
                            case VariantElementType.Bool:
                                return variantValue.intValue != 0;
                            case VariantElementType.Byte:
                                return variantValue.signedByteValue;
                            case VariantElementType.UByte:
                                return variantValue.byteValue;
                            case VariantElementType.UShort:
                                return variantValue.ushortValue;
                            case VariantElementType.Short:
                                return variantValue.shortValue;
                            case VariantElementType.UInt:
                            case VariantElementType.UInt1:
                                return variantValue.uintValue;
                            case VariantElementType.Int:
                            case VariantElementType.Int1:
                                return variantValue.intValue;
                            case VariantElementType.ULong:
                                return variantValue.ulongValue;
                            case VariantElementType.Long:
                                return variantValue.longValue;
                            case VariantElementType.Float:
                                return variantValue.floatValue;
                            case VariantElementType.Double:
                                return variantValue.doubleValue;
                            case VariantElementType.BinaryString:
                                return SevenZipHandle.PtrToStringBSTR(variantValue.pointerValue);
                            case VariantElementType.StringPointer:
                                return Marshal.PtrToStringAnsi(variantValue.pointerValue);
                            case VariantElementType.WStringPointer:
                                return Marshal.PtrToStringUni(variantValue.pointerValue);
                            case VariantElementType.Dispatch:
                            case VariantElementType.ComUnknown:
                                return new ComObject(variantValue.pointerValue);
                            case VariantElementType.Pointer:
                            case VariantElementType.IntPointer:
                                return variantValue.pointerValue;
                            case VariantElementType.FileTime:
                                return DateTime.FromFileTime(variantValue.longValue);
                            default:
                                return null;
                        }
                    case VariantType.Vector:
                        {
                            int num = (int)variantValue.recordValue.RecordInfo;
                            switch (ElementType)
                            {
                                case VariantElementType.Bool:
                                    {
                                        RawBool* pointer = stackalloc RawBool[num];
                                        ReadOnlySpan<RawBool> readOnlySpan = new ReadOnlySpan<RawBool>(pointer, num);
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, readOnlySpan, num);
                                        return RawBoolHelpers.ConvertToBoolArray(readOnlySpan);
                                    }
                                case VariantElementType.Byte:
                                    {
                                        sbyte[] array12 = new sbyte[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<sbyte>(array12), num);
                                        return array12;
                                    }
                                case VariantElementType.UByte:
                                    {
                                        byte[] array10 = new byte[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<byte>(array10), num);
                                        return array10;
                                    }
                                case VariantElementType.UShort:
                                    {
                                        ushort[] array9 = new ushort[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<ushort>(array9), num);
                                        return array9;
                                    }
                                case VariantElementType.Short:
                                    {
                                        short[] array7 = new short[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<short>(array7), num);
                                        return array7;
                                    }
                                case VariantElementType.UInt:
                                case VariantElementType.UInt1:
                                    {
                                        uint[] array6 = new uint[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<uint>(array6), num);
                                        return array6;
                                    }
                                case VariantElementType.Int:
                                case VariantElementType.Int1:
                                    {
                                        int[] array3 = new int[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<int>(array3), num);
                                        return array3;
                                    }
                                case VariantElementType.ULong:
                                    {
                                        ulong[] array2 = new ulong[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<ulong>(array2), num);
                                        return array2;
                                    }
                                case VariantElementType.Long:
                                    {
                                        long[] array15 = new long[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<long>(array15), num);
                                        return array15;
                                    }
                                case VariantElementType.Float:
                                    {
                                        float[] array14 = new float[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<float>(array14), num);
                                        return array14;
                                    }
                                case VariantElementType.Double:
                                    {
                                        double[] array13 = new double[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<double>(array13), num);
                                        return array13;
                                    }
                                case VariantElementType.BinaryString:
                                    throw new NotSupportedException();
                                case VariantElementType.StringPointer:
                                    {
                                        string?[] array11 = new string[num];
                                        for (int l = 0; l < num; l++)
                                        {
                                            array11[l] = Marshal.PtrToStringAnsi(*(IntPtr*)((byte*)(void*)variantValue.recordValue.RecordPointer + l * (nint)sizeof(IntPtr)));
                                        }

                                        return array11;
                                    }
                                case VariantElementType.WStringPointer:
                                    {
                                        string?[] array8 = new string[num];
                                        for (int k = 0; k < num; k++)
                                        {
                                            array8[k] = Marshal.PtrToStringUni(*(IntPtr*)((byte*)(void*)variantValue.recordValue.RecordPointer + k * (nint)sizeof(IntPtr)));
                                        }

                                        return array8;
                                    }
                                case VariantElementType.Dispatch:
                                case VariantElementType.ComUnknown:
                                    {
                                        ComObject[] array5 = new ComObject[num];
                                        for (int j = 0; j < num; j++)
                                        {
                                            array5[j] = new ComObject(*(IntPtr*)((byte*)(void*)variantValue.recordValue.RecordPointer + j * (nint)sizeof(IntPtr)));
                                        }

                                        return array5;
                                    }
                                case VariantElementType.Pointer:
                                case VariantElementType.IntPointer:
                                    {
                                        IntPtr[] array4 = new IntPtr[num];
                                        MemoryHelpers.Read(variantValue.recordValue.RecordPointer, new ReadOnlySpan<IntPtr>(array4), num);
                                        return array4;
                                    }
                                case VariantElementType.FileTime:
                                    {
                                        DateTime[] array = new DateTime[num];
                                        for (int i = 0; i < num; i++)
                                        {
                                            array[i] = DateTime.FromFileTime(*(long*)((byte*)(void*)variantValue.recordValue.RecordPointer + i * (nint)8));
                                        }

                                        return array;
                                    }
                                default:
                                    return null;
                            }
                        }
                    default:
                        return null;
                }
            }
            set
            {
                if (value == null)
                {
                    Type = VariantType.Default;
                    ElementType = VariantElementType.Null;
                    return;
                }

                Type type = value.GetType();
                Type = VariantType.Default;
                if (type.GetTypeInfo().IsPrimitive)
                {
                    if ((object)type == typeof(byte))
                    {
                        ElementType = VariantElementType.UByte;
                        variantValue.byteValue = (byte)value;
                        return;
                    }

                    if ((object)type == typeof(sbyte))
                    {
                        ElementType = VariantElementType.Byte;
                        variantValue.signedByteValue = (sbyte)value;
                        return;
                    }

                    if ((object)type == typeof(int))
                    {
                        ElementType = VariantElementType.Int;
                        variantValue.intValue = (int)value;
                        return;
                    }

                    if ((object)type == typeof(uint))
                    {
                        ElementType = VariantElementType.UInt;
                        variantValue.uintValue = (uint)value;
                        return;
                    }

                    if ((object)type == typeof(long))
                    {
                        ElementType = VariantElementType.Long;
                        variantValue.longValue = (long)value;
                        return;
                    }

                    if ((object)type == typeof(ulong))
                    {
                        ElementType = VariantElementType.ULong;
                        variantValue.ulongValue = (ulong)value;
                        return;
                    }

                    if ((object)type == typeof(short))
                    {
                        ElementType = VariantElementType.Short;
                        variantValue.shortValue = (short)value;
                        return;
                    }

                    if ((object)type == typeof(ushort))
                    {
                        ElementType = VariantElementType.UShort;
                        variantValue.ushortValue = (ushort)value;
                        return;
                    }

                    if ((object)type == typeof(float))
                    {
                        ElementType = VariantElementType.Float;
                        variantValue.floatValue = (float)value;
                        return;
                    }

                    if ((object)type == typeof(double))
                    {
                        ElementType = VariantElementType.Double;
                        variantValue.doubleValue = (double)value;
                        return;
                    }
                }
                else
                {
                    if (value is ComObject comObject)
                    {
                        ElementType = VariantElementType.ComUnknown;
                        variantValue.pointerValue = comObject.NativePointer;
                        return;
                    }

                    if (value is DateTime dateTime)
                    {
                        ElementType = VariantElementType.FileTime;
                        variantValue.longValue = dateTime.ToFileTime();
                        return;
                    }

                    //string? s;
                    if (value is string s)
                    {
                        // depending on the set element type copy the data over
                        switch (ElementType)
                        {
                            case VariantElementType.BinaryString:
                                variantValue.pointerValue = SevenZipHandle.StringToBSTR(s);
                                break;
                            case VariantElementType.WStringPointer:
                                variantValue.pointerValue = Marshal.StringToCoTaskMemUni(s);
                                break;
                        }
                        return;
                    }
                }

                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Type [{0}] is not handled", args: new object[1] { type.Name }));
            }
        }
    }
}
