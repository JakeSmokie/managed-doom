using System;
using ManagedDoom.SoftwareRendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ManagedDoom.HardwareRendering
{
    public class MonoRenderer
    {
        private readonly GraphicsDeviceManager graphicsDeviceManager;
        private readonly ThreeDRenderer threeDRenderer;
        private readonly GraphicsDevice graphicsDevice;

        private Vector3 camPosition = new Vector3(9856, 105, -256);
        private Vector3 camTarget = new Vector3(0, 105, -256);
        private float angle = 0;

        private Matrix projectionMatrix;
        private Matrix viewMatrix;
        private Matrix worldMatrix;
        private BasicEffect basicEffect;

        private VertexPositionColor[] triangleVertices;
        private int[] triangleIndices;
        private VertexBuffer vertexBuffer;
        private Player player;

        public int VertexesAmount { get; set; }
        public int IndicesAmount { get; set; }

        public MonoRenderer(GraphicsDeviceManager graphicsDeviceManager, ThreeDRenderer threeDRenderer)
        {
            this.graphicsDeviceManager = graphicsDeviceManager;
            this.threeDRenderer = threeDRenderer;

            graphicsDevice = graphicsDeviceManager.GraphicsDevice;
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(65f), graphicsDevice.Viewport.AspectRatio, 1f, 100000f);
            basicEffect = new BasicEffect(graphicsDevice) { Alpha = 1f, VertexColorEnabled = true, LightingEnabled = false };

            triangleVertices = new VertexPositionColor[10000];
            triangleIndices = new int[30000];
        }

        public void SetProjection(Player player)
        {
            this.player = player;
            VertexesAmount = 0;
            IndicesAmount = 0;

            var viewX = player.Mobj.X.ToFloat();
            var viewY = player.Mobj.Y.ToFloat();
            var viewZ = player.ViewZ.ToFloat();
            var viewAngle = player.Mobj.Angle.ToRadian();

            camPosition = new Vector3(-viewX, viewZ, viewY);
            camTarget = camPosition + new Vector3(-MathF.Cos((float) viewAngle), 0, MathF.Sin((float) viewAngle));
            viewMatrix = Matrix.CreateLookAt(camPosition, camTarget, Vector3.Up);
            worldMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);
        }

        public void Render()
        {
            basicEffect.Projection = projectionMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.World = worldMatrix;

            // vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), VertexesAmount, BufferUsage.WriteOnly);
            // vertexBuffer.SetData(triangleVertices, 0, VertexesAmount);
            // graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.WireFrame };

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                // graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 3);
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    triangleVertices,
                    0,
                    triangleVertices.Length,
                    triangleIndices,
                    0,
                    triangleIndices.Length / 3,
                    VertexPositionColor.VertexDeclaration
                );
            }
        }

        public void DrawMaskedRange(ThreeDRenderer.VisWallRange drawSeg)
        {
            var v1x = -drawSeg.Seg.Vertex1.X.ToFloat();
            var v1y = drawSeg.Seg.Vertex1.Y.ToFloat();
            var v2x = -drawSeg.Seg.Vertex2.X.ToFloat();
            var v2y = drawSeg.Seg.Vertex2.Y.ToFloat();

            var floor = MathF.Max(drawSeg.Seg.BackSector.FloorHeight.ToFloat(), drawSeg.Seg.FrontSector.FloorHeight.ToFloat());
            var ceiling = MathF.Min(drawSeg.Seg.BackSector.CeilingHeight.ToFloat(), drawSeg.Seg.FrontSector.CeilingHeight.ToFloat());

            var topLeft = new VertexPositionColor(new(v1x, ceiling, v1y), Color.White);
            var topRight = new VertexPositionColor(new(v2x, ceiling, v2y), Color.White);
            var bottomLeft = new VertexPositionColor(new(v1x, floor, v1y), Color.White);
            var bottomRight = new VertexPositionColor(new(v2x, floor, v2y), Color.White);

            triangleVertices[VertexesAmount + 0] = topLeft;
            triangleVertices[VertexesAmount + 1] = topRight;
            triangleVertices[VertexesAmount + 2] = bottomLeft;
            triangleVertices[VertexesAmount + 3] = bottomRight;

            triangleIndices[IndicesAmount + 0] = VertexesAmount + 0;
            triangleIndices[IndicesAmount + 1] = VertexesAmount + 1;
            triangleIndices[IndicesAmount + 2] = VertexesAmount + 2;
            triangleIndices[IndicesAmount + 3] = VertexesAmount + 1;
            triangleIndices[IndicesAmount + 4] = VertexesAmount + 2;
            triangleIndices[IndicesAmount + 5] = VertexesAmount + 3;

            VertexesAmount += 4;
            IndicesAmount += 6;
        }

        public void ProjectSprite(Mobj thing)
        {
            // if (thing.Type == MobjType.Shotgun) 
            {
                var bottomZ = thing.Z.ToFloat();
                var topZ = thing.Z.ToFloat() + thing.Height.ToFloat();
                var thingX = -thing.X.ToFloat();
                var thingY = thing.Y.ToFloat();
                var radius = -thing.Radius.ToFloat();

                var playerX = -player.Mobj.X.ToFloat();
                var playerY = player.Mobj.Y.ToFloat();

                var playerAngle = -MathF.Atan((thingX - playerX) / (thingY - playerY));
                var box1 = new Vector2(thingX + MathF.Cos(playerAngle) * radius, thingY + MathF.Sin(playerAngle) * radius);
                var box2 = new Vector2(thingX - MathF.Cos(playerAngle) * radius, thingY - MathF.Sin(playerAngle) * radius);

                var topLeft = new VertexPositionColor(new(box1.X, bottomZ, box1.Y), Color.White);
                var topRight = new VertexPositionColor(new(box2.X, bottomZ, box2.Y), Color.White);
                var bottomLeft = new VertexPositionColor(new(box1.X, topZ, box1.Y), Color.White);
                var bottomRight = new VertexPositionColor(new(box2.X, topZ, box2.Y), Color.White);

                triangleVertices[VertexesAmount + 0] = topLeft;
                triangleVertices[VertexesAmount + 1] = topRight;
                triangleVertices[VertexesAmount + 2] = bottomLeft;
                triangleVertices[VertexesAmount + 3] = bottomRight;

                triangleIndices[IndicesAmount + 0] = VertexesAmount + 0;
                triangleIndices[IndicesAmount + 1] = VertexesAmount + 1;
                triangleIndices[IndicesAmount + 2] = VertexesAmount + 2;
                triangleIndices[IndicesAmount + 3] = VertexesAmount + 1;
                triangleIndices[IndicesAmount + 4] = VertexesAmount + 2;
                triangleIndices[IndicesAmount + 5] = VertexesAmount + 3;

                VertexesAmount += 4;
                IndicesAmount += 6;
            }
        }
    }
}