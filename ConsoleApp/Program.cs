namespace ConsoleApp
{
    using Smart.Reflection;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var o = new DataClass();

            var setterInt = DelegateFactory.Default.CreateSetter<DataClass, int>(typeof(DataClass).GetProperty("IntValue"), true);
            var getterInt = DelegateFactory.Default.CreateGetter<DataClass, int>(typeof(DataClass).GetProperty("IntValue"), true);
            setterInt(o, 123);
            var i = getterInt(o);

            var setterString = DelegateFactory.Default.CreateSetter<DataClass, string>(typeof(DataClass).GetProperty("StringValue"), true);
            var getterString = DelegateFactory.Default.CreateGetter<DataClass, string>(typeof(DataClass).GetProperty("StringValue"), true);
            setterString(o, "test");
            var s = getterString(o);

            var setterIntHolder = DelegateFactory.Default.CreateSetter<DataClass, int>(typeof(DataClass).GetProperty("IntNotificationValue"), true);
            var getterIntHolder = DelegateFactory.Default.CreateGetter<DataClass, int>(typeof(DataClass).GetProperty("IntNotificationValue"), true);
            setterIntHolder(o, 123);
            var ih = getterIntHolder(o);

            var setterStringHolder = DelegateFactory.Default.CreateSetter<DataClass, string>(typeof(DataClass).GetProperty("StringNotificationValue"), true);
            var getterStringHolder = DelegateFactory.Default.CreateGetter<DataClass, string>(typeof(DataClass).GetProperty("StringNotificationValue"), true);
            setterStringHolder(o, "test");
            var sh = getterStringHolder(o);
        }

        //private static readonly Type VoidType = typeof(void);

        //private static readonly Type ObjectType = typeof(object);

        ////// Accessor
        ////private static readonly Type[] GetterParameterTypes = { ObjectType, ObjectType };
        ////private static readonly Type[] SetterParameterTypes = { ObjectType, ObjectType, ObjectType };
        ////private static readonly Type GetterType = typeof(Func<object, object>);
        ////private static readonly Type SetterType = typeof(Action<object, object>);

        //// Accessor

        //private static Func<T, TMember> CreateGetter<T, TMember>(PropertyInfo pi, bool extension)
        //{
        //    var holderType = !extension ? null : ValueHolderHelper.FindValueHolderType(pi);
        //    var isValueProperty = holderType != null;
        //    var tpi = isValueProperty ? ValueHolderHelper.GetValueTypeProperty(holderType) : pi;

        //    if (pi.DeclaringType != typeof(T))
        //    {
        //        throw new ArgumentException($"Invalid type parameter. name=[{pi.Name}]", nameof(pi));
        //    }

        //    if (tpi.PropertyType != typeof(TMember))
        //    {
        //        throw new ArgumentException($"Invalid type parameter. name=[{pi.Name}]", nameof(pi));
        //    }

        //    if (isValueProperty && !pi.CanRead)
        //    {
        //        throw new ArgumentException($"Value holder is not readable. name=[{pi.Name}]", nameof(pi));
        //    }

        //    if (!tpi.CanRead)
        //    {
        //        return null;
        //    }

        //    var dynamic = new DynamicMethod(string.Empty, typeof(TMember), new[] { ObjectType, typeof(T) }, true);
        //    var il = dynamic.GetILGenerator();

        //    if (!pi.GetGetMethod().IsStatic)
        //    {
        //        il.Emit(OpCodes.Ldarg_1);
        //        //il.Emit(OpCodes.Castclass, pi.DeclaringType);
        //    }

        //    il.Emit(pi.GetGetMethod().IsStatic ? OpCodes.Call : OpCodes.Callvirt, pi.GetGetMethod());

        //    if (isValueProperty)
        //    {
        //        il.Emit(OpCodes.Callvirt, tpi.GetGetMethod());
        //    }
        //    //if (tpi.PropertyType.IsValueType)
        //    //{
        //    //    il.Emit(OpCodes.Box, tpi.PropertyType);
        //    //}

        //    il.Emit(OpCodes.Ret);

        //    return (Func<T, TMember>)dynamic.CreateDelegate(typeof(Func<T, TMember>), null);
        //}

        //private static Action<T, TMember> CreateSetter<T, TMember>(PropertyInfo pi, bool extension)
        //{
        //    var holderType = !extension ? null : ValueHolderHelper.FindValueHolderType(pi);
        //    var isValueProperty = holderType != null;
        //    var tpi = isValueProperty ? ValueHolderHelper.GetValueTypeProperty(holderType) : pi;

        //    if (pi.DeclaringType != typeof(T))
        //    {
        //        throw new ArgumentException($"Invalid type parameter. name=[{pi.Name}]", nameof(pi));
        //    }

        //    if (tpi.PropertyType != typeof(TMember))
        //    {
        //        throw new ArgumentException($"Invalid type parameter. name=[{pi.Name}]", nameof(pi));
        //    }

        //    if (isValueProperty && !pi.CanRead)
        //    {
        //        throw new ArgumentException($"Value holder is not readable. name=[{pi.Name}]", nameof(pi));
        //    }

        //    if (!tpi.CanWrite)
        //    {
        //        return null;
        //    }

        //    var isStatic = isValueProperty ? pi.GetGetMethod().IsStatic : pi.GetSetMethod().IsStatic;

        //    var dynamic = new DynamicMethod(string.Empty, VoidType, new[] { ObjectType, typeof(T), typeof(TMember) }, true);
        //    var il = dynamic.GetILGenerator();

        //    if (!isStatic)
        //    {
        //        il.Emit(OpCodes.Ldarg_1);
        //        //il.Emit(OpCodes.Castclass, pi.DeclaringType);
        //    }

        //    if (isValueProperty)
        //    {
        //        il.Emit(pi.GetGetMethod().IsStatic ? OpCodes.Call : OpCodes.Callvirt, pi.GetGetMethod());
        //    }

        //    il.Emit(OpCodes.Ldarg_2);
        //    //il.Emit(OpCodes.Unbox_Any, tpi.PropertyType);

        //    il.Emit(tpi.GetSetMethod().IsStatic ? OpCodes.Call : OpCodes.Callvirt, tpi.GetSetMethod());

        //    il.Emit(OpCodes.Ret);

        //    return (Action<T, TMember>)dynamic.CreateDelegate(typeof(Action<T, TMember>), null);
        //}
    }
}
