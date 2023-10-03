using System;

using WorkMapper;
using WorkMapper.Functions;

namespace WorkIL
{
    public sealed class NullGuardMapper
    {
        public Destination? ClassFunc(Source? source)
        {
            if (source is null)
            {
                return null;
            }

            var destination = new Destination();
            destination.Value = 1;
            return destination;
        }

        public void ClassAction(Source? source, Destination? destination)
        {
            if (source is null)
            {
                return;
            }

            destination.Value = 1;
        }

        public StructDestination? NullableFuncToNullable(StructSource? source)
        {
            if (source is null)
            {
                return null;
            }

            var destination = new StructDestination();
            destination.Value = 1;
            return destination;
        }

        public StructDestination NullableFuncToStruct(StructSource? source)
        {
            if (source is null)
            {
                return default;
            }

            var destination = new StructDestination();
            destination.Value = 1;
            return destination;
        }

        public void NullableAction(StructSource? source, StructDestination destination)
        {
            if (source is null)
            {
                return;
            }

            destination.Value = 1;
        }
    }
}
