using System;
using System.Diagnostics;

namespace WorkGenericConvert
{
    public static class TestNullable
    {
        public static void Test()
        {
            NullableInt32ToInt32();
            Int32ToNullableInt32();
            NullableInt32ToNullableInt32();
            NullableInt32ToInt64();
            Int32ToNullableInt64();
            NullableInt32ToNullableInt64();
            NullableInt32ToInt16();
            Int32ToNullableInt16();
            NullableInt32ToNullableInt16();

            NullableInt32ToDecimal();
            DecimalToNullableInt32();
            Int32ToNullableDecimal();
            NullableDecimalToInt32();
        }

        //--------------------------------------------------------------------------------
        // Same
        //--------------------------------------------------------------------------------

        public static void NullableInt32ToInt32()
        {
            var f = Factory.Create<int?, int>();

            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            Debug.Assert(Int32.MinValue == f(Int32.MinValue));
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue));
        }

        public static void Int32ToNullableInt32()
        {
            var f = Factory.Create<int, int?>();

            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            Debug.Assert(Int32.MinValue == f(Int32.MinValue));
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue));
        }

        public static void NullableInt32ToNullableInt32()
        {
            var f = Factory.Create<int?, int?>();

            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            Debug.Assert(Int32.MinValue == f(Int32.MinValue));
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // To Large
        //--------------------------------------------------------------------------------

        public static void NullableInt32ToInt64()
        {
            var f = Factory.Create<int?, long>();

            Debug.Assert(ManualConverter.Int32ToInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void Int32ToNullableInt64()
        {
            var f = Factory.Create<int, long?>();

            Debug.Assert(ManualConverter.Int32ToInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void NullableInt32ToNullableInt64()
        {
            var f = Factory.Create<int?, long?>();

            Debug.Assert(ManualConverter.Int32ToInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // To Small
        //--------------------------------------------------------------------------------

        public static void NullableInt32ToInt16()
        {
            var f = Factory.Create<int?, short>();

            // Base
            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            // Min/Max
            Debug.Assert(ManualConverter.Int32ToInt16(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt16(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void Int32ToNullableInt16()
        {
            var f = Factory.Create<int, short?>();

            // Base
            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            // Min/Max
            Debug.Assert(ManualConverter.Int32ToInt16(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt16(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void NullableInt32ToNullableInt16()
        {
            var f = Factory.Create<int?, short?>();

            // Base
            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            // Min/Max
            Debug.Assert(ManualConverter.Int32ToInt16(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt16(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // Extra
        //--------------------------------------------------------------------------------

        public static void NullableInt32ToDecimal()
        {
            var f = Factory.Create<int?, decimal>();

            Debug.Assert(ManualConverter.Int32ToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToDecimal(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToDecimal(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToDecimal(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void DecimalToNullableInt32()
        {
            var f = Factory.Create<decimal, int?>();

            Debug.Assert(ManualConverter.DecimalToInt32(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToInt32(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToInt32(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToInt32(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToInt32(Decimal.MaxValue) == f(Decimal.MaxValue));
        }

        public static void Int32ToNullableDecimal()
        {
            var f = Factory.Create<int, decimal?>();

            Debug.Assert(ManualConverter.Int32ToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToDecimal(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToDecimal(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToDecimal(Int32.MaxValue) == f(Int32.MaxValue));
        }

        public static void NullableDecimalToInt32()
        {
            var f = Factory.Create<decimal?, int>();

            Debug.Assert(ManualConverter.DecimalToInt32(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToInt32(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToInt32(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToInt32(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToInt32(Decimal.MaxValue) == f(Decimal.MaxValue));
        }
    }
}
