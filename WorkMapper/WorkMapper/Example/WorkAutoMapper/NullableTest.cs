using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;

namespace WorkAutoMapper
{
    public static class NullableTest
    {
        public static void Run()
        {
            TestClassToStruct();
            TestClassToNullable();
            TestNullableToClass();
            TestNullableToNullable();
            TestNullableToStruct();
            TestStructToNullable();
        }

        private static void TestClassToStruct()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, StructDestination>();
            });
            var mapper = config.CreateMapper();

            // OK
            var d1 = mapper.Map<StructDestination>(new Source { Data = 1 });
            // +OK D:nullableを透過的に扱う
            var d2 = mapper.Map<StructDestination?>(new Source { Data = 1 });
        }

        private static void TestClassToNullable()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, StructDestination?>();
            });
            var mapper = config.CreateMapper();

            // 未サポート
            //var d1 = mapper.Map<StructDestination>(new Source { Data = 1 });
            // null
            var d2 = mapper.Map<StructDestination?>(new Source { Data = 1 });
        }

        private static void TestNullableToClass()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<StructSource?, Destination>();
            });
            var mapper = config.CreateMapper();

            // 未サポート
            //var d1 = mapper.Map<Destination>(new StructSource { Data = 1 });
            // Data 0
            var d2 = mapper.Map<StructSource?, Destination>(new StructSource { Data = 1 });
        }

        private static void TestNullableToNullable()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<StructSource?, StructDestination?>();
            });
            var mapper = config.CreateMapper();

            // 未サポート
            //var d1 = mapper.Map<StructDestination?>(new StructSource { Data = 1 });
            // 未サポート
            //var d2 = mapper.Map<StructDestination>(new StructSource { Data = 1 });
            // 未サポート
            //var d3 = mapper.Map<StructSource?, StructDestination?>(new StructSource { Data = 1 });
            // 未サポート
            //var d4 = mapper.Map<StructSource?, StructDestination>(new StructSource { Data = 1 });
        }

        private static void TestNullableToStruct()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<StructSource?, StructDestination>();
            });
            var mapper = config.CreateMapper();

            // 未サポート
            //var d1 = mapper.Map<StructDestination?>(new StructSource { Data = 1 });
            // 未サポート
            //var d2 = mapper.Map<StructDestination>(new StructSource { Data = 1 });
            // 未サポート
            //var d3 = mapper.Map<StructSource?, StructDestination?>(new StructSource { Data = 1 });
            // Data 0
            var d4 = mapper.Map<StructSource?, StructDestination>(new StructSource { Data = 1 });
        }

        private static void TestStructToNullable()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<StructSource, StructDestination?>();
            });
            var mapper = config.CreateMapper();

            // null
            var d1 = mapper.Map<StructDestination?>(new StructSource { Data = 1 });
            // 未サポート
            //var d2 = mapper.Map<StructDestination>(new StructSource { Data = 1 });
            // null
            var d3 = mapper.Map<StructSource?, StructDestination?>(new StructSource { Data = 1 });
            // 未サポート
            //var d4 = mapper.Map<StructSource?, StructDestination>(new StructSource { Data = 1 });
        }

        public class Source
        {
            public int Data { get; set; }
        }

        public class Destination
        {
            public int Data { get; set; }
        }

        public struct StructSource
        {
            public int Data { get; set; }
        }

        public struct StructDestination
        {
            public int Data { get; set; }
        }
    }
}
