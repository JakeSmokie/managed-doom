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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using ManagedDoom.Extensions;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Shapes;

namespace ManagedDoom
{
    public sealed class Map
    {
        private TextureLookup textures;
        private FlatLookup flats;
        private TextureAnimation animation;

        private World world;

        private Vertex[] vertices;
        private Sector[] sectors;
        private SideDef[] sides;
        private LineDef[] lines;
        private Seg[] segs;
        private Subsector[] subsectors;
        private Node[] nodes;
        private MapThing[] things;
        private BlockMap blockMap;
        private Reject reject;

        private Texture skyTexture;

        private string title;

        public Map(CommonResource resorces, World world)
            : this(resorces.Wad, resorces.Textures, resorces.Flats, resorces.Animation, world)
        {
        }

        public Map(Wad wad, TextureLookup textures, FlatLookup flats, TextureAnimation animation, World world)
        {
            try
            {
                this.textures = textures;
                this.flats = flats;
                this.animation = animation;
                this.world = world;

                var options = world.Options;

                string name;
                if (wad.GameMode == GameMode.Commercial)
                {
                    name = "MAP" + options.Map.ToString("00");
                }
                else
                {
                    name = "E" + options.Episode + "M" + options.Map;
                }

                Console.Write("Load map '" + name + "': ");

                var map = wad.GetLumpNumber(name);

                if (map == -1)
                {
                    throw new Exception("Map '" + name + "' was not found!");
                }

                vertices = Vertex.FromWad(wad, map + 4);
                sectors = Sector.FromWad(wad, map + 8, flats);
                sides = SideDef.FromWad(wad, map + 3, textures, sectors);
                lines = LineDef.FromWad(wad, map + 2, vertices, sides);
                segs = Seg.FromWad(wad, map + 5, vertices, lines);
                subsectors = Subsector.FromWad(wad, map + 6, segs);
                nodes = Node.FromWad(wad, map + 7, subsectors);
                things = MapThing.FromWad(wad, map + 1);
                blockMap = BlockMap.FromWad(wad, map + 10, lines);
                reject = Reject.FromWad(wad, map + 9, sectors);

                SetupSubsectorShapes(new List<int>(), nodes.Length - 1);
                GroupLines();

                skyTexture = GetSkyTextureByMapName(name);

                if (options.GameMode == GameMode.Commercial)
                {
                    switch (options.MissionPack)
                    {
                        case MissionPack.Plutonia:
                            title = DoomInfo.MapTitles.Plutonia[options.Map - 1];
                            break;
                        case MissionPack.Tnt:
                            title = DoomInfo.MapTitles.Tnt[options.Map - 1];
                            break;
                        default:
                            title = DoomInfo.MapTitles.Doom2[options.Map - 1];
                            break;
                    }
                }
                else
                {
                    title = DoomInfo.MapTitles.Doom[options.Episode - 1][options.Map - 1];
                }

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private void SetupSubsectorShapes(List<int> nodesStack, int nodeIndex)
        {
            if (Node.IsSubsector(nodeIndex))
            {
                SetupSubsectorShape(nodesStack, nodeIndex == -1 ? 0 : Node.GetSubsector(nodeIndex));
                return;
            }

            nodesStack.Add(nodeIndex);

            SetupSubsectorShapes(nodesStack, nodes[nodeIndex].Children[0]);
            SetupSubsectorShapes(nodesStack, nodes[nodeIndex].Children[1]);

            nodesStack.RemoveAt(nodesStack.Count - 1);
        }

        private void SetupSubsectorShape(List<int> nodesStack, int subsectorIndex)
        {
            var node = nodes[nodesStack[0]];

            var a1x = node.BoundingBox[0][2].ToFloat();
            var a1y = node.BoundingBox[0][0].ToFloat();
            var a2x = node.BoundingBox[0][3].ToFloat();
            var a2y = node.BoundingBox[0][1].ToFloat();
            var b1x = node.BoundingBox[1][2].ToFloat();
            var b1y = node.BoundingBox[1][0].ToFloat();
            var b2x = node.BoundingBox[1][3].ToFloat();
            var b2y = node.BoundingBox[1][1].ToFloat();
            var x1 = MathF.Min(a1x, MathF.Min(a2x, MathF.Min(b1x, b2x)));
            var y1 = MathF.Min(a1y, MathF.Min(a2y, MathF.Min(b1y, b2y)));
            var x2 = MathF.Max(a1x, MathF.Max(a2x, MathF.Max(b1x, b2x)));
            var y2 = MathF.Max(a1y, MathF.Max(a2y, MathF.Max(b1y, b2y)));

            var polygon = new List<Vector2>
            {
                new(x1, y1),
                new(x2, y1),
                new(x2, y2),
                new(x1, y2)
            };

            var n1 = NodeSplit(nodes[nodesStack[0]]);
            var n2 = NodeSplit(nodes[nodesStack[0]]);
            var n3 = NodeSplit(nodes[nodesStack[0]]);
            var n4 = NodeSplit(nodes[nodesStack[0]]);


            static (Vector2, Vector2) NodeSplit(Node node)
            {
                var x = node.X.ToFloat();
                var y = node.Y.ToFloat();
                var dx = node.Dx.ToFloat();
                var dy = node.Dy.ToFloat();

                return (new(x, y), new(x + dx, y + dy));
            }

            // if (MathF.Abs(x2 - x1) < float.Epsilon || MathF.Abs(y2 - y1) < float.Epsilon)
            // {
            //     ;
            // }
            //
            // var subsectorSegs = new ReadOnlySpan<Seg>(segs, subsector.FirstSeg, subsector.SegCount);
            //
            // foreach (var seg in subsectorSegs)
            // {
            //     var segV1 = seg.Vertex1.ToVector2();
            //     var segV2 = seg.Vertex2.ToVector2();
            //
            //     for (var i = 0; i < polygon.Count; i++)
            //     {
            //         var polyV1 = polygon[i];
            //         var polyV2 = polygon[(i + 1) % polygon.Count];
            //
            //         if (GeometryHelper.SegmentsIntersect(segV1, segV2, polyV1, polyV2, out var point))
            //         {
            //             ;
            //         }
            //     }
            // }
        }

        private void GroupLines()
        {
            var sectorLines = new List<LineDef>();
            var boundingBox = new Fixed[4];

            foreach (var line in lines)
            {
                if (line.Special != 0)
                {
                    var so = new Mobj(world);
                    so.X = (line.Vertex1.X + line.Vertex2.X) / 2;
                    so.Y = (line.Vertex1.Y + line.Vertex2.Y) / 2;
                    line.SoundOrigin = so;
                }
            }

            foreach (var sector in sectors)
            {
                sectorLines.Clear();
                Box.Clear(boundingBox);

                foreach (var line in lines)
                {
                    if (line.FrontSector == sector || line.BackSector == sector)
                    {
                        sectorLines.Add(line);
                        Box.AddPoint(boundingBox, line.Vertex1.X, line.Vertex1.Y);
                        Box.AddPoint(boundingBox, line.Vertex2.X, line.Vertex2.Y);
                    }
                }

                sector.Lines = sectorLines.ToArray();

                // Set the degenmobj_t to the middle of the bounding box.
                sector.SoundOrigin = new Mobj(world);
                sector.SoundOrigin.X = (boundingBox[Box.Right] + boundingBox[Box.Left]) / 2;
                sector.SoundOrigin.Y = (boundingBox[Box.Top] + boundingBox[Box.Bottom]) / 2;

                sector.BlockBox = new int[4];
                int block;

                // Adjust bounding box to map blocks.
                block = (boundingBox[Box.Top] - blockMap.OriginY + GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
                block = block >= blockMap.Height ? blockMap.Height - 1 : block;
                sector.BlockBox[Box.Top] = block;

                block = (boundingBox[Box.Bottom] - blockMap.OriginY - GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
                block = block < 0 ? 0 : block;
                sector.BlockBox[Box.Bottom] = block;

                block = (boundingBox[Box.Right] - blockMap.OriginX + GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
                block = block >= blockMap.Width ? blockMap.Width - 1 : block;
                sector.BlockBox[Box.Right] = block;

                block = (boundingBox[Box.Left] - blockMap.OriginX - GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
                block = block < 0 ? 0 : block;
                sector.BlockBox[Box.Left] = block;
            }
        }

        private Texture GetSkyTextureByMapName(string name)
        {
            if (name.Length == 4)
            {
                switch (name[1])
                {
                    case '1':
                        return textures["SKY1"];
                    case '2':
                        return textures["SKY2"];
                    case '3':
                        return textures["SKY3"];
                    default:
                        return textures["SKY4"];
                }
            }
            else
            {
                var number = int.Parse(name.Substring(3));
                if (number <= 11)
                {
                    return textures["SKY1"];
                }
                else if (number <= 21)
                {
                    return textures["SKY2"];
                }
                else
                {
                    return textures["SKY3"];
                }
            }
        }

        public TextureLookup Textures => textures;
        public FlatLookup Flats => flats;
        public TextureAnimation Animation => animation;

        public Vertex[] Vertices => vertices;
        public Sector[] Sectors => sectors;
        public SideDef[] Sides => sides;
        public LineDef[] Lines => lines;
        public Seg[] Segs => segs;
        public Subsector[] Subsectors => subsectors;
        public Node[] Nodes => nodes;
        public MapThing[] Things => things;
        public BlockMap BlockMap => blockMap;
        public Reject Reject => reject;
        public Texture SkyTexture => skyTexture;
        public int SkyFlatNumber => flats.SkyFlatNumber;
        public string Title => title;


        private static readonly Bgm[] e4BgmList = new Bgm[]
        {
            Bgm.E3M4, // American   e4m1
            Bgm.E3M2, // Romero     e4m2
            Bgm.E3M3, // Shawn      e4m3
            Bgm.E1M5, // American   e4m4
            Bgm.E2M7, // Tim        e4m5
            Bgm.E2M4, // Romero     e4m6
            Bgm.E2M6, // J.Anderson e4m7 CHIRON.WAD
            Bgm.E2M5, // Shawn      e4m8
            Bgm.E1M9 // Tim        e4m9
        };

        public static Bgm GetMapBgm(GameOptions options)
        {
            Bgm bgm;
            if (options.GameMode == GameMode.Commercial)
            {
                bgm = Bgm.RUNNIN + options.Map - 1;
            }
            else
            {
                if (options.Episode < 4)
                {
                    bgm = Bgm.E1M1 + (options.Episode - 1) * 9 + options.Map - 1;
                }
                else
                {
                    bgm = e4BgmList[options.Map - 1];
                }
            }

            return bgm;
        }
    }
}