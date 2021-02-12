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

            graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            graphicsDevice.BlendState = BlendState.NonPremultiplied;

            vertexBuffer.SetData(triangleVertices);
            indexBuffer.SetData(triangleIndices);
            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;

            graphicsDevice.Clear(Color.Aqua);

            if (true)
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

            graphicsDevice.DepthStencilState = equalStencilState;
            for (var index = meshesAmount - 1; index >= 0; index--)
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

        public void DrawMaskedRange(Seg seg)
        {
            if (seg.SideDef.MiddleTexture <= 0)
            {
                return;
            }
            
            var (x1, y1) = seg.Vertex1.ToGlVector2();
            var (x2, y2) = seg.Vertex2.ToGlVector2();

            var floor = MathF.Max(seg.BackSector.FloorHeight.ToFloat(), seg.FrontSector.FloorHeight.ToFloat());
            var ceiling = MathF.Min(seg.BackSector.CeilingHeight.ToFloat(), seg.FrontSector.CeilingHeight.ToFloat());

            ref var mesh = ref DrawRectangle(
                new(new(x1, ceiling, y1), new(0, 0)),
                new(new(x2, ceiling, y2), new(1, 0)),
                new(new(x1, floor, y1), new(0, 1)),
                new(new(x2, floor, y2), new(1, 1))
            );

            var wallTexture = commonResource
                .Textures[threeDRenderer.World.Specials.TextureTranslation[seg.SideDef.MiddleTexture]]
                .Composite.Texture2D;

            mesh.Texture2D = wallTexture;
            mesh.Alpha = seg.LineDef.Action == 208 ? seg.LineDef.ActionArgs[1] / 255f : 1;
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

            var frontFloor = seg.FrontSector.FloorHeight.ToFloat();
            var backFloor = seg.BackSector.FloorHeight.ToFloat();
            var frontCeiling = seg.FrontSector.CeilingHeight.ToFloat();
            var backCeiling = seg.BackSector.CeilingHeight.ToFloat();

            DrawSolidWallTopOrBottom(seg, frontCeiling, backCeiling, seg.SideDef.TopTexture);
            DrawSolidWallTopOrBottom(seg, frontFloor, backFloor, seg.SideDef.BottomTexture);
        }

        private void DrawSolidWallTopOrBottom(Seg seg, float z1, float z2, int texture)
        {
            if (texture <= 0)
            {
                return;
            }
            
            var (x1, y1) = seg.Vertex1.ToGlVector2();
            var (x2, y2) = seg.Vertex2.ToGlVector2();

            ref var mesh = ref DrawRectangle(
                new(new(x1, z1, y1), new(0, 0)),
                new(new(x2, z1, y2), new(1, 0)),
                new(new(x1, z2, y1), new(0, 1)),
                new(new(x2, z2, y2), new(1, 1))
            );


            var wallTexture = commonResource
                .Textures[threeDRenderer.World.Specials.TextureTranslation[texture]]
                .Composite.Texture2D;

            mesh.Texture2D = wallTexture;
        }

        private void DrawMiddleSolidWall(Seg seg)
        {
            var texture = seg.SideDef.MiddleTexture;

            if (texture <= 0)
            {
                return;
            }
            
            var (x1, y1) = seg.Vertex1.ToGlVector2();
            var (x2, y2) = seg.Vertex2.ToGlVector2();

            var ceiling = seg.FrontSector.CeilingHeight.ToFloat();
            var floor = seg.FrontSector.FloorHeight.ToFloat();

            ref var mesh = ref DrawRectangle(
                new(new(x1, ceiling, y1), new(0, 0)),
                new(new(x2, ceiling, y2), new(1, 0)),
                new(new(x1, floor, y1), new(0, 1)),
                new(new(x2, floor, y2), new(1, 1))
            );


            var wallTexture = commonResource
                .Textures[threeDRenderer.World.Specials.TextureTranslation[texture]]
                .Composite.Texture2D;

            mesh.Texture2D = wallTexture;
        }

        public void DrawPassWall(Seg seg)
        {
            DrawMaskedRange(seg);

            var frontFloor = seg.FrontSector.FloorHeight.ToFloat();
            var backFloor = seg.BackSector.FloorHeight.ToFloat();
            var frontCeiling = seg.FrontSector.CeilingHeight.ToFloat();
            var backCeiling = seg.BackSector.CeilingHeight.ToFloat();

            DrawSolidWallTopOrBottom(seg, frontCeiling, backCeiling, seg.SideDef.TopTexture);
            DrawSolidWallTopOrBottom(seg, frontFloor, backFloor, seg.SideDef.BottomTexture);
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
    }
}