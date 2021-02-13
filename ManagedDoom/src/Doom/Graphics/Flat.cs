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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ManagedDoom
{
    public sealed class Flat
    {
        private string name;
        private byte[] data;

        public Flat(string name, byte[] data, Palette palette)
        {
            this.name = name;
            this.data = data;

            if (palette != null)
            {
                const int width = 64;
                const int height = 64;
                var colors = palette[0];

                if (data.Length == width * height)
                {
                    var pixels = new Color[data.Length];
                    for (var x = 0; x < data.Length; x++)
                    {
                        pixels[x].PackedValue = colors[data[x]];
                    }

                    var texture2D = new Texture2D(DoomApplication.StaticGraphicsDevice, width, height);
                    texture2D.SetData(pixels);

                    Texture2D = texture2D;
                }
                else
                {
                    throw new ApplicationException($"Flat {name} is not 64x64");
                }
            }
        }

        public static Flat FromData(string name, byte[] data)
        {
            return new Flat(name, data, null);
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public byte[] Data => data;
        public Texture2D Texture2D { get; set; }
    }
}