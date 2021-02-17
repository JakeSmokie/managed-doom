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
    public sealed class LineDef
    {
        private static readonly int dataSize = 14;
        private static readonly int hexenDataSize = 16;

        private Vertex vertex1;
        private Vertex vertex2;

        private Fixed dx;
        private Fixed dy;

        private LineFlags flags;
        private LineSpecial special;
        private short tag;

        private SideDef frontSide;
        private SideDef backSide;

        private Fixed[] boundingBox;

        private SlopeType slopeType;

        private Sector frontSector;
        private Sector backSector;

        private int validCount;

        private Thinker specialData;

        private Mobj soundOrigin;

        public LineDef(
            Vertex vertex1,
            Vertex vertex2,
            LineFlags flags,
            LineSpecial special,
            short tag,
            SideDef frontSide,
            SideDef backSide)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
            this.flags = flags;
            this.special = special;
            this.tag = tag;
            this.frontSide = frontSide;
            this.backSide = backSide;

            dx = vertex2.X - vertex1.X;
            dy = vertex2.Y - vertex1.Y;

            if (dx == Fixed.Zero)
            {
                slopeType = SlopeType.Vertical;
            }
            else if (dy == Fixed.Zero)
            {
                slopeType = SlopeType.Horizontal;
            }
            else
            {
                if (dy / dx > Fixed.Zero)
                {
                    slopeType = SlopeType.Positive;
                }
                else
                {
                    slopeType = SlopeType.Negative;
                }
            }

            boundingBox = new Fixed[4];
            boundingBox[Box.Top] = Fixed.Max(vertex1.Y, vertex2.Y);
            boundingBox[Box.Bottom] = Fixed.Min(vertex1.Y, vertex2.Y);
            boundingBox[Box.Left] = Fixed.Min(vertex1.X, vertex2.X);
            boundingBox[Box.Right] = Fixed.Max(vertex1.X, vertex2.X);

            frontSector = frontSide?.Sector;
            backSector = backSide?.Sector;
        }

        public LineDef(
            Vertex vertex1,
            Vertex vertex2,
            LineFlags flags,
            LineSpecial special,
            short tag,
            SideDef frontSide,
            SideDef backSide,
            byte action,
            byte[] actionArgs,
            int id
        ) : this(vertex1, vertex2, flags, special, tag, frontSide, backSide)
        {
            Action = action;
            ActionArgs = actionArgs;
            Id = id;

            if (action == 11)
            {
                Special = (LineSpecial) 31;
            }

            if (action == 12)
            {
                Special = (LineSpecial) 1;
            }
            
            if (action == 62)
            {
                Special = (LineSpecial) 62;
                Tag = actionArgs[0];
            }
        }

        public static LineDef FromData(byte[] data, int offset, Vertex[] vertices, SideDef[] sides)
        {
            var vertex1Number = BitConverter.ToInt16(data, offset);
            var vertex2Number = BitConverter.ToInt16(data, offset + 2);
            var flags = BitConverter.ToInt16(data, offset + 4);
            var special = BitConverter.ToInt16(data, offset + 6);
            var tag = BitConverter.ToInt16(data, offset + 8);
            var side0Number = BitConverter.ToInt16(data, offset + 10);
            var side1Number = BitConverter.ToInt16(data, offset + 12);

            return new LineDef(
                vertex1Number == -1 ? null : vertices[vertex1Number],
                vertex2Number == -1 ? null : vertices[vertex2Number],
                (LineFlags) flags,
                (LineSpecial) special,
                tag,
                sides[side0Number],
                side1Number != -1 ? sides[side1Number] : null);
        }

        public static LineDef[] FromWad(Wad wad, int lump, Vertex[] vertices, SideDef[] sides)
        {
            var length = wad.GetLumpSize(lump);

            if (wad.GetLumpNumber("BEHAVIOR") != -1)
            {
                return FromWadHexen(wad, lump, vertices, sides, length);
            }
            
            if (length % dataSize == 0)
            {
                var data = wad.ReadLump(lump);
                var count = length / dataSize;
                var lines = new LineDef[count];
                ;

                for (var i = 0; i < count; i++)
                {
                    var offset = 14 * i;
                    lines[i] = FromData(data, offset, vertices, sides);
                }

                return lines;
            }

            throw new Exception();
        }

        private static LineDef[] FromWadHexen(Wad wad, int lump, Vertex[] vertices, SideDef[] sides, int length)
        {
            var data = wad.ReadLump(lump);
            var count = length / hexenDataSize;
            var lines = new LineDef[count];

            for (var i = 0; i < count; i++)
            {
                var offset = hexenDataSize * i;
                var span = new ReadOnlySpan<byte>(data, offset, hexenDataSize);

                lines[i] = FromDataHexen(i, span, vertices, sides);
            }

            return lines;
        }

        private static LineDef FromDataHexen(int i, ReadOnlySpan<byte> data, Vertex[] vertices, SideDef[] sides)
        {
            var vertex1Number = BitConverter.ToInt16(data);
            var vertex2Number = BitConverter.ToInt16(data[2..]);
            var flags = BitConverter.ToInt16(data[4..]);
            var action = data[6];
            var actionArgs = data[7..12].ToArray();
            var side0Number = BitConverter.ToInt16(data[12..]);
            var side1Number = BitConverter.ToInt16(data[14..]);

            return new LineDef(
                vertices[vertex1Number],
                vertices[vertex2Number],
                (LineFlags) flags,
                0,
                0,
                sides[side0Number],
                side1Number != -1 ? sides[side1Number] : null,
                action,
                actionArgs,
                i
            );
        }

        public Vertex Vertex1 => vertex1;
        public Vertex Vertex2 => vertex2;

        public Fixed Dx => dx;
        public Fixed Dy => dy;

        public LineFlags Flags
        {
            get => flags;
            set => flags = value;
        }

        public LineSpecial Special
        {
            get => special;
            set => special = value;
        }

        public short Tag
        {
            get => tag;
            set => tag = value;
        }

        public byte Action { get; set; }
        public byte[] ActionArgs { get; set; }
        public int Id { get; set; }

        public SideDef FrontSide => frontSide;
        public SideDef BackSide => backSide;

        public Fixed[] BoundingBox => boundingBox;

        public SlopeType SlopeType => slopeType;

        public Sector FrontSector => frontSector;
        public Sector BackSector => backSector;

        public int ValidCount
        {
            get => validCount;
            set => validCount = value;
        }

        public Thinker SpecialData
        {
            get => specialData;
            set => specialData = value;
        }

        public Mobj SoundOrigin
        {
            get => soundOrigin;
            set => soundOrigin = value;
        }
    }
}