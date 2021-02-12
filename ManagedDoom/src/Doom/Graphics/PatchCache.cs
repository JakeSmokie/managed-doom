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


using System.Collections.Generic;

namespace ManagedDoom
{
    public sealed class PatchCache
    {
        private Wad wad;
        private Dictionary<string, Patch> cache;
        private Palette palette;

        public PatchCache(Wad wad, Palette palette)
        {
            this.wad = wad;
            this.palette = palette;

            cache = new Dictionary<string, Patch>();
        }

        public Patch this[string name]
        {
            get
            {
                if (!cache.TryGetValue(name, out var patch))
                {
                    patch = Patch.FromWad(wad, name, palette);
                    cache.Add(name, patch);
                }

                return patch;
            }
        }

        public int GetWidth(string name)
        {
            return this[name].Width;
        }

        public int GetHeight(string name)
        {
            return this[name].Height;
        }
    }
}
