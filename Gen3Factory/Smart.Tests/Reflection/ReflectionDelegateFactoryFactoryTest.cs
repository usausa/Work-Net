﻿namespace Smart.Reflection
{
    using System.Linq;

    using Xunit;

    public class ReflectionDelegateFactoryActivatorTest
    {
        [Fact]
        public void FactoryData0()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory(typeof(Data0).GetConstructors().First());

            var data = factory(null);
            Assert.NotNull(data);
        }

        [Fact]
        public void FactoryData1()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory(typeof(Data1).GetConstructors().First());

            var data = (Data1)factory(new object[] { 1 });
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
        }

        [Fact]
        public void FactoryData17()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory(typeof(Data17).GetConstructors().First());

            var data = (Data17)factory(new object[] { 1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11, "12", 13, "14", 15, "16", 17 });
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
            Assert.Equal("12", data.Param12);
            Assert.Equal(13, data.Param13);
            Assert.Equal("14", data.Param14);
            Assert.Equal(15, data.Param15);
            Assert.Equal("16", data.Param16);
            Assert.Equal(17, data.Param17);
        }

        [Fact]
        public void Factory0()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory0(typeof(Data0).GetConstructors().First());

            var data = factory();
            Assert.NotNull(data);
        }

        [Fact]
        public void Factory1()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory1(typeof(Data1).GetConstructors().First());

            var data = (Data1)factory(1);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
        }

        [Fact]
        public void Factory2()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory2(typeof(Data2).GetConstructors().First());

            var data = (Data2)factory(1, "2");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
        }

        [Fact]
        public void Factory3()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory3(typeof(Data3).GetConstructors().First());

            var data = (Data3)factory(1, "2", 3);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
        }

        [Fact]
        public void Factory4()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory4(typeof(Data4).GetConstructors().First());

            var data = (Data4)factory(1, "2", 3, "4");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
        }

        [Fact]
        public void Factory5()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory5(typeof(Data5).GetConstructors().First());

            var data = (Data5)factory(1, "2", 3, "4", 5);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
        }

        [Fact]
        public void Factory6()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory6(typeof(Data6).GetConstructors().First());

            var data = (Data6)factory(1, "2", 3, "4", 5, "6");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
        }

        [Fact]
        public void Factory7()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory7(typeof(Data7).GetConstructors().First());

            var data = (Data7)factory(1, "2", 3, "4", 5, "6", 7);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
        }

        [Fact]
        public void Factory8()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory8(typeof(Data8).GetConstructors().First());

            var data = (Data8)factory(1, "2", 3, "4", 5, "6", 7, "8");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
        }

        [Fact]
        public void Factory9()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory9(typeof(Data9).GetConstructors().First());

            var data = (Data9)factory(1, "2", 3, "4", 5, "6", 7, "8", 9);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
        }

        [Fact]
        public void Factory10()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory10(typeof(Data10).GetConstructors().First());

            var data = (Data10)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
        }

        [Fact]
        public void Factory11()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory11(typeof(Data11).GetConstructors().First());

            var data = (Data11)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
        }

        [Fact]
        public void Factory12()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory12(typeof(Data12).GetConstructors().First());

            var data = (Data12)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11, "12");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
            Assert.Equal("12", data.Param12);
        }

        [Fact]
        public void Factory13()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory13(typeof(Data13).GetConstructors().First());

            var data = (Data13)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11, "12", 13);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
            Assert.Equal("12", data.Param12);
            Assert.Equal(13, data.Param13);
        }

        [Fact]
        public void Factory14()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory14(typeof(Data14).GetConstructors().First());

            var data = (Data14)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11, "12", 13, "14");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
            Assert.Equal("12", data.Param12);
            Assert.Equal(13, data.Param13);
            Assert.Equal("14", data.Param14);
        }

        [Fact]
        public void Factory15()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory15(typeof(Data15).GetConstructors().First());

            var data = (Data15)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11, "12", 13, "14", 15);
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
            Assert.Equal("12", data.Param12);
            Assert.Equal(13, data.Param13);
            Assert.Equal("14", data.Param14);
            Assert.Equal(15, data.Param15);
        }

        [Fact]
        public void Factory16()
        {
            var factory = ReflectionDelegateFactory.Default.CreateFactory16(typeof(Data16).GetConstructors().First());

            var data = (Data16)factory(1, "2", 3, "4", 5, "6", 7, "8", 9, "10", 11, "12", 13, "14", 15, "16");
            Assert.NotNull(data);
            Assert.Equal(1, data.Param1);
            Assert.Equal("2", data.Param2);
            Assert.Equal(3, data.Param3);
            Assert.Equal("4", data.Param4);
            Assert.Equal(5, data.Param5);
            Assert.Equal("6", data.Param6);
            Assert.Equal(7, data.Param7);
            Assert.Equal("8", data.Param8);
            Assert.Equal(9, data.Param9);
            Assert.Equal("10", data.Param10);
            Assert.Equal(11, data.Param11);
            Assert.Equal("12", data.Param12);
            Assert.Equal(13, data.Param13);
            Assert.Equal("14", data.Param14);
            Assert.Equal(15, data.Param15);
            Assert.Equal("16", data.Param16);
        }
    }
}