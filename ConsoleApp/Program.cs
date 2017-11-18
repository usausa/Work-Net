namespace ConsoleApp
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using ConsoleApp.Reflection;

    using Smart.Reflection;
    using Smart.Reflection.Emit;

    public static class Program
    {
        public static void Main(string[] args)
        {
            //WorkDynamicProxy.Test();

            //var type0 = typeof(Class0);
            //var type1 = typeof(Class1);
            //var ctor0 = type0.GetConstructor(Type.EmptyTypes);
            //var ctor1 = type1.GetConstructors().First();

            //var activator0 = EmitMethodGenerator.CreateActivator(ctor0);
            //var o = activator0.Create(null);

            //var data = new DataClass();
            //var piStr = data.GetType().GetProperty(nameof(DataClass.StringValue));
            //var piInt = data.GetType().GetProperty(nameof(DataClass.IntValue));
            //var piStruct = data.GetType().GetProperty(nameof(DataClass.StructValue));
            //var piStrNotify = data.GetType().GetProperty(nameof(DataClass.StringNotificationValue));
            //var piIntNotify = data.GetType().GetProperty(nameof(DataClass.IntNotificationValue));

            // str notify
            //var accessorStrNotify = EmitMethodGenerator.CreateAccessor(piStrNotify);
            //accessorStrNotify.SetValue(data, "a");
            //var retStrNotify = accessorStrNotify.GetValue(data);

            // int notify
            //var accessorIntNotify = EmitMethodGenerator.CreateAccessor(piIntNotify);
            //accessorIntNotify.SetValue(data, 1);
            //var retStrNotify = accessorIntNotify.GetValue(data);
            //accessorIntNotify.SetValue(data, null);
            //retStrNotify = accessorIntNotify.GetValue(data);

            //// struct
            //var accessorStruct = EmitMethodGenerator.CreateAccessor(piStruct);

            //data.StructValue = new Size { X = 1, Y = 2 };
            //var i = data.StructValue.X;
            //accessorStruct.SetValue(data, null);
            //i = data.StructValue.X;
            //accessorStruct.SetValue(data, new Size { X = 2, Y = 3 });
            //i = data.StructValue.X;

            //// str
            //var accessorStr = EmitMethodGenerator.CreateAccessor(piStr);
            //accessorStr.SetValue(data, "a");
            //var retStr = accessorStr.GetValue(data);

            //// int
            //var accessorInt = EmitMethodGenerator.CreateAccessor(piInt);
            //accessorInt.SetValue(data, 1);
            //var retInt = accessorInt.GetValue(data);
            //accessorInt.SetValue(data, null);
            //retInt = accessorInt.GetValue(data);

            //var data = new EnumPropertyData();
            //var pi = data.GetType().GetProperty(nameof(EnumPropertyData.EnumNotificationValue));
            //var accessor = EmitMethodGenerator.CreateAccessor(pi);

            //accessor.SetValue(data, MyEnum.One);
            //Debug.Assert(Equals(data.EnumNotificationValue.Value, MyEnum.One));
            //var value = accessor.GetValue(data);
            //Debug.Assert(Equals(value, MyEnum.One));

            //accessor.SetValue(data, null);
            //Debug.Assert(Equals(data.EnumNotificationValue.Value, MyEnum.Zero));
            //value = accessor.GetValue(data);
            //Debug.Assert(Equals(value, MyEnum.Zero));

            //var data = new StructPropertyData();
            //var pi = data.GetType().GetProperty(nameof(StructPropertyData.StructNotificationValue));
            //var accessor = EmitMethodGenerator.CreateAccessor(pi);

            //accessor.SetValue(data, new MyStruct { X = 1, Y = 2 });
            //Debug.Assert(data.StructNotificationValue.Value.X == 1);
            //Debug.Assert(data.StructNotificationValue.Value.Y == 2);
            //var value = (MyStruct)accessor.GetValue(data);
            //Debug.Assert(value.X == 1);
            //Debug.Assert(value.Y == 2);

            //accessor.SetValue(data, null);
            //Debug.Assert(data.StructNotificationValue.Value.X == 0);
            //Debug.Assert(data.StructNotificationValue.Value.Y == 0);
            //value = (MyStruct)accessor.GetValue(data);
            //Debug.Assert(value.X == 0);
            //Debug.Assert(value.Y == 0);
        }
    }
}
