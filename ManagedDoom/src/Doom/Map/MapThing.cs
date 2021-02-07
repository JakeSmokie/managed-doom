//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom
{
    public sealed class MapThing
    {
        private static readonly int dataSize = 10;
        private static readonly int hexenDataSize = 20;

        public static MapThing Empty = new MapThing(
            Fixed.Zero,
            Fixed.Zero,
            Angle.Ang0,
            0,
            0);

        private Fixed x;
        private Fixed y;
        private Angle angle;
        private int type;
        private ThingFlags flags;

        public MapThing(
            Fixed x,
            Fixed y,
            Angle angle,
            int type,
            ThingFlags flags)
        {
            this.x = x;
            this.y = y;
            this.angle = angle;
            this.type = type;
            this.flags = flags;
        }

        public static MapThing FromData(byte[] data, int offset)
        {
            var x = BitConverter.ToInt16(data, offset);
            var y = BitConverter.ToInt16(data, offset + 2);
            var angle = BitConverter.ToInt16(data, offset + 4);
            var type = BitConverter.ToInt16(data, offset + 6);
            var flags = BitConverter.ToInt16(data, offset + 8);

            return new MapThing(
                Fixed.FromInt(x),
                Fixed.FromInt(y),
                new Angle(ManagedDoom.Angle.Ang45.Data * (uint)(angle / 45)),
                type,
                (ThingFlags)flags);
        }

        public static MapThing[] FromWad(Wad wad, int lump)
        {
            if (wad.GetLumpNumber("BEHAVIOR") != -1)
            {
                return FromWadHexen(wad, lump);
            }

            var length = wad.GetLumpSize(lump);
            if (length % dataSize != 0)
            {
                throw new Exception();
            }

            var data = wad.ReadLump(lump);
            var count = length / dataSize;
            var things = new MapThing[count];

            for (var i = 0; i < count; i++)
            {
                var offset = dataSize * i;
                things[i] = FromData(data, offset);
            }

            return things;
        }

        private static MapThing[] FromWadHexen(Wad wad, int lump)
        {
            var length = wad.GetLumpSize(lump);
            var data = new ReadOnlySpan<byte>(wad.ReadLump(lump));
            var count = length / hexenDataSize;
            var things = new MapThing[count];

            for (var i = 0; i < count; i++)
            {
                var offset = hexenDataSize * i;
                things[i] = FromDataHexen(data.Slice(offset, hexenDataSize));
            }

            return things;
        }

        private static MapThing FromDataHexen(ReadOnlySpan<byte> data)
        {
            var x = BitConverter.ToInt16(data.Slice(2));
            var y = BitConverter.ToInt16(data.Slice(4));
            var angle = BitConverter.ToInt16(data.Slice(8));
            var type = BitConverter.ToInt16(data.Slice(10));
            var flags = BitConverter.ToInt16(data.Slice(12));

            return new MapThing(
                Fixed.FromInt(x),
                Fixed.FromInt(y),
                new Angle(ManagedDoom.Angle.Ang45.Data * (uint)(angle / 45)),
                type,
                (ThingFlags)flags);
        }

        public Fixed X => x;
        public Fixed Y => y;
        public Angle Angle => angle;

        public int Type
        {
            get => type;
            set => type = value;
        }

        public ThingFlags Flags => flags;
    }
}
