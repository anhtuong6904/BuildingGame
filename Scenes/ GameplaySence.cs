using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using TribeBuild.World;
using TribeBuild.UI;
using TribeBuild.Player;

namespace TribeBuild.Scenes
{
    /// <summary>
    /// ✅ FIXED: GameplayScene with proper Day Summary rendering
    /// </summary>
    public class GameplayScene : Scene
    {
        // Game systems
        private MyraUIManager uiManager;
        private GameManager gameManager;
        public RPGCamera rpgCamera { get; set; }
        
        // Rendering
        private Tilemap tilemap;
        private Camera2D camera;
        private CameraController cameraController;
        
        // Debug
        private bool showDebugInfo = false;
        private SpriteFont debugFont;
        private float debugToggleCooldown = 0f;
        private const float DEBUG_TOGGLE_DELAY = 0.2f;

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
            Console.WriteLine("[GameplayScene] Initializing...");
        }

        public override void LoadContent()
        {
            base.LoadContent();
            Console.WriteLine("[GameplayScene] Loading content...");
            
            InitializeUI();
            LoadTilemap();
            SetupCamera();
            InitializeGameManager();
            LoadDebugResources();
            
            // ✅ IMPORTANT: Initialize Day Summary UI with fonts
            InitializeDaySummaryUI();
            
            Console.WriteLine("[GameplayScene] ✅ Content loaded successfully!");
        }

        private void InitializeUI()
        {
            uiManager = new MyraUIManager(Core.Instance);
            uiManager.ShowMainMenu();
            Console.WriteLine("[GameplayScene] UI initialized");
        }

        private void LoadTilemap()
        {
            tilemap = Tilemap.FromFile(
                Core.Content, 
                "Images/samplemap5.xml", 
                "Images/punyworld-overworld-tileset-sheet.xml",
                "",
                249
            );
            
            tilemap.FitToWindow(
                Core.GraphicsDevice.Viewport.Width, 
                Core.GraphicsDevice.Viewport.Height
            );
            
            tilemap.BuildCollisionMatrix("", 249);
            
            tilemap.SetDebugMode(
                grid: false,
                collision: false,
                entities: false
            );
            
            Console.WriteLine($"[GameplayScene] Tilemap loaded: {tilemap.Width}x{tilemap.Height} tiles");
        }

        private void SetupCamera()
        {
            camera = new Camera2D(
                Core.GraphicsDevice.Viewport.Width, 
                Core.GraphicsDevice.Viewport.Height
            );
            
            camera.SetBounds(new Rectangle(
                0, 0, 
                (int)tilemap.ScaleMapWidth, 
                (int)tilemap.ScaleMapHeight
            ));
            
            camera.Position = new Vector2(
                tilemap.TileToWorld(10, 10).X,
                tilemap.TileToWorld(10, 10).Y
            );
            camera.Zoom = 1f;
            
            cameraController = new CameraController(
                camera, 
                Core.Input.Keyboard, 
                Core.Input.Mouse
            );
            
            cameraController.ConfigureForRTS();
            cameraController.MoveSpeed = 400f;
            cameraController.MouseWheelZoomSensitivity = 0.15f;
            
            float fitZoom = Math.Min(
                (float)Core.GraphicsDevice.Viewport.Width / tilemap.MapWidthInPixels,
                (float)Core.GraphicsDevice.Viewport.Height / tilemap.MapHeightInPixels
            );
            camera.SetZoomLimitsFromFit(fitZoom, 0.5f, 3.0f);
            
            Console.WriteLine($"[GameplayScene] Camera initialized");
        }

        private void InitializeGameManager()
        {
            gameManager = new GameManager(tilemap);
            gameManager.Initialize();
            gameManager.StartGame();
            
            Console.WriteLine("[GameplayScene] GameManager initialized");
        }

        /// <summary>
        /// ✅ Initialize Day Summary UI with proper fonts
        /// </summary>
        private void InitializeDaySummaryUI()
        {
            try
            {
                // Load fonts for day summary
                SpriteFont titleFont = Core.Content.Load<SpriteFont>("Fonts/Debug"); // Use Debug font as title
                SpriteFont normalFont = Core.Content.Load<SpriteFont>("Fonts/Debug");
                SpriteFont smallFont = Core.Content.Load<SpriteFont>("Fonts/Debug");
                
                gameManager.InitializeUI(titleFont, normalFont, smallFont, Core.GraphicsDevice);
                
                Console.WriteLine("[GameplayScene] ✅ Day Summary UI initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameplayScene] ⚠️ WARNING: Could not initialize Day Summary UI: {ex.Message}");
                Console.WriteLine("[GameplayScene] Day will auto-advance without summary screen");
            }
        }

        private void LoadDebugResources()
        {
            try
            {
                debugFont = Core.Content.Load<SpriteFont>("Fonts/Debug");
                Console.WriteLine("[GameplayScene] Debug font loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameplayScene] Warning: Debug font not found - {ex.Message}");
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            HandleDebugInput(deltaTime);
            tilemap.UpdateAnimations(gameTime);
            
            // Update RPG camera (follows player)
            if (gameManager.World.GetPlayerCharacter != null)
            {
                rpgCamera = new RPGCamera(camera, gameManager.World.GetPlayerCharacter);
                rpgCamera.Update(gameTime);
            }
            
            // ✅ Always update game manager (it handles all states)
            gameManager.Update(gameTime);
            
            // ✅ Only update Myra UI when in Playing or Paused state
            if (gameManager.CurrentState == GameState.Playing || 
                gameManager.CurrentState == GameState.Paused)
            {
                uiManager?.Update(gameTime);
            }
        }

        private void HandleDebugInput(float deltaTime)
        {
            var kb = Core.Input.Keyboard;
            
            if (debugToggleCooldown > 0f)
                debugToggleCooldown -= deltaTime;
            
            if (debugToggleCooldown > 0f)
                return;
            
            if (kb.WasKeyPressed(Keys.F3))
            {
                tilemap.ToggleDebugGrid();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            if (kb.WasKeyPressed(Keys.F4))
            {
                tilemap.ToggleCollisionGrid();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            if (kb.WasKeyPressed(Keys.F5))
            {
                tilemap.ToggleEntityPositions();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            if (kb.WasKeyPressed(Keys.F6))
            {
                showDebugInfo = !showDebugInfo;
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
                Console.WriteLine($"[Debug] Debug info: {(showDebugInfo ? "ON" : "OFF")}");
            }
            
            if (kb.WasKeyPressed(Keys.F7))
            {
                gameManager.World.ForceDebugLog();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
        }   

        public override void Draw(GameTime gameTime)
        {
            Core.GraphicsDevice.Clear(Color.CornflowerBlue);

            // ==================== WORLD RENDERING (with camera) ====================
            
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: camera.Transform
            );

            tilemap.Draw(Core.SpriteBatch, Vector2.Zero, camera);
            
            var viewBounds = camera.GetViewBounds();
            gameManager.World.Draw(Core.SpriteBatch, gameTime, viewBounds);
            
            tilemap.DrawDebug(Core.SpriteBatch, Vector2.Zero, camera);
            
            Core.SpriteBatch.End();

            // ==================== UI RENDERING (no camera transform) ====================
            
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            // ✅ Draw Myra UI only when NOT showing day summary
            if (gameManager.CurrentState != GameState.DaySummary)
            {
                uiManager?.Draw();
            }
            
            // ✅ Draw Day Summary overlay (drawn OVER everything)
            if (gameManager.CurrentState == GameState.DaySummary)
            {
                gameManager.DrawUI(Core.SpriteBatch, gameTime);
            }
            
            // Draw debug overlays
            if (showDebugInfo)
            {
                DrawDebugOverlay(gameTime);
            }
            
            Core.SpriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawDebugOverlay(GameTime gameTime)
        {
            if (debugFont == null)
                return;
            
            int y = 10;
            int lineHeight = 20;
            Color textColor = Color.White;
            Color bgColor = Color.Black * 0.7f;
            
            void DrawDebugText(string text)
            {
                var size = debugFont.MeasureString(text);
                var bgRect = new Rectangle(5, y - 2, (int)size.X + 10, (int)size.Y + 4);
                
                var pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { Color.White });
                Core.SpriteBatch.Draw(pixel, bgRect, bgColor);
                pixel.Dispose();
                
                Core.SpriteBatch.DrawString(debugFont, text, new Vector2(10, y), textColor);
                y += lineHeight;
            }
            
            if (showDebugInfo)
            {
                var stats = gameManager.World.GetStatistics();
                
                DrawDebugText($"=== GAME INFO ===");
                DrawDebugText($"Day {gameManager.CurrentDay} | {gameManager.GetTimeString()} ({gameManager.GetTimeOfDayName()})");
                DrawDebugText($"State: {gameManager.CurrentState}");
                y += 5;
                
                DrawDebugText($"=== ENTITIES ===");
                DrawDebugText($"Total: {stats.TotalEntities} | KD-Tree: {stats.SpatialTreeSize}");
                DrawDebugText($"Enemies: {stats.NightEnemyCount} | Resources: {stats.ResourceCount}");
                y += 5;
                
                DrawDebugText($"=== PLAYER ===");
                var player = gameManager.World.GetPlayerCharacter;
                if (player != null)
                {
                    DrawDebugText($"HP: {player.Health:F0}/{player.MaxHealth:F0}");
                    DrawDebugText($"Pos: ({player.Position.X:F0}, {player.Position.Y:F0})");
                }
                y += 5;
                
                DrawDebugText($"=== RESOURCES ===");
                DrawDebugText($"Wood: {gameManager.WoodCollected} | Food: {gameManager.FoodCollected}");
            }
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            gameManager?.Shutdown();
            uiManager = null;
            Console.WriteLine("[GameplayScene] Unloaded");
        }
    }
}