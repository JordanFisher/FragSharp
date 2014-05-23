using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using FragSharpFramework;

namespace FragSharpHelper
{
    // Summary:
    //     Specifies the game controller associated with a player.
    public enum PlayerIndex
    {
        // Summary:
        //     The first controller.
        One = 0,
        //
        // Summary:
        //     The second controller.
        Two = 1,
        //
        // Summary:
        //     The third controller.
        Three = 2,
        //
        // Summary:
        //     The fourth controller.
        Four = 3,
        //
        // Summary:
        //     Any controller.
        Any = -1,
        //
        // Summary:
        //     The primary controller.
        Primary = -4,
    }

    public static class Input
    {
        private static PlayerIndex PrimaryPlayer = PlayerIndex.One;
        public static void SetPrimaryPlayer(PlayerIndex PrimaryPlayer)
        {
            Input.PrimaryPlayer = PrimaryPlayer;
        }

        public static KeyboardState CurKeyboard, PrevKeyboard;
        public static GamePadState[] CurGamepad = new GamePadState[4], PrevGamepad = new GamePadState[4];
        public static MouseState CurMouse, PrevMouse;
        
        public static vec2 CurMousePos, PrevMousePos;
        public static vec2 DeltaMousPos;
        public static float DeltaMouseScroll;

        public static void Update()
        {
            PrevMouse = CurMouse;
            PrevMousePos = CurMousePos;

            PrevKeyboard = CurKeyboard;
            CurKeyboard = Keyboard.GetState();

            for (int i = 0; i < 4; i++)
            {
                PrevGamepad[i] = CurGamepad[i];
                CurGamepad[i]  = GamePad.GetState(Xna.PlayerIndex.One);
            }

            CurMouse = Mouse.GetState();
            CurMousePos = new vec2(CurMouse.X, CurMouse.Y);

            DeltaMousPos = new vec2(CurMouse.X - PrevMouse.X, CurMouse.Y - PrevMouse.Y);
            DeltaMouseScroll = CurMouse.ScrollWheelValue - PrevMouse.ScrollWheelValue;
        }

        public static bool LeftMousePressed
        {
            get
            {
                return CurMouse.LeftButton  == ButtonState.Pressed &&
                       PrevMouse.LeftButton == ButtonState.Released;
            }
        }

        public static bool LeftMouseDown
        {
            get
            {
                return CurMouse.LeftButton  == ButtonState.Pressed;
            }
        }

        public static bool RightMousePressed
        {
            get
            {
                return CurMouse.RightButton  == ButtonState.Pressed &&
                       PrevMouse.RightButton == ButtonState.Released;
            }
        }

        public static bool RightMouseDown
        {
            get
            {
                return CurMouse.RightButton == ButtonState.Pressed;
            }
        }

        public static vec2 KeyboardDir(Keys Left = Keys.Left, Keys Right = Keys.Right, Keys Up = Keys.Up, Keys Down = Keys.Down)
        {
            vec2 dir = vec2.Zero;

            if (Up.Pressed())    dir.y =  1;
            if (Down.Pressed())  dir.y = -1;
            if (Right.Pressed()) dir.x =  1;
            if (Left.Pressed())  dir.x = -1;

            return dir;
        }

        public static vec2 KeyboardDirAsdw()
        {
            return KeyboardDir(Keys.A, Keys.D, Keys.W, Keys.S);
        }

        public static vec2 ProcessJoystickDir(vec2 dir)
        {
            if (dir.Length() > .1f)
            {
                return dir;
            }

            return vec2.Zero;
        }

        public static vec2 GamepadLeftJoyDir(PlayerIndex Player)
        {
            return ProcessJoystickDir((vec2)CurGamepad[(int)Player].ThumbSticks.Left);
        }

        public static vec2 GamepadRightJoyDir(PlayerIndex Player)
        {
            return ProcessJoystickDir((vec2)CurGamepad[(int)Player].ThumbSticks.Right);
        }

        public static vec2 GamepadDpadDir(PlayerIndex Player)
        {
            vec2 dir = vec2.Zero;
            var gamepad = CurGamepad[(int)Player];

            if (gamepad.DPad.Right.Down()) dir.x =  1;
            if (gamepad.DPad.Left.Down())  dir.x = -1;
            if (gamepad.DPad.Up.Down())    dir.y =  1;
            if (gamepad.DPad.Down.Down())  dir.y = -1;

            return dir;
        }

        public static vec2 CombineDir(vec2 PrimarySource, vec2 SecondarySource)
        {
            if (PrimarySource != vec2.Zero)
                return PrimarySource;
            else
                return SecondarySource;
        }

        public static vec2 CombineDir(params vec2[] Sources)
        {
            vec2 result = Sources[Sources.Length - 1];
            for (int i = Sources.Length - 2; i >= 0; i--)
            {
                result = CombineDir(Sources[i], result);
            }

            return result;
        }

        public static vec2 Direction(PlayerIndex Player = PlayerIndex.Primary)
        {
            if (Player == PlayerIndex.Primary) Player = PrimaryPlayer;
            if (Player == PlayerIndex.Any) return CombineDir(Direction(PlayerIndex.One), Direction(PlayerIndex.Two), Direction(PlayerIndex.Three), Direction(PlayerIndex.Four));
            
            return CombineDir(GamepadLeftJoyDir(Player), KeyboardDir(), KeyboardDirAsdw(), GamepadDpadDir(Player), GamepadRightJoyDir(Player));
        }

        public static bool Pressed(this Keys key)
        {
            return Input.CurKeyboard.IsKeyDown(key);
        }

        public static bool Down(this ButtonState state)
        {
            return state == ButtonState.Pressed;
        }

        public static bool Pressed(this Buttons button, PlayerIndex Player = PlayerIndex.Primary)
        {
            if (Player == PlayerIndex.Primary) Player = PrimaryPlayer;
            if (Player == PlayerIndex.Any) return button.Pressed(PlayerIndex.One) || button.Pressed(PlayerIndex.Two) || button.Pressed(PlayerIndex.Three) || button.Pressed(PlayerIndex.Four);
            
            int i = (int)Player;
            return Input.CurGamepad[i].IsButtonDown(button) && Input.PrevGamepad[i].IsButtonUp(button);
        }

        public static bool Released(this Buttons button, PlayerIndex Player = PlayerIndex.Primary)
        {
            if (Player == PlayerIndex.Primary) Player = PrimaryPlayer;
            if (Player == PlayerIndex.Any) return button.Released(PlayerIndex.One) || button.Released(PlayerIndex.Two) || button.Released(PlayerIndex.Three) || button.Released(PlayerIndex.Four);

            int i = (int)Player;
            return Input.CurGamepad[i].IsButtonUp(button) && Input.PrevGamepad[i].IsButtonDown(button);
        }

        public static bool Down(this Buttons button, PlayerIndex Player = PlayerIndex.Primary)
        {
            if (Player == PlayerIndex.Primary) Player = PrimaryPlayer;
            if (Player == PlayerIndex.Any) return button.Down(PlayerIndex.One) || button.Down(PlayerIndex.Two) || button.Down(PlayerIndex.Three) || button.Down(PlayerIndex.Four);

            int i = (int)Player;
            return Input.CurGamepad[i].IsButtonDown(button);
        }
    }
}
