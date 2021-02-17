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
using System.Runtime.ExceptionServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ManagedDoom.UserInput
{
    public sealed class SfmlUserInput : IUserInput, IDisposable
    {
        private readonly DoomApplication doom;
        private Config config;
        private GraphicsDevice graphics;

        private bool useMouse;

        private bool[] weaponKeys;
        private int turnHeld;

        private int windowCenterX;
        private int windowCenterY;
        private int mouseX;
        private int mouseY;
        private bool cursorCentered;

        public SfmlUserInput(Config config, DoomApplication doom, bool useMouse)
        {
            this.doom = doom;

            try
            {
                Console.Write("Initialize user input: ");

                this.config = config;

                config.mouse_sensitivity = Math.Max(config.mouse_sensitivity, 0);

                graphics = doom.GraphicsDevice;

                this.useMouse = useMouse;

                weaponKeys = new bool[7];
                turnHeld = 0;

                windowCenterX = graphics.Viewport.Width / 2;
                windowCenterY = graphics.Viewport.Height / 2;
                mouseX = 0;
                mouseY = 0;
                cursorCentered = false;

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            var keyForward = IsPressed(keyboardState, mouseState, config.key_forward);
            var keyBackward = IsPressed(keyboardState, mouseState, config.key_backward);
            var keyStrafeLeft = IsPressed(keyboardState, mouseState, config.key_strafeleft);
            var keyStrafeRight = IsPressed(keyboardState, mouseState, config.key_straferight);
            var keyTurnLeft = IsPressed(keyboardState, mouseState, config.key_turnleft);
            var keyTurnRight = IsPressed(keyboardState, mouseState, config.key_turnright);
            var keyFire = IsPressed(keyboardState, mouseState, config.key_fire);
            var keyUse = IsPressed(keyboardState, mouseState, config.key_use);
            var keyRun = IsPressed(keyboardState, mouseState, config.key_run);
            var keyStrafe = IsPressed(keyboardState, mouseState, config.key_strafe);

            weaponKeys[0] = keyboardState.IsKeyDown(Keys.D1);
            weaponKeys[1] = keyboardState.IsKeyDown(Keys.D2);
            weaponKeys[2] = keyboardState.IsKeyDown(Keys.D3);
            weaponKeys[3] = keyboardState.IsKeyDown(Keys.D4);
            weaponKeys[4] = keyboardState.IsKeyDown(Keys.D5);
            weaponKeys[5] = keyboardState.IsKeyDown(Keys.D6);
            weaponKeys[6] = keyboardState.IsKeyDown(Keys.D7);

            cmd.Clear();

            var strafe = keyStrafe;
            var speed = keyRun ? 1 : 0;
            var forward = 0;
            var side = 0;

            if (config.game_alwaysrun)
            {
                speed = 1 - speed;
            }

            if (keyTurnLeft || keyTurnRight)
            {
                turnHeld++;
            }
            else
            {
                turnHeld = 0;
            }

            int turnSpeed;
            if (turnHeld < PlayerBehavior.SlowTurnTics)
            {
                turnSpeed = 2;
            }
            else
            {
                turnSpeed = speed;
            }

            if (strafe)
            {
                if (keyTurnRight)
                {
                    side += PlayerBehavior.SideMove[speed];
                }

                if (keyTurnLeft)
                {
                    side -= PlayerBehavior.SideMove[speed];
                }
            }
            else
            {
                if (keyTurnRight)
                {
                    cmd.AngleTurn -= (short) PlayerBehavior.AngleTurn[turnSpeed];
                }

                if (keyTurnLeft)
                {
                    cmd.AngleTurn += (short) PlayerBehavior.AngleTurn[turnSpeed];
                }
            }

            if (keyForward)
            {
                forward += PlayerBehavior.ForwardMove[speed];
            }

            if (keyBackward)
            {
                forward -= PlayerBehavior.ForwardMove[speed];
            }

            if (keyStrafeLeft)
            {
                side -= PlayerBehavior.SideMove[speed];
            }

            if (keyStrafeRight)
            {
                side += PlayerBehavior.SideMove[speed];
            }

            if (keyFire)
            {
                cmd.Buttons |= TicCmdButtons.Attack;
            }

            if (keyUse)
            {
                cmd.Buttons |= TicCmdButtons.Use;
            }

            // Check weapon keys.
            for (var i = 0; i < weaponKeys.Length; i++)
            {
                if (weaponKeys[i])
                {
                    cmd.Buttons |= TicCmdButtons.Change;
                    cmd.Buttons |= (byte) (i << TicCmdButtons.WeaponShift);
                    break;
                }
            }

            UpdateMouse();
            var ms = 0.5F * config.mouse_sensitivity;
            var mx = (int) MathF.Round(ms * mouseX);
            var my = (int) MathF.Round(ms * mouseY);

            cmd.Pitch += my * 0x8;
            
            if (strafe)
            {
                side += mx * 2;
            }
            else
            {
                cmd.AngleTurn -= (short) (mx * 0x8);
            }

            if (forward > PlayerBehavior.MaxMove)
            {
                forward = PlayerBehavior.MaxMove;
            }
            else if (forward < -PlayerBehavior.MaxMove)
            {
                forward = -PlayerBehavior.MaxMove;
            }

            if (side > PlayerBehavior.MaxMove)
            {
                side = PlayerBehavior.MaxMove;
            }
            else if (side < -PlayerBehavior.MaxMove)
            {
                side = -PlayerBehavior.MaxMove;
            }

            cmd.ForwardMove += (sbyte) forward;
            cmd.SideMove += (sbyte) side;
        }

        private bool IsPressed(KeyboardState keyboardState, MouseState mouseState, KeyBinding keyBinding)
        {
            foreach (var key in keyBinding.Keys)
            {
                if (keyboardState.IsKeyDown((Keys) key))
                {
                    return true;
                }
            }

            if (doom.IsActive)
            {
                foreach (var mouseButton in keyBinding.MouseButtons)
                {
                    switch (mouseButton)
                    {
                        case DoomMouseButton.Unknown:
                            break;
                        case DoomMouseButton.Mouse1:
                            return mouseState.LeftButton == ButtonState.Pressed;
                        case DoomMouseButton.Mouse2:
                            return mouseState.RightButton == ButtonState.Pressed;
                        case DoomMouseButton.Mouse3:
                            return mouseState.MiddleButton == ButtonState.Pressed;
                        case DoomMouseButton.Mouse4:
                            return mouseState.XButton1 == ButtonState.Pressed;
                        case DoomMouseButton.Mouse5:
                            return mouseState.XButton2 == ButtonState.Pressed;
                        // case DoomMouseButton.Count: // TODO: Handle this
                        //     return mouseState.RightButton == ButtonState.Pressed;
                    }
                }
            }

            return false;
        }

        public void Reset()
        {
            mouseX = 0;
            mouseY = 0;
            cursorCentered = false;
        }

        private void UpdateMouse()
        {
            if (doom.IsActive)
            {
                if (cursorCentered)
                {
                    var current = Mouse.GetState();
                    mouseX = current.X - windowCenterX;

                    if (config.mouse_disableyaxis)
                    {
                        mouseY = 0;
                    }
                    else
                    {
                        mouseY = -(current.Y - windowCenterY);
                    }
                }
                else
                {
                    mouseX = 0;
                    mouseY = 0;
                }

                Mouse.SetPosition(windowCenterX, windowCenterY);
                var pos = Mouse.GetState();
                cursorCentered = (pos.X == windowCenterX && pos.Y == windowCenterY);
            }
            else
            {
                mouseX = 0;
                mouseY = 0;
                cursorCentered = false;
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Shutdown user input.");
        }

        public int MaxMouseSensitivity
        {
            get { return 15; }
        }

        public int MouseSensitivity
        {
            get { return config.mouse_sensitivity; }

            set { config.mouse_sensitivity = value; }
        }
    }
}