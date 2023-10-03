namespace WorkGenericConvert
{
    using System;
    using System.Diagnostics;

    public static class TestEnum
    {
        public static void Test()
        {
            Enum16ToInt32();
            Enum32ToInt32();
            Enum64ToInt32();

            Int32ToEnum16();
            Int32ToEnum32();
            Int32ToEnum64();

            Enum16ToEnum32();
            Enum32ToEnum32();
            Enum64ToEnum32();

            Enum16ToNullableInt32();
            Enum32ToNullableInt32();
            Enum64ToNullableInt32();

            NullableInt32ToEnum16();
            NullableInt32ToEnum32();
            NullableInt32ToEnum64();

            NullableEnum16ToInt32();
            NullableEnum32ToInt32();
            NullableEnum64ToInt32();

            Int32ToNullableEnum16();
            Int32ToNullableEnum32();
            Int32ToNullableEnum64();

            NullableEnum16ToEnum32();
            NullableEnum32ToEnum32();
            NullableEnum64ToEnum32();

            Enum16ToNullableEnum32();
            Enum32ToNullableEnum32();
            Enum64ToNullableEnum32();

            NullableEnum16ToNullableEnum32();
            NullableEnum32ToNullableEnum32();
            NullableEnum64ToNullableEnum32();
        }

        //--------------------------------------------------------------------------------
        // Enum to Value
        //--------------------------------------------------------------------------------

        public static void Enum16ToInt32()
        {
            var f = Factory.Create<Enum16, int>();

            Debug.Assert(0 == f(Enum16.Zero));
            Debug.Assert(1 == f(Enum16.One));
            Debug.Assert(ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void Enum32ToInt32()
        {
            var f = Factory.Create<Enum32, int>();

            Debug.Assert(0 == f(Enum32.Zero));
            Debug.Assert(1 == f(Enum32.One));
            Debug.Assert(Int32.MaxValue == f(Enum32.Max));
        }

        public static void Enum64ToInt32()
        {
            var f = Factory.Create<Enum64, int>();

            Debug.Assert(0 == f(Enum64.Zero));
            Debug.Assert(1 == f(Enum64.One));
            Debug.Assert(ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // Value to Enum
        //--------------------------------------------------------------------------------

        public static void Int32ToEnum16()
        {
            var f = Factory.Create<int, Enum16>();

            Debug.Assert(Enum16.Zero == f(0));
            Debug.Assert(Enum16.One == f(1));
            Debug.Assert((Enum16)ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void Int32ToEnum32()
        {
            var f = Factory.Create<int, Enum32>();

            Debug.Assert(Enum32.Zero == f(0));
            Debug.Assert(Enum32.One == f(1));
            Debug.Assert((Enum32)Int32.MaxValue == f(Int32.MaxValue));
        }

        public static void Int32ToEnum64()
        {
            var f = Factory.Create<int, Enum64>();

            Debug.Assert(Enum64.Zero == f(0));
            Debug.Assert(Enum64.One == f(1));
            Debug.Assert((Enum64)ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // Enum to Enum
        //--------------------------------------------------------------------------------

        public static void Enum16ToEnum32()
        {
            var f = Factory.Create<Enum16, Enum32>();

            Debug.Assert(Enum32.Zero == f(Enum16.Zero));
            Debug.Assert(Enum32.One == f(Enum16.One));
            Debug.Assert((Enum32)ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void Enum32ToEnum32()
        {
            var f = Factory.Create<Enum32, Enum32>();

            Debug.Assert(Enum32.Zero == f(Enum32.Zero));
            Debug.Assert(Enum32.One == f(Enum32.One));
            Debug.Assert(Enum32.Max == f(Enum32.Max));
        }

        public static void Enum64ToEnum32()
        {
            var f = Factory.Create<Enum64, Enum32>();

            Debug.Assert(Enum32.Zero == f(Enum64.Zero));
            Debug.Assert(Enum32.One == f(Enum64.One));
            Debug.Assert((Enum32)ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // Enum to NullableValue
        //--------------------------------------------------------------------------------

        public static void Enum16ToNullableInt32()
        {
            var f = Factory.Create<Enum16, int?>();

            Debug.Assert(0 == f(Enum16.Zero));
            Debug.Assert(1 == f(Enum16.One));
            Debug.Assert(ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void Enum32ToNullableInt32()
        {
            var f = Factory.Create<Enum32, int?>();

            Debug.Assert(0 == f(Enum32.Zero));
            Debug.Assert(1 == f(Enum32.One));
            Debug.Assert(Int32.MaxValue == f(Enum32.Max));
        }

        public static void Enum64ToNullableInt32()
        {
            var f = Factory.Create<Enum64, int?>();

            Debug.Assert(0 == f(Enum64.Zero));
            Debug.Assert(1 == f(Enum64.One));
            Debug.Assert(ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // NullableValue to Enum
        //--------------------------------------------------------------------------------

        public static void NullableInt32ToEnum16()
        {
            var f = Factory.Create<int?, Enum16>();

            Debug.Assert(Enum16.Zero == f(0));
            Debug.Assert(Enum16.One == f(1));
            Debug.Assert((Enum16)ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void NullableInt32ToEnum32()
        {
            var f = Factory.Create<int?, Enum32>();

            Debug.Assert(Enum32.Zero == f(0));
            Debug.Assert(Enum32.One == f(1));
            Debug.Assert((Enum32)Int32.MaxValue == f(Int32.MaxValue));
        }

        public static void NullableInt32ToEnum64()
        {
            var f = Factory.Create<int?, Enum64>();

            Debug.Assert(Enum64.Zero == f(0));
            Debug.Assert(Enum64.One == f(1));
            Debug.Assert((Enum64)ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // NullableEnum to Value
        //--------------------------------------------------------------------------------

        public static void NullableEnum16ToInt32()
        {
            var f = Factory.Create<Enum16?, int>();

            Debug.Assert(0 == f(Enum16.Zero));
            Debug.Assert(1 == f(Enum16.One));
            Debug.Assert(ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void NullableEnum32ToInt32()
        {
            var f = Factory.Create<Enum32?, int>();

            Debug.Assert(0 == f(Enum32.Zero));
            Debug.Assert(1 == f(Enum32.One));
            Debug.Assert(Int32.MaxValue == f(Enum32.Max));
        }

        public static void NullableEnum64ToInt32()
        {
            var f = Factory.Create<Enum64?, int>();

            Debug.Assert(0 == f(Enum64.Zero));
            Debug.Assert(1 == f(Enum64.One));
            Debug.Assert(ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // Value to NullableEnum
        //--------------------------------------------------------------------------------

        public static void Int32ToNullableEnum16()
        {
            var f = Factory.Create<int, Enum16?>();

            Debug.Assert(Enum16.Zero == f(0));
            Debug.Assert(Enum16.One == f(1));
            Debug.Assert((Enum16)ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void Int32ToNullableEnum32()
        {
            var f = Factory.Create<int, Enum32?>();

            Debug.Assert(Enum32.Zero == f(0));
            Debug.Assert(Enum32.One == f(1));
            Debug.Assert((Enum32)Int32.MaxValue == f(Int32.MaxValue));
        }

        public static void Int32ToNullableEnum64()
        {
            var f = Factory.Create<int, Enum64?>();

            Debug.Assert(Enum64.Zero == f(0));
            Debug.Assert(Enum64.One == f(1));
            Debug.Assert((Enum64)ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // NullableEnum to Enum
        //--------------------------------------------------------------------------------

        public static void NullableEnum16ToEnum32()
        {
            var f = Factory.Create<Enum16?, Enum32>();

            Debug.Assert(Enum32.Zero == f(Enum16.Zero));
            Debug.Assert(Enum32.One == f(Enum16.One));
            Debug.Assert((Enum32)ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void NullableEnum32ToEnum32()
        {
            var f = Factory.Create<Enum32?, Enum32>();

            Debug.Assert(Enum32.Zero == f(Enum32.Zero));
            Debug.Assert(Enum32.One == f(Enum32.One));
            Debug.Assert(Enum32.Max == f(Enum32.Max));
        }

        public static void NullableEnum64ToEnum32()
        {
            var f = Factory.Create<Enum64?, Enum32>();

            Debug.Assert(Enum32.Zero == f(Enum64.Zero));
            Debug.Assert(Enum32.One == f(Enum64.One));
            Debug.Assert((Enum32)ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // Enum to NullableEnum
        //--------------------------------------------------------------------------------

        public static void Enum16ToNullableEnum32()
        {
            var f = Factory.Create<Enum16, Enum32?>();

            Debug.Assert(Enum32.Zero == f(Enum16.Zero));
            Debug.Assert(Enum32.One == f(Enum16.One));
            Debug.Assert((Enum32)ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void Enum32ToNullableEnum32()
        {
            var f = Factory.Create<Enum32, Enum32?>();

            Debug.Assert(Enum32.Zero == f(Enum32.Zero));
            Debug.Assert(Enum32.One == f(Enum32.One));
            Debug.Assert(Enum32.Max == f(Enum32.Max));
        }

        public static void Enum64ToNullableEnum32()
        {
            var f = Factory.Create<Enum64, Enum32?>();

            Debug.Assert(Enum32.Zero == f(Enum64.Zero));
            Debug.Assert(Enum32.One == f(Enum64.One));
            Debug.Assert((Enum32)ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // NullableEnum to NullableEnum
        //--------------------------------------------------------------------------------

        public static void NullableEnum16ToNullableEnum32()
        {
            var f = Factory.Create<Enum16?, Enum32?>();

            Debug.Assert(Enum32.Zero == f(Enum16.Zero));
            Debug.Assert(Enum32.One == f(Enum16.One));
            Debug.Assert((Enum32)ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Enum16.Max));
        }

        public static void NullableEnum32ToNullableEnum32()
        {
            var f = Factory.Create<Enum32?, Enum32?>();

            Debug.Assert(Enum32.Zero == f(Enum32.Zero));
            Debug.Assert(Enum32.One == f(Enum32.One));
            Debug.Assert(Enum32.Max == f(Enum32.Max));
        }

        public static void NullableEnum64ToNullableEnum32()
        {
            var f = Factory.Create<Enum64?, Enum32?>();

            Debug.Assert(Enum32.Zero == f(Enum64.Zero));
            Debug.Assert(Enum32.One == f(Enum64.One));
            Debug.Assert((Enum32)ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Enum64.Max));
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public enum Enum16 : short
        {
            Zero = 0,
            One = 1,
            Max = Int16.MaxValue
        }

        public enum Enum32
        {
            Zero = 0,
            One = 1,
            Max = Int32.MaxValue
        }

        public enum Enum64 : long
        {
            Zero = 0,
            One = 1,
            Max = Int64.MaxValue
        }
    }
}
