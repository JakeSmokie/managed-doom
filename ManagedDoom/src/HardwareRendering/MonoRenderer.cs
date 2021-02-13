using System;
using System.IO;
using System.Linq;
using ManagedDoom.SoftwareRendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ManagedDoom.HardwareRendering
{
    public class MonoRenderer
    {
        private struct RenderMesh
        {
            public Texture2D Texture2D;
            public int VertexesOffset;
            public int IndicesOffset;
            public int VertexesCount;
            public int IndicesCount;
            public float Alpha;
            public bool Sprite;
        }

        private readonly GraphicsDeviceManager graphicsDeviceManager;
        private readonly ThreeDRenderer threeDRenderer;
        private readonly CommonResource commonResource;
        private readonly GraphicsDevice graphicsDevice;

        private Vector3 camPosition = new Vector3(9856, 105, -256);
        private Vector3 camTarget = new Vector3(0, 105, -256);
        private float angle = 0;

        private Matrix projectionMatrix;
        private Matrix viewMatrix;
        private Matrix worldMatrix;

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private Player player;

        private readonly VertexPositionTexture[] triangleVertices = new VertexPositionTexture[40000];
        private readonly int[] triangleIndices = new int[60000];
        private readonly RenderMesh[] meshes = new RenderMesh[60000];

        private readonly BasicEffect basicEffect;
        private readonly AlphaTestEffect alphaMaskEffect;

        private static readonly DepthStencilState alwaysStencilState = new()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1,
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,
        };

        private static readonly DepthStencilState equalStencilState = new()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.LessEqual,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
        };

        private int vertexesAmount;
        private int indicesAmount;
        private int meshesAmount;

        public MonoRenderer(GraphicsDeviceManager graphicsDeviceManager, ThreeDRenderer threeDRenderer, CommonResource commonResource)
        {
            this.graphicsDeviceManager = graphicsDeviceManager;
            this.threeDRenderer = threeDRenderer;
            this.commonResource = commonResource;

            graphicsDevice = graphicsDeviceManager.GraphicsDevice;
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(65f), graphicsDevice.Viewport.AspectRatio, 1f, 10000f);

            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), triangleVertices.Length, BufferUsage.WriteOnly);
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, triangleIndices.Length, BufferUsage.WriteOnly);

            basicEffect = new BasicEffect(graphicsDevice)
            {
                Alpha = 1,
                VertexColorEnabled = false,
                LightingEnabled = false,
                TextureEnabled = true
            };

            alphaMaskEffect = new AlphaTestEffect(graphicsDevice)
            {
                AlphaFunction = CompareFunction.Greater,
                ReferenceAlpha = 0
            };
        }

        public void SetProjection(Player player)
        {
            this.player = player;

            vertexesAmount = 0;
            indicesAmount = 0;
            meshesAmount = 0;

            var viewX = player.Mobj.X.ToFloat();
            var viewY = player.Mobj.Y.ToFloat();
            var viewZ = player.ViewZ.ToFloat();
            var viewAngle = player.Mobj.Angle.ToRadian();

            camPosition = new Vector3(-viewX, viewZ, viewY);
            camTarget = camPosition + new Vector3(-MathF.Cos((float) viewAngle), 0, MathF.Sin((float) viewAngle));
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
            worldMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            basicEffect.Projection = projectionMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.World = worldMatrix;

            alphaMaskEffect.Projection = projectionMatrix;
            alphaMaskEffect.View = viewMatrix;
            alphaMaskEffect.World = worldMatrix;
        }

        private static bool Drawn = false;
        private static int Frame = 0;

        public void Render()
        {
            Frame++;

            var random = new Random(Frame / 35);

            graphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = true
            };

            graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            // graphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            // graphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            graphicsDevice.BlendState = BlendState.NonPremultiplied;

            vertexBuffer.SetData(triangleVertices);
            indexBuffer.SetData(triangleIndices);
            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;

            graphicsDevice.Clear(Color.Aqua);

            if (false)
            {
                graphicsDevice.DepthStencilState = alwaysStencilState;
                for (var index = 0; index < meshesAmount; index++)
                {
                    var mesh = meshes[index];

                    alphaMaskEffect.Alpha = mesh.Alpha;
                    alphaMaskEffect.Texture = mesh.Texture2D;

                    foreach (var pass in alphaMaskEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0,
                            mesh.IndicesOffset,
                            mesh.IndicesCount / 3
                        );
                    }
                }
            }

            // graphicsDevice.DepthStencilState = equalStencilState;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            for (var index = 0; index < meshesAmount; index++)
            {
                var mesh = meshes[index];
                basicEffect.Alpha = mesh.Alpha;
                basicEffect.Texture = mesh.Texture2D;

                foreach (var pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        mesh.IndicesOffset,
                        mesh.IndicesCount / 3
                    );
                }
            }
        }

        public void DrawSprite(ThreeDRenderer.VisSprite sprite)
        {
            var thing = sprite.Thing;

            var bottomZ = thing.Z.ToFloat();
            var topZ = thing.Z.ToFloat() + sprite.Patch.Height;
            var thingX = -thing.X.ToFloat();
            var thingY = thing.Y.ToFloat();
            var radius = sprite.Patch.Width / 2f;

            var playerX = -player.Mobj.X.ToFloat();
            var playerY = player.Mobj.Y.ToFloat();

            var playerAngle = -MathF.Atan((thingX - playerX) / (thingY - playerY));
            var box1 = new Vector2(thingX + MathF.Cos(playerAngle) * radius, thingY + MathF.Sin(playerAngle) * radius);
            var box2 = new Vector2(thingX - MathF.Cos(playerAngle) * radius, thingY - MathF.Sin(playerAngle) * radius);

            // var flip = sprite.Flip; // TODO: Fix Flip
            // var tex1 = flip ? 1 : 0;
            // var tex2 = flip ? 0 : 1;
            var tex1 = 0;
            var tex2 = 1;

            ref var mesh = ref DrawRectangle(
                new(new(box1.X, topZ, box1.Y), new(tex1, 0)),
                new(new(box2.X, topZ, box2.Y), new(tex2, 0)),
                new(new(box1.X, bottomZ, box1.Y), new(tex1, 1)),
                new(new(box2.X, bottomZ, box2.Y), new(tex2, 1))
            );

            mesh.Texture2D = sprite.Patch.Texture2D;
            mesh.Alpha = (sprite.MobjFlags & MobjFlags.Shadow) != 0 ? 0.5f : 1;
            mesh.Sprite = true;
        }

        public void DrawSolidWall(Seg seg)
        {
            if (seg.BackSector == null)
            {
                DrawMiddleSolidWall(seg);
                return;
            }

            DrawSolidWallTopOrBottom(true, seg);
            DrawSolidWallTopOrBottom(false, seg);
        }

        public void DrawPassWall(Seg seg)
        {
            DrawMaskedRange(seg);

            DrawSolidWallTopOrBottom(true, seg);
            DrawSolidWallTopOrBottom(false, seg);
        }

        private void DrawSolidWallTopOrBottom(bool top, Seg seg)
        {
            var frontFloor = seg.FrontSector.FloorHeight.ToFloat();
            var backFloor = seg.BackSector.FloorHeight.ToFloat();
            var frontCeiling = seg.FrontSector.CeilingHeight.ToFloat();
            var backCeiling = seg.BackSector.CeilingHeight.ToFloat();
            var texture = top ? seg.SideDef.TopTexture : seg.SideDef.BottomTexture;

            var z1 = top ? backCeiling : backFloor;
            var z2 = top ? frontCeiling : frontFloor;

            if (texture <= 0)
            {
                return;
            }

            var floor = MathF.Min(z1, z2);
            var ceiling = MathF.Max(z1, z2);

            var v1 = seg.Vertex1.ToGlVector2();
            var v2 = seg.Vertex2.ToGlVector2();

            var wallTexture = commonResource
                .Textures[threeDRenderer.World.Specials.TextureTranslation[texture]]
                .Composite;

            var width = Vector2.Distance(v1, v2);
            var height = ceiling - floor;
            var xOffset = seg.SideDef.TextureOffset.ToFloat() + seg.Offset.ToFloat();
            var yOffset = seg.SideDef.RowOffset.ToFloat();

            var texWidth = wallTexture.Width;
            var texHeight = wallTexture.Height;

            if ((seg.LineDef.Flags & LineFlags.DontPegTop) == 0 && top)
            {
                yOffset += backCeiling - backFloor;
            }

            if ((seg.LineDef.Flags & LineFlags.DontPegBottom) != 0 && !top)
            {
                yOffset += frontCeiling - backFloor;
            }

            ref var mesh = ref DrawRectangle(
                new(new(v1.X, ceiling, v1.Y), new(xOffset / texWidth, yOffset / texHeight)),
                new(new(v2.X, ceiling, v2.Y), new((xOffset + width) / texWidth, yOffset / texHeight)),
                new(new(v1.X, floor, v1.Y), new(xOffset / texWidth, (yOffset + height) / texHeight)),
                new(new(v2.X, floor, v2.Y), new((xOffset + width) / texWidth, (yOffset + height) / texHeight))
            );

            mesh.Texture2D = wallTexture.Texture2D;
        }

        private void DrawMaskedRange(Seg seg)
        {
            if (seg.SideDef.MiddleTexture <= 0)
            {
                return;
            }

            var wallTexture = commonResource
                .Textures[threeDRenderer.World.Specials.TextureTranslation[seg.SideDef.MiddleTexture]]
                .Composite;

            var texWidth = wallTexture.Width;
            var texHeight = wallTexture.Height;

            var v1 = seg.Vertex1.ToGlVector2();
            var v2 = seg.Vertex2.ToGlVector2();
            var width = Vector2.Distance(v1, v2);

            var xOffset = seg.SideDef.TextureOffset.ToFloat() + seg.Offset.ToFloat();
            var yOffset = seg.SideDef.RowOffset.ToFloat();

            var floor = MathF.Max(seg.BackSector.FloorHeight.ToFloat(), seg.FrontSector.FloorHeight.ToFloat());
            var ceiling = MathF.Min(seg.BackSector.CeilingHeight.ToFloat(), seg.FrontSector.CeilingHeight.ToFloat());

            var peg = (seg.LineDef.Flags & LineFlags.DontPegBottom) == 0;

            float zTop, zBottom;

            if (peg)
            {
                zTop = ceiling - yOffset;
                zBottom = ceiling - texHeight - yOffset;
            }
            else
            {
                zTop = floor + texHeight + yOffset;
                zBottom = floor + yOffset;
            }

            ref var mesh = ref DrawRectangle(
                new(new(v1.X, zTop, v1.Y), new(xOffset / texWidth, 0)),
                new(new(v2.X, zTop, v2.Y), new((xOffset + width) / texWidth, 0)),
                new(new(v1.X, zBottom, v1.Y), new(xOffset / texWidth, 1)),
                new(new(v2.X, zBottom, v2.Y), new((xOffset + width) / texWidth, 1))
            );

            mesh.Texture2D = wallTexture.Texture2D;
            mesh.Alpha = seg.LineDef.Action == 208 ? seg.LineDef.ActionArgs[1] / 255f : 1;
        }

        private void DrawMiddleSolidWall(Seg seg)
        {
            var texture = seg.SideDef.MiddleTexture;

            if (texture <= 0)
            {
                return;
            }

            var v1 = seg.Vertex1.ToGlVector2();
            var v2 = seg.Vertex2.ToGlVector2();

            var ceiling = seg.FrontSector.CeilingHeight.ToFloat();
            var floor = seg.FrontSector.FloorHeight.ToFloat();

            var wallTexture = commonResource
                .Textures[threeDRenderer.World.Specials.TextureTranslation[texture]]
                .Composite;

            var width = Vector2.Distance(v1, v2);
            var height = ceiling - floor;
            var xOffset = seg.SideDef.TextureOffset.ToFloat() + seg.Offset.ToFloat();
            var yOffset = seg.SideDef.RowOffset.ToFloat();

            var texWidth = wallTexture.Width;
            var texHeight = wallTexture.Height;

            if ((seg.LineDef.Flags & LineFlags.DontPegBottom) != 0)
            {
                yOffset -= ceiling % texHeight;
            }

            ref var mesh = ref DrawRectangle(
                new(new(v1.X, ceiling, v1.Y), new(xOffset / texWidth, yOffset / texHeight)),
                new(new(v2.X, ceiling, v2.Y), new((xOffset + width) / texWidth, yOffset / texHeight)),
                new(new(v1.X, floor, v1.Y), new(xOffset / texWidth, (yOffset + height) / texHeight)),
                new(new(v2.X, floor, v2.Y), new((xOffset + width) / texWidth, (yOffset + height) / texHeight))
            );

            mesh.Texture2D = wallTexture.Texture2D;
        }

        private ref RenderMesh DrawRectangle(
            VertexPositionTexture topLeft,
            VertexPositionTexture topRight,
            VertexPositionTexture bottomLeft,
            VertexPositionTexture bottomRight
        )
        {
            triangleVertices[vertexesAmount + 0] = topLeft;
            triangleVertices[vertexesAmount + 1] = topRight;
            triangleVertices[vertexesAmount + 2] = bottomLeft;
            triangleVertices[vertexesAmount + 3] = bottomRight;

            triangleIndices[indicesAmount + 0] = vertexesAmount + 0;
            triangleIndices[indicesAmount + 1] = vertexesAmount + 1;
            triangleIndices[indicesAmount + 2] = vertexesAmount + 2;
            triangleIndices[indicesAmount + 3] = vertexesAmount + 1;
            triangleIndices[indicesAmount + 4] = vertexesAmount + 2;
            triangleIndices[indicesAmount + 5] = vertexesAmount + 3;

            meshes[meshesAmount] = new RenderMesh
            {
                Texture2D = null,
                IndicesOffset = indicesAmount,
                VertexesOffset = vertexesAmount,
                IndicesCount = 6,
                VertexesCount = 4,
                Alpha = 1,
                Sprite = false
            };

            meshesAmount += 1;
            vertexesAmount += 4;
            indicesAmount += 6;

            return ref meshes[meshesAmount - 1];
        }

        public void DrawSubsector(Subsector subsector)
        {
            if (subsector.Points == null)
            {
                return;
            }

            DrawSubsectorFlat(subsector, true);
            DrawSubsectorFlat(subsector, false);
        }

        private void DrawSubsectorFlat(Subsector subsector, bool floor)
        {
            var sector = subsector.Sector;
            var flatIndex = floor ? sector.FloorFlat : sector.CeilingFlat;

            if (flatIndex == commonResource.Flats.SkyFlatNumber)
            {
                return;
            }

            var flat = commonResource.Flats[flatIndex];

            var texture2D = flat.Texture2D;
            var points = subsector.Points;
            var z = floor ? sector.FloorHeight.ToFloat() : sector.CeilingHeight.ToFloat();

            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];

                triangleVertices[vertexesAmount + i].Position = new(-point.X, z, point.Y);
                triangleVertices[vertexesAmount + i].TextureCoordinate = new(-point.X / texture2D.Width, point.Y / texture2D.Height);
            }

            var indices = 0;
            for (var i = 0; i < points.Length - 2; i++)
            {
                triangleIndices[indicesAmount + indices + 0] = vertexesAmount;
                triangleIndices[indicesAmount + indices + 1] = vertexesAmount + i + 1;
                triangleIndices[indicesAmount + indices + 2] = vertexesAmount + i + 2;

                indices += 3;
            }

            meshes[meshesAmount] = new RenderMesh
            {
                Texture2D = texture2D,
                IndicesOffset = indicesAmount,
                VertexesOffset = vertexesAmount,
                IndicesCount = indices,
                VertexesCount = points.Length,
                Alpha = 1,
                Sprite = false
            };

            if (Frame == 0)
            {
                using var file = File.OpenWrite($"sprites/{flat.Name}.jpeg");
                texture2D.SaveAsJpeg(file, texture2D.Width, texture2D.Height);
            }

            meshesAmount += 1;
            vertexesAmount += points.Length;
            indicesAmount += indices;
        }
    }
}