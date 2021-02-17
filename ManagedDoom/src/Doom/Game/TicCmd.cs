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
    public sealed class TicCmd
    {
        public void Clear()
        {
            ForwardMove = 0;
            SideMove = 0;
            AngleTurn = 0;
            Buttons = 0;
            Pitch = 0;
        }

        public void CopyFrom(TicCmd cmd)
        {
            ForwardMove = cmd.ForwardMove;
            SideMove = cmd.SideMove;
            AngleTurn = cmd.AngleTurn;
            Buttons = cmd.Buttons;
            Pitch = cmd.Pitch;
        }

        public sbyte ForwardMove { get; set; }
        public sbyte SideMove { get; set; }
        public short AngleTurn { get; set; }
        public byte Buttons { get; set; }
        public int Pitch { get; set; }
    }
}
