using System;
using System.Collections.Generic;
using System.Linq;
using ManagedDoom.SoftwareRendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ManagedDoom.HardwareRendering
{
    public class MonoHudRenderer
    {
        private readonly Mono3DRenderer renderer;
        private readonly CommonResource resource;
        private readonly GraphicsDevice graphicsDevice;

        private readonly Dictionary<int, Patch> digitToNumberPatch;
        private StatusBarRenderer.Patches patches;

        private const int Scale = 3;

        public MonoHudRenderer(Mono3DRenderer renderer, CommonResource resource, GraphicsDevice graphicsDevice)
        {
            this.renderer = renderer;
            this.resource = resource;
            this.graphicsDevice = graphicsDevice;

            patches = new StatusBarRenderer.Patches(resource);
        }

        public void RenderHud(Player player, bool drawBackground)
        {
            RenderHealth(player, out var healthOffset);
            RenderArmor(player, healthOffset);
            RenderAmmo(player);
            RenderKeys(player);
        }

        private void RenderKeys(Player player)
        {
            var viewportWidth = graphicsDevice.Viewport.Width;
            var viewportHeight = graphicsDevice.Viewport.Height;
            var pos = new Vector2(viewportWidth * 0.95f - 10 * Scale, viewportHeight * 0.05f);

            var cards = player.Cards;

            for (var index = 0; index < cards.Length; index++)
            {
                if (!cards[index])
                {
                    continue;
                }

                var displace = new Vector2(index / 3 * 10, index % 3 * 10 + (index < 3 ? 1 : 0)) * Scale;
                renderer.AddSprite(new()
                {
                    Position = pos + displace,
                    Scale = Scale,
                    Texture2D = patches.Keys[index].Texture2D
                });
            }
        }

        private void RenderAmmo(Player player)
        {
            var viewportWidth = graphicsDevice.Viewport.Width;
            var viewportHeight = graphicsDevice.Viewport.Height;
            var pos = new Vector2(viewportWidth * 0.95f, viewportHeight * 0.9f);

            AmmoType? ammoType = player.ReadyWeapon switch
            {
                WeaponType.Bfg => AmmoType.Cell,
                WeaponType.Plasma => AmmoType.Cell,
                WeaponType.Chaingun => AmmoType.Clip,
                WeaponType.Pistol => AmmoType.Clip,
                WeaponType.Shotgun => AmmoType.Shell,
                WeaponType.SuperShotgun => AmmoType.Shell,
                WeaponType.Missile => AmmoType.Missile,
                WeaponType.Chainsaw => null,
                WeaponType.Fist => null,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (ammoType == null)
            {
                return;
            }

            DrawNumberAlignRight(pos, player.Ammo[(int) ammoType], out var xOffset);

            var ammoTexture = patches.Ammo[(int) ammoType].Texture2D;

            var numberHeight = patches.TallNumbers[0].Height;
            var ammoHeight = ammoTexture.Height;
            var align = (numberHeight - ammoHeight) / 2;

            renderer.AddSprite(new()
            {
                Position = pos + new Vector2(xOffset - ammoTexture.Width, align) * Scale - new Vector2(viewportWidth * 0.01f, 0),
                // Position = pos,
                Scale = Scale,
                Texture2D = ammoTexture
            });
        }

        private void RenderArmor(Player player, float healthOffset)
        {
            var viewportWidth = graphicsDevice.Viewport.Width;
            var viewportHeight = graphicsDevice.Viewport.Height;
            var pos = new Vector2(healthOffset + viewportWidth * 0.05f, viewportHeight * 0.9f);

            const int armorScale = 2;
            var armorPatch = player.ArmorType switch
            {
                0 or 1 => patches.ArmorGreen,
                2 => patches.ArmorBlue,
                _ => throw new ArgumentOutOfRangeException()
            };

            renderer.AddSprite(new()
            {
                Position = pos + new Vector2(0, viewportHeight * 0.01f),
                Scale = armorScale,
                Texture2D = armorPatch.Texture2D
            });

            DrawNumber(pos + new Vector2(armorPatch.Width * armorScale + viewportWidth * 0.01f, 0), player.ArmorPoints, out _);
        }

        private void RenderHealth(Player player, out float xOffset)
        {
            var viewportWidth = graphicsDevice.Viewport.Width;
            var viewportHeight = graphicsDevice.Viewport.Height;
            var pos = new Vector2(viewportWidth * 0.05f, viewportHeight * 0.9f);

            const int medikitScale = 2;
            var medikitTexture = patches.Medikit.Texture2D;

            renderer.AddSprite(new()
            {
                Position = pos + new Vector2(0, viewportHeight * 0.008f),
                Scale = medikitScale,
                Texture2D = medikitTexture
            });

            xOffset = medikitTexture.Width * medikitScale + viewportWidth * 0.01f;
            DrawNumber(pos + new Vector2(xOffset, 0), player.Health, out var numberOffset);
            xOffset += numberOffset * Scale + pos.X;
        }

        private void DrawNumber(Vector2 pos, int number, out float xOffset)
        {
            xOffset = 0;
            var numberString = number.ToString();

            foreach (var symbol in numberString)
            {
                var patch = symbol switch
                {
                    '-' => patches.TallMinus,
                    >= '0' and <= '9' => patches.TallNumbers[symbol - '0'],
                    _ => throw new ArgumentOutOfRangeException()
                };

                renderer.AddSprite(new()
                {
                    Position = pos + new Vector2(xOffset, 0) * Scale,
                    Scale = Scale,
                    Texture2D = patch.Texture2D
                });

                xOffset += patch.Texture2D.Width;
            }
        }

        private void DrawNumberAlignRight(Vector2 pos, int number, out float xOffset)
        {
            xOffset = 0;
            var numberString = number.ToString();

            for (var index = numberString.Length - 1; index >= 0; index--)
            {
                var symbol = numberString[index];
                var patch = symbol switch
                {
                    '-' => patches.TallMinus,
                    >= '0' and <= '9' => patches.TallNumbers[symbol - '0'],
                    _ => throw new ArgumentOutOfRangeException()
                };

                renderer.AddSprite(new()
                {
                    Position = pos + new Vector2(xOffset - patch.Texture2D.Width, 0) * Scale,
                    Scale = Scale,
                    Texture2D = patch.Texture2D
                });

                xOffset -= patch.Texture2D.Width;
            }
        }
    }
}