using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using FragSharpHelper;
using FragSharpFramework;

namespace Life
{
    public static class RndExtension
    {
        public static float RndBit(this Random rnd)
        {
            return rnd.NextDouble() > .5 ? 1 : 0;
        }
    }

    public class LifeGame : Game
    {
        const bool MouseEnabled = false;
        const bool UnlimitedSpeed = false;

        vec2 CameraPos = vec2.Zero;
        float CameraZoom = 30;
        float CameraAspect = 1;
        vec4 camvec { get { return new vec4(CameraPos.x, CameraPos.y, CameraZoom, CameraZoom); } }

        GraphicsDeviceManager graphics;

        RenderTarget2D
            Temp, Current;

        const int w = 1024, h = 1024;

        public LifeGame()
        {
            graphics = new GraphicsDeviceManager(this);

            Window.Title = "Game of Life, FragSharp Demo";
            graphics.PreferredBackBufferWidth  = w;
            graphics.PreferredBackBufferHeight = h;
            //graphics.IsFullScreen = rez.Mode == WindowMode.Fullscreen;
            graphics.SynchronizeWithVerticalRetrace = !UnlimitedSpeed;
            IsFixedTimeStep = !UnlimitedSpeed;

            Content.RootDirectory = "Content";
        }

        public vec2 Screen
        {
            get
            {
                return new vec2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            }
        }

        public float Restrict(float val, float a, float b)
        {
            if (val < a) return a;
            if (val > b) return b;
            return val;
        }

        void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        protected override void Initialize()
        {
            FragSharp.Initialize(Content, GraphicsDevice);

            GridHelper.Initialize(GraphicsDevice);

            Current    = MakeTarget(w, h);

            InitialConditions(w, h);

            Temp = MakeTarget(w, h);
            
            base.Initialize();
        }
        
        void InitialConditions(int w, int h)
        {
            Color[] clr = new Color[w * h];

            Current.GetData(clr);

            var rnd = new System.Random();
            for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
            {
                //if (true)
                //if (false)
                if (rnd.NextDouble() > 0.5f)
                //if (i == w / 2 && j == h / 2)
                //if (Math.Abs(i - w / 2) < 500)
                //if (j == h / 2)
                //if (i % 9 == 0)
                //if (j % 2 == 0 || i % 2 == 0)
                //if (j % 2 == 0 && i % 2 == 0)
                {
                    clr[i * h + j].R = (int)(255f * State.Alive);
                }
            }

            Current.SetData(clr);
        }

        private RenderTarget2D MakeTarget(int w, int h)
        {
            return new RenderTarget2D(graphics.GraphicsDevice, w, h);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (Buttons.Back.Down())
                this.Exit();

            Input.Update();

            const float MaxZoomOut = 1, MaxZoomIn = 200;

            // Switch to software emulation
            if (Keys.LeftShift.Pressed())
            {
                __SamplerHelper.SoftwareEmulation = !__SamplerHelper.SoftwareEmulation;
            }

            // Zoom all the way out
            if (Keys.Space.Pressed())
                CameraZoom = MaxZoomOut;

            // Zoom in/out, into the location of the cursor
            var world_mouse_pos = GetWorldCoordinate(Input.CurMousePos);
            var hold_camvec = camvec;
            
            float ZoomRate = 1.3333f;
            if      (Input.DeltaMouseScroll < 0) CameraZoom /= ZoomRate;
            else if (Input.DeltaMouseScroll > 0) CameraZoom *= ZoomRate;

            float KeyZoomRate = 1.125f;
            if      (Buttons.X.Down() || Keys.X.Down() || Keys.E.Down()) CameraZoom /= KeyZoomRate;
            else if (Buttons.A.Down() || Keys.Z.Down() || Keys.Q.Down()) CameraZoom *= KeyZoomRate;

            if (CameraZoom < MaxZoomOut) CameraZoom = MaxZoomOut;
            if (CameraZoom > MaxZoomIn)  CameraZoom = MaxZoomIn;

            if (MouseEnabled && !(Buttons.A.Pressed() || Buttons.X.Pressed()))
            {
                var shifted = GetShiftedCamera(Input.CurMousePos, camvec, world_mouse_pos);
                CameraPos = shifted;
            }

            // Move the camera via: Click And Drag
            //float MoveRate_ClickAndDrag = .00165f;
            //if (InputInfo.LeftMouseDown)
            //    CameraPos += InputInfo.DeltaMousPos / CameraZoom * MoveRate_ClickAndDrag * new vec2(-1, 1);

            // Move the camera via: Push Edge
            //float MoveRate_PushEdge = .07f;
            //var push_dir = vec2.Zero;
            //float EdgeRatio = .1f;
            //push_dir.x += -Restrict((EdgeRatio * Screen.x -     InputInfo.MousePos.x) / (EdgeRatio * Screen.x), 0, 1);
            //push_dir.x +=  Restrict((InputInfo.MousePos.x - (1-EdgeRatio) * Screen.x) / (EdgeRatio * Screen.x), 0, 1);
            //push_dir.y -= -Restrict((EdgeRatio * Screen.y - InputInfo.MousePos.y) / (EdgeRatio * Screen.y), 0, 1);
            //push_dir.y -=  Restrict((InputInfo.MousePos.y - (1 - EdgeRatio) * Screen.y) / (EdgeRatio * Screen.y), 0, 1);

            //CameraPos += push_dir / CameraZoom * MoveRate_PushEdge;

            // Move the camera via: Keyboard or Gamepad
            var dir = Input.Direction();

            float MoveRate_Keyboard = .07f;
            CameraPos += dir / CameraZoom * MoveRate_Keyboard;


            // Make sure the camera doesn't go too far offscreen
            var TR = GetWorldCoordinate(new vec2(Screen.x, 0));
            if (TR.x > 1)  CameraPos = new vec2(CameraPos.x - (TR.x - 1), CameraPos.y);
            if (TR.y > 1)  CameraPos = new vec2(CameraPos.x, CameraPos.y - (TR.y - 1));
            var BL = GetWorldCoordinate(new vec2(0, Screen.y));
            if (BL.x < -1) CameraPos = new vec2(CameraPos.x - (BL.x + 1), CameraPos.y);
            if (BL.y < -1) CameraPos = new vec2(CameraPos.x, CameraPos.y - (BL.y + 1));


            base.Update(gameTime);
        }

        const double DelayBetweenUpdates = .3333;
        double SecondsSinceLastUpdate = DelayBetweenUpdates;
        public static float PercentSimStepComplete = 0;

        int DrawCount = 0;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            DrawCount++;

            //if (CurKeyboard.IsKeyDown(Keys.Enter))
            SecondsSinceLastUpdate += gameTime.ElapsedGameTime.TotalSeconds;

            // Render setup
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            // Check if we need to do a simulation update
            if (UnlimitedSpeed || SecondsSinceLastUpdate > DelayBetweenUpdates)
            //if (SecondsSinceLastUpdate > DelayBetweenUpdates)
            {
                SecondsSinceLastUpdate -= DelayBetweenUpdates;

                SimulationUpdate();
            }

            // Draw texture to screen
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            PercentSimStepComplete = (float)(SecondsSinceLastUpdate / DelayBetweenUpdates);

            DrawLife.Using(camvec, CameraAspect, Current);
            GridHelper.DrawGrid();


            base.Draw(gameTime);
        }

        vec2 ScreenToGridCoordinates(vec2 pos)
        {
            var world = GetWorldCoordinate(pos);
            world.y = -world.y;

            return Screen * (world + vec2.Ones) / 2;
        }

        vec2 GetWorldCoordinate(vec2 pos)
        {
            var screen = new vec2(Screen.x, Screen.y);
            var ScreenCord = (2 * pos - screen) / screen;
            vec2 WorldCord;
            WorldCord.x = CameraAspect * ScreenCord.x / camvec.z + camvec.x;
            WorldCord.y = -ScreenCord.y / camvec.w + camvec.y;
            return WorldCord;
        }

        vec2 GetShiftedCamera(vec2 pos, vec4 prev_camvec, vec2 prev_worldcoord)
        {
            var screen = new vec2(Screen.x, Screen.y);
            var ScreenCord = (2 * pos - screen) / screen;

            vec2 shifted_cam;
            shifted_cam.x = prev_worldcoord.x - CameraAspect * ScreenCord.x / prev_camvec.z;
            shifted_cam.y = prev_worldcoord.y + ScreenCord.y / prev_camvec.w;

            return shifted_cam;
        }

        void SimulationUpdate()
        {
            if (__SamplerHelper.SoftwareEmulation)
            {
                UpdateLife._Apply(Current, Output: Temp);
            }
            else
            {
                UpdateLife.Apply(Current, Output: Temp);
            }
            Swap(ref Current, ref Temp);
        }
    }
}
