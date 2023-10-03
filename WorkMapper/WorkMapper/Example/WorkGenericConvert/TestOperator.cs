namespace WorkGenericConvert
{
    using System;
    using System.Diagnostics;

    public static class TestOperator
    {
        public static void Test()
        {
            ValueToClass();
            ClassToValue();
            ValueToStruct();
            StructToValue();

            ValueToNClass();
            //NClassToValue();
            ValueToNStruct();
            //NStructToValue();

            NullableValueToClass();
            ClassToNullableValue();
            NullableValueToStruct();
            StructToNullableValue();

            NullableValueToNClass();
            NClassToNullableValue();
            NullableValueToNStruct();
            NStructToNullableValue();

            ValueToNullableStruct();
            NullableStructToValue();

            ValueToNullableNStruct();
            //NullableNStructToValue();

            NullableValueToNullableStruct();
            NullableStructToNullableValue();

            NullableValueToNullableNStruct();
            NullableNStructToNullableValue();

            ClassPair1ToClassPair2();
            ClassPair2ToClassPair1();

            StructPair1ToStructPair2();
            StructPair2ToStructPair1();

            NullableStructPair1ToStructPair2();
            StructPair2ToNullableStructPair1();

            StructPair1ToNullableStructPair2();
            NullableStructPair2ToStructPair1();

            NullableStructPair1ToNullableStructPair2();
            NullableStructPair2ToNullableStructPair1();

            CrossPairCToCrossPairS();
            CrossPairSToCrossPairC();

            CrossPairCToNullableCrossPairS();
            NullableCrossPairSToCrossPairC();
        }

        //--------------------------------------------------------------------------------
        // Value/Class
        //--------------------------------------------------------------------------------

        public static void ValueToClass()
        {
            var f = Factory.Create<int, OperatorClass>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        public static void ClassToValue()
        {
            var f = Factory.Create<OperatorClass, int>();

            Debug.Assert(0 == f(new OperatorClass { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorClass { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorClass { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // Value/Struct
        //--------------------------------------------------------------------------------

        public static void ValueToStruct()
        {
            var f = Factory.Create<int, OperatorStruct>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        public static void StructToValue()
        {
            var f = Factory.Create<OperatorStruct, int>();

            Debug.Assert(0 == f(new OperatorStruct { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorStruct { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorStruct { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // Value/NClass
        //--------------------------------------------------------------------------------

        public static void ValueToNClass()
        {
            var f = Factory.Create<int, OperatorNClass>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        // Not Support
        //public static void NClassToValue()
        //{
        //    var f = Factory.Create<OperatorNClass, int>();

        //    Debug.Assert(0 == f(new OperatorNClass { Value = 0 }));
        //    Debug.Assert(-1 == f(new OperatorNClass { Value = -1 }));
        //    Debug.Assert(Int32.MaxValue == f(new OperatorNClass { Value = Int32.MaxValue }));
        //}

        //--------------------------------------------------------------------------------
        // Value/NStruct
        //--------------------------------------------------------------------------------

        public static void ValueToNStruct()
        {
            var f = Factory.Create<int, OperatorNStruct>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        // Not Support
        //public static void NStructToValue()
        //{
        //    var f = Factory.Create<OperatorNStruct, int>();

        //    Debug.Assert(0 == f(new OperatorNStruct { Value = 0 }));
        //    Debug.Assert(-1 == f(new OperatorNStruct { Value = -1 }));
        //    Debug.Assert(Int32.MaxValue == f(new OperatorNStruct { Value = Int32.MaxValue }));
        //}

        //--------------------------------------------------------------------------------
        // Value/Class
        //--------------------------------------------------------------------------------

        public static void NullableValueToClass()
        {
            var f = Factory.Create<int?, OperatorClass>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        public static void ClassToNullableValue()
        {
            var f = Factory.Create<OperatorClass, int?>();

            Debug.Assert(0 == f(new OperatorClass { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorClass { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorClass { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // NullableValue/Struct
        //--------------------------------------------------------------------------------

        public static void NullableValueToStruct()
        {
            var f = Factory.Create<int?, OperatorStruct>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        public static void StructToNullableValue()
        {
            var f = Factory.Create<OperatorStruct, int?>();

            Debug.Assert(0 == f(new OperatorStruct { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorStruct { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorStruct { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // NullableValue/NClass
        //--------------------------------------------------------------------------------

        public static void NullableValueToNClass()
        {
            var f = Factory.Create<int?, OperatorNClass>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        public static void NClassToNullableValue()
        {
            var f = Factory.Create<OperatorNClass, int?>();

            Debug.Assert(0 == f(new OperatorNClass { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorNClass { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorNClass { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // NullableValue/NStruct
        //--------------------------------------------------------------------------------

        public static void NullableValueToNStruct()
        {
            var f = Factory.Create<int?, OperatorNStruct>();

            Debug.Assert(0 == f(0).Value);
            Debug.Assert(-1 == f(-1).Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue).Value);
        }

        public static void NStructToNullableValue()
        {
            var f = Factory.Create<OperatorNStruct, int?>();

            Debug.Assert(0 == f(new OperatorNStruct { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorNStruct { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorNStruct { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // Value/NullableStruct
        //--------------------------------------------------------------------------------

        public static void ValueToNullableStruct()
        {
            var f = Factory.Create<int, OperatorStruct?>();

            Debug.Assert(0 == f(0)!.Value.Value);
            Debug.Assert(-1 == f(-1)!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue)!.Value.Value);
        }

        public static void NullableStructToValue()
        {
            var f = Factory.Create<OperatorStruct?, int>();

            Debug.Assert(0 == f(new OperatorStruct { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorStruct { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorStruct { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // Value/NullableNStruct
        //--------------------------------------------------------------------------------

        public static void ValueToNullableNStruct()
        {
            var f = Factory.Create<int, OperatorNStruct?>();

            Debug.Assert(0 == f(0)!.Value.Value);
            Debug.Assert(-1 == f(-1)!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue)!.Value.Value);
        }

        // Not Support
        //public static void NullableNStructToValue()
        //{
        //    var f = Factory.Create<OperatorNStruct?, int>();

        //    Debug.Assert(0 == f(new OperatorNStruct { Value = 0 }));
        //    Debug.Assert(-1 == f(new OperatorNStruct { Value = -1 }));
        //    Debug.Assert(Int32.MaxValue == f(new OperatorNStruct { Value = Int32.MaxValue }));
        //}

        //--------------------------------------------------------------------------------
        // NullableValue/NullableStruct
        //--------------------------------------------------------------------------------

        public static void NullableValueToNullableStruct()
        {
            var f = Factory.Create<int?, OperatorStruct?>();

            Debug.Assert(0 == f(0)!.Value.Value);
            Debug.Assert(-1 == f(-1)!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue)!.Value.Value);
        }

        public static void NullableStructToNullableValue()
        {
            var f = Factory.Create<OperatorStruct?, int?>();

            Debug.Assert(0 == f(new OperatorStruct { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorStruct { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorStruct { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // NullableValue/NullableNStruct
        //--------------------------------------------------------------------------------

        public static void NullableValueToNullableNStruct()
        {
            var f = Factory.Create<int?, OperatorNStruct?>();

            Debug.Assert(0 == f(0)!.Value.Value);
            Debug.Assert(-1 == f(-1)!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(Int32.MaxValue)!.Value.Value);
        }

        public static void NullableNStructToNullableValue()
        {
            var f = Factory.Create<OperatorNStruct?, int?>();

            Debug.Assert(0 == f(new OperatorNStruct { Value = 0 }));
            Debug.Assert(-1 == f(new OperatorNStruct { Value = -1 }));
            Debug.Assert(Int32.MaxValue == f(new OperatorNStruct { Value = Int32.MaxValue }));
        }

        //--------------------------------------------------------------------------------
        // ClassPair1/ClassPair2
        //--------------------------------------------------------------------------------

        public static void ClassPair1ToClassPair2()
        {
            var f = Factory.Create<ClassPair1, ClassPair2>();

            Debug.Assert(0 == f(new ClassPair1 { Value = 0 }).Value);
            Debug.Assert(-1 == f(new ClassPair1 { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new ClassPair1 { Value = Int32.MaxValue }).Value);
        }

        public static void ClassPair2ToClassPair1()
        {
            var f = Factory.Create<ClassPair2, ClassPair1>();

            Debug.Assert(0 == f(new ClassPair2 { Value = 0 }).Value);
            Debug.Assert(-1 == f(new ClassPair2 { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new ClassPair2 { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // StructPair1/StructPair2
        //--------------------------------------------------------------------------------

        public static void StructPair1ToStructPair2()
        {
            var f = Factory.Create<StructPair1, StructPair2>();

            Debug.Assert(0 == f(new StructPair1 { Value = 0 }).Value);
            Debug.Assert(-1 == f(new StructPair1 { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair1 { Value = Int32.MaxValue }).Value);
        }

        public static void StructPair2ToStructPair1()
        {
            var f = Factory.Create<StructPair2, StructPair1>();

            Debug.Assert(0 == f(new StructPair2 { Value = 0 }).Value);
            Debug.Assert(-1 == f(new StructPair2 { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair2 { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // NullableStructPair1/StructPair2
        //--------------------------------------------------------------------------------

        public static void NullableStructPair1ToStructPair2()
        {
            var f = Factory.Create<StructPair1?, StructPair2>();

            Debug.Assert(0 == f(new StructPair1 { Value = 0 }).Value);
            Debug.Assert(-1 == f(new StructPair1 { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair1 { Value = Int32.MaxValue }).Value);
        }

        public static void StructPair2ToNullableStructPair1()
        {
            var f = Factory.Create<StructPair2, StructPair1?>();

            Debug.Assert(0 == f(new StructPair2 { Value = 0 })!.Value.Value);
            Debug.Assert(-1 == f(new StructPair2 { Value = -1 })!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair2 { Value = Int32.MaxValue })!.Value.Value);
        }

        //--------------------------------------------------------------------------------
        // StructPair1/NullableStructPair2
        //--------------------------------------------------------------------------------

        public static void StructPair1ToNullableStructPair2()
        {
            var f = Factory.Create<StructPair1, StructPair2?>();

            Debug.Assert(0 == f(new StructPair1 { Value = 0 })!.Value.Value);
            Debug.Assert(-1 == f(new StructPair1 { Value = -1 })!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair1 { Value = Int32.MaxValue })!.Value.Value);
        }

        public static void NullableStructPair2ToStructPair1()
        {
            var f = Factory.Create<StructPair2?, StructPair1>();

            Debug.Assert(0 == f(new StructPair2 { Value = 0 }).Value);
            Debug.Assert(-1 == f(new StructPair2 { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair2 { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // NullableStructPair1/NullableStructPair2
        //--------------------------------------------------------------------------------

        public static void NullableStructPair1ToNullableStructPair2()
        {
            var f = Factory.Create<StructPair1?, StructPair2?>();

            Debug.Assert(0 == f(new StructPair1 { Value = 0 })!.Value.Value);
            Debug.Assert(-1 == f(new StructPair1 { Value = -1 })!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair1 { Value = Int32.MaxValue })!.Value.Value);
        }

        public static void NullableStructPair2ToNullableStructPair1()
        {
            var f = Factory.Create<StructPair2?, StructPair1?>();

            Debug.Assert(0 == f(new StructPair2 { Value = 0 })!.Value.Value);
            Debug.Assert(-1 == f(new StructPair2 { Value = -1 })!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(new StructPair2 { Value = Int32.MaxValue })!.Value.Value);
        }

        //--------------------------------------------------------------------------------
        // CrossPairC/CrossPairS
        //--------------------------------------------------------------------------------

        public static void CrossPairCToCrossPairS()
        {
            var f = Factory.Create<CrossPairC, CrossPairS>();

            Debug.Assert(0 == f(new CrossPairC { Value = 0 }).Value);
            Debug.Assert(-1 == f(new CrossPairC { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new CrossPairC { Value = Int32.MaxValue }).Value);
        }

        public static void CrossPairSToCrossPairC()
        {
            var f = Factory.Create<CrossPairS, CrossPairC>();

            Debug.Assert(0 == f(new CrossPairS { Value = 0 }).Value);
            Debug.Assert(-1 == f(new CrossPairS { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new CrossPairS { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // CrossPairC/NullableCrossPairS
        //--------------------------------------------------------------------------------

        public static void CrossPairCToNullableCrossPairS()
        {
            var f = Factory.Create<CrossPairC, CrossPairS?>();

            Debug.Assert(0 == f(new CrossPairC { Value = 0 })!.Value.Value);
            Debug.Assert(-1 == f(new CrossPairC { Value = -1 })!.Value.Value);
            Debug.Assert(Int32.MaxValue == f(new CrossPairC { Value = Int32.MaxValue })!.Value.Value);
        }

        public static void NullableCrossPairSToCrossPairC()
        {
            var f = Factory.Create<CrossPairS?, CrossPairC>();

            Debug.Assert(0 == f(new CrossPairS { Value = 0 }).Value);
            Debug.Assert(-1 == f(new CrossPairS { Value = -1 }).Value);
            Debug.Assert(Int32.MaxValue == f(new CrossPairS { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public struct OperatorStruct
        {
            public int Value { get; set; }

            public static implicit operator OperatorStruct(int value) => new() { Value = value };
            public static explicit operator int(OperatorStruct value) => value.Value;
        }

        public class OperatorClass
        {
            public int Value { get; set; }

            public static implicit operator OperatorClass(int value) => new() { Value = value };
            public static explicit operator int(OperatorClass value) => value.Value;
        }

        public struct OperatorNStruct
        {
            public int? Value { get; set; }

            public static implicit operator OperatorNStruct(int? value) => new() { Value = value };
            public static explicit operator int?(OperatorNStruct value) => value.Value;
        }

        public class OperatorNClass
        {
            public int? Value { get; set; }

            public static implicit operator OperatorNClass(int? value) => new() { Value = value };
            public static explicit operator int?(OperatorNClass value) => value.Value;
        }

        public class ClassPair1
        {
            public int Value { get; set; }

            public static implicit operator ClassPair1(ClassPair2 value) => new() { Value = value.Value };
            public static explicit operator ClassPair2(ClassPair1 value) => new() { Value = value.Value };
        }

        public class ClassPair2
        {
            public int Value { get; set; }
        }

        public struct StructPair1
        {
            public int Value { get; set; }

            public static implicit operator StructPair1(StructPair2 value) => new() { Value = value.Value };
            public static explicit operator StructPair2(StructPair1 value) => new() { Value = value.Value };
        }

        public struct StructPair2
        {
            public int Value { get; set; }
        }

        public class CrossPairC
        {
            public int Value { get; set; }

            public static implicit operator CrossPairC(CrossPairS value) => new() { Value = value.Value };
            public static explicit operator CrossPairS(CrossPairC value) => new() { Value = value.Value };
        }

        public struct CrossPairS
        {
            public int Value { get; set; }
        }
    }
}
