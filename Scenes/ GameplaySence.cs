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
    /// ✅ GameplayScene - Chạy được cả khi không có font
    /// </summary>
    public class GameplayScene : Scene
    {
        // Game systems
        private MyraUIManager uiManager;
        private GameManager gameManager;
        private RPGCamera rpgCamera;
        
        // Rendering
        private Tilemap tilemap;
        private Camera2D camera;
        private CameraController cameraController;
        
        // Fonts - CÓ THỂ NULL
        private SpriteFont titleFont;
        private SpriteFont normalFont;
        private SpriteFont smallFont;
        private SpriteFont debugFont;
        private bool hasFonts = false;
        
        // Debug
        private bool showDebugInfo = false;
        private float debugToggleCooldown = 0f;
        private const float DEBUG_TOGGLE_DELAY = 0.2f;
        
        // FPS Counter
        private int frameCount = 0;
        private double elapsedTime = 0;
        private int currentFPS = 0;

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
            Console.WriteLine("[GameplayScene] Initializing...");
        }

        /// <summary>
        /// Set fonts - CÓ THỂ NULL
        /// </summary>
        public void SetFonts(SpriteFont title, SpriteFont normal, SpriteFont small)
        {
            titleFont = title;
            normalFont = normal;
            smallFont = small;
            debugFont = normal;
            hasFonts = (title != null);
            
            if (hasFonts)
            {
                Console.WriteLine("[GameplayScene] ✅ Fonts configured");
            }
            else
            {
                Console.WriteLine("[GameplayScene] ⚠️ No fonts - UI text will be invisible");
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            Console.WriteLine("[GameplayScene] Loading content...");
            
            LoadTilemap();
            SetupCamera();
            InitializeGameManager();
            InitializeUI();
            
            Console.WriteLine("[GameplayScene] ✅ Content loaded successfully!");
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
            tilemap.SetDebugMode(grid: false, collision: false, entities: false);
            
            Console.WriteLine($"[GameplayScene] ✅ Tilemap loaded: {tilemap.Width}x{tilemap.Height} tiles");
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
                tilemap.TileToWorld(22, 18).X,
                tilemap.TileToWorld(22, 18).Y
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
            
            Console.WriteLine($"[GameplayScene] ✅ Camera initialized");
        }

        private void InitializeGameManager()
        {
            gameManager = new GameManager(tilemap);
            gameManager.Initialize();
            
            // ✅ Only init UI if we have fonts
            if (hasFonts && titleFont != null)
            {
                try
                {
                    gameManager.InitializeUI(titleFont, normalFont, smallFont, Core.GraphicsDevice);
                    Console.WriteLine("[GameplayScene] ✅ GameManager UI initialized");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameplayScene] ⚠️ Could not init UI: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[GameplayScene] ⚠️ Skipping GameManager UI (no fonts)");
            }
            
            Console.WriteLine("[GameplayScene] ✅ GameManager initialized");
        }

        private void InitializeUI()
        {
            try
            {
                // ✅ Only create UI if we have fonts
                if (hasFonts && titleFont != null)
                {
                    uiManager = new MyraUIManager(Core.Instance);
                    // Don't show main menu, go straight to game
                    uiManager.ShowGame();
                    Console.WriteLine("[GameplayScene] ✅ UI initialized");
                }
                else
                {
                    Console.WriteLine("[GameplayScene] ⚠️ Skipping UI (no fonts)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameplayScene] ⚠️ UI init failed: {ex.Message}");
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            UpdateFPS(gameTime);
            HandleDebugInput(deltaTime);
            tilemap.UpdateAnimations(gameTime);
            
            // Update RPG camera
            if (gameManager.World.GetPlayerCharacter != null)
            {
                if (rpgCamera == null)
                {
                    rpgCamera = new RPGCamera(camera, gameManager.World.GetPlayerCharacter);
                }
                rpgCamera.Update(gameTime);
            }
            
            // Update game
            UpdatePlayingState(gameTime);
        }

        private void UpdateFPS(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
            frameCount++;
            
            if (elapsedTime >= 1000)
            {
                currentFPS = frameCount;
                frameCount = 0;
                elapsedTime = 0;
            }
        }

        private void UpdatePlayingState(GameTime gameTime)
        {
            gameManager.Update(gameTime);
            
            // ✅ Only update UI if it exists
            if (uiManager != null)
            {
                uiManager.Update(gameTime);
            }
        }

        private void HandleDebugInput(float deltaTime)
        {
            var kb = Core.Input.Keyboard;
            
            if (debugToggleCooldown > 0f)
                debugToggleCooldown -= deltaTime;
            
            if (debugToggleCooldown > 0f)
                return;
            
            // F3 - Toggle grid
            if (kb.WasKeyPressed(Keys.F3))
            {
                tilemap.ToggleDebugGrid();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            // F4 - Toggle collision
            if (kb.WasKeyPressed(Keys.F4))
            {
                tilemap.ToggleCollisionGrid();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            // F5 - Toggle entity positions
            if (kb.WasKeyPressed(Keys.F5))
            {
                tilemap.ToggleEntityPositions();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            // F6 - Toggle debug overlay (only if we have fonts)
            if (kb.WasKeyPressed(Keys.F6) && hasFonts)
            {
                showDebugInfo = !showDebugInfo;
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
                Console.WriteLine($"[Debug] Debug overlay: {(showDebugInfo ? "ON" : "OFF")}");
            }
            
            // F7 - Print stats
            if (kb.WasKeyPressed(Keys.F7))
            {
                gameManager.World.ForceDebugLog();
                tilemap.PrintInfo();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
            
            // F8 - Print resources
            if (kb.WasKeyPressed(Keys.F8))
            {
                gameManager.World.LogStatistics();
                debugToggleCooldown = DEBUG_TOGGLE_DELAY;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Core.GraphicsDevice.Clear(new Color(20, 30, 40));

            // ==================== WORLD RENDERING ====================
            
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: camera.Transform
            );

            tilemap.Draw(Core.SpriteBatch, Vector2.Zero, camera);
            
            var viewBounds = camera.GetViewBounds();
            gameManager.Draw(Core.SpriteBatch, gameTime, viewBounds);
            
            tilemap.DrawDebug(Core.SpriteBatch, Vector2.Zero, camera);
            
            Core.SpriteBatch.End();

            // ==================== UI RENDERING ====================
            
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            // ✅ Only draw UI if it exists
            if (uiManager != null && gameManager.CurrentState != GameState.DaySummary)
            {
                uiManager.Draw();
            }
            
            if (gameManager.CurrentState == GameState.DaySummary)
            {
                gameManager.Draw(Core.SpriteBatch, gameTime, viewBounds);
            }
            
            // ✅ Only draw debug info if we have fonts
            if (showDebugInfo && hasFonts && debugFont != null)
            {
                DrawDebugOverlay(gameTime);
            }
            
            // ✅ Warning if no fonts
            if (!hasFonts)
            {
                // Draw a colored rectangle to show game is running
                var pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { Color.White });
                
                // Top-left corner indicator (green = running)
                Core.SpriteBatch.Draw(pixel, new Rectangle(10, 10, 20, 20), Color.LimeGreen);
                
                pixel.Dispose();
            }
            
            Core.SpriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawDebugOverlay(GameTime gameTime)
        {
            if (debugFont == null) return;
            
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
            
            DrawDebugText($"=== PERFORMANCE ===");
            DrawDebugText($"FPS: {currentFPS}");
            y += 5;
            
            DrawDebugText($"=== GAME STATE ===");
            DrawDebugText($"Day {gameManager.CurrentDay} | {gameManager.GetTimeString()}");
            DrawDebugText($"State: {gameManager.CurrentState}");
            y += 5;
            
            var stats = gameManager.World.GetStatistics();
            DrawDebugText($"=== WORLD ===");
            DrawDebugText($"Entities: {stats.TotalEntities}");
            DrawDebugText($"Resources: {stats.ResourceCount}");
            y += 5;
            
            DrawDebugText($"=== CONTROLS ===");
            DrawDebugText($"WASD: Move | E: Interact");
            DrawDebugText($"F3: Grid | F4: Collision | F5: Entities");
            DrawDebugText($"F7: Stats | F8: Resources");
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            gameManager?.Shutdown();
            uiManager = null;
            Console.WriteLine("[GameplayScene] ✅ Unloaded");
        }

        public GameWorld GetGameWorld()
        {
            return gameManager?.World;
        }

        public GameManager GetGameManager()
        {
            return gameManager;
        }
    }
}