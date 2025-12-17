using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using MonoGameLibrary.Input;
using TribeBuild;

namespace TribeBuild.Scenes
{
    public class GameplayScene : Scene
    {
        // Game systems

        private SpriteFont spriteFont;
        private GameManager gameManager;
        private GameWorld gameWorld;
        
        // Rendering
        private Tilemap tilemap;
        private Camera2D camera;
        private CameraController cameraController;
        
        // Debug
        private bool showDebugInfo = true;
        private SpriteFont debugFont;

        public override void Initialize()
        {
            base.Initialize();
            Core.ExitOnEscape = false;
            
            //GameLogger.Instance?.Info("Scene", "GameplayScene initializing...");
        }

        public override void LoadContent()
        {
            base.LoadContent();

            debugFont = Core.Content.Load<SpriteFont>("Font/Baskic8"); 
            
            // Load tilemap first
            tilemap = Tilemap.FromFile(
                Core.Content, 
                "Images/samplemap5.tmx", 
                "Images/punyworld-overworld-tiles.xml"
            );
            tilemap.FitToWindow(Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
            
            // Initialize camera
            camera = new Camera2D(
                Core.GraphicsDevice.Viewport.Width, 
                Core.GraphicsDevice.Viewport.Height
            );
            
            // Set camera bounds to map size
            camera.SetBounds(new Rectangle(0,0,(int)tilemap.ScaleMapWidth,(int) tilemap.ScaleMapHeight));
            
            // Center camera on map
            camera.Position = new Vector2(
               camera.ViewportWidth * 0.5f,
               camera.ViewportHeight * 0.5f
            );
            camera.Zoom = 1f;
            
            // Initialize camera controller with Core's input
            cameraController = new CameraController(camera, Core.Input.Keyboard, Core.Input.Mouse);
            
            // Configure camera for RTS-style controls
            cameraController.ConfigureForRTS();
            cameraController.MoveSpeed = 400f;
            cameraController.MouseWheelZoomSensitivity = 0.15f;
            
            // Set zoom limits based on map fit
            float fitZoom = Math.Min(
                (float)Core.GraphicsDevice.Viewport.Width / tilemap.MapWidthInPixels,
                (float)Core.GraphicsDevice.Viewport.Height / tilemap.MapHeightInPixels
            );
            camera.SetZoomLimitsFromFit(fitZoom, 0.5f, 3.0f);
            
            // Initialize GameManager with tilemap
            gameManager = new GameManager(tilemap);
            gameManager.Initialize(tilemap.MapWidthInPixels, tilemap.MapHeightInPixels);
            gameWorld = gameManager.World;
            
            // Set world reference for all NPCs
            foreach (var npc in gameWorld.GetEntitiesOfType<Entity.NPC.NPCBody>())
            {
                npc.SetWorld(gameWorld);
            }
            
            // Load debug font if available
            try
            {
                debugFont = Core.Content.Load<SpriteFont>("Fonts/Debug");
            }
            catch
            {
                //GameLogger.Instance?.Warning("Scene", "Debug font not found");
            }
            
            // Start the game
            gameManager.StartGame();
            
            //GameLogger.Instance?.Info("Scene", "GameplayScene loaded successfully");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Handle custom input (pause, debug, time scale)
            HandleCustomInput();
            
            // Update tilemap animations
            tilemap.UpdateAnimations(gameTime);
            
            // Update camera controller (handles WASD, mouse drag, zoom)
            cameraController.Update(gameTime);
            
            // Update game systems
            // if (gameManager.CurrentState == GameState.Playing)
            // {
                gameManager.Update(gameTime);
            // }
        }

        private void HandleCustomInput()
        {
            // Toggle debug info with F3
            if (Core.Input.Keyboard.WasKeyPressed(Keys.F3))
            {
                showDebugInfo = !showDebugInfo;
            }
            
            // Pause/Resume with P or Escape
            if (Core.Input.Keyboard.WasKeyPressed(Keys.P) || Core.Input.Keyboard.WasKeyPressed(Keys.Escape))
            {
                if (gameManager.CurrentState == GameState.Playing)
                {
                    gameManager.PauseGame();
                }
                else if (gameManager.CurrentState == GameState.Paused)
                {
                    gameManager.ResumeGame();
                }
            }
            
            // Time scale controls with +/-
            if (Core.Input.Keyboard.WasKeyPressed(Keys.OemPlus) || Core.Input.Keyboard.WasKeyPressed(Keys.Add))
            {
                gameManager.TimeScale = Math.Min(gameManager.TimeScale + 0.5f, 5f);
            }
            if (Core.Input.Keyboard.WasKeyPressed(Keys.OemMinus) || Core.Input.Keyboard.WasKeyPressed(Keys.Subtract))
            {
                gameManager.TimeScale = Math.Max(gameManager.TimeScale - 0.5f, 0.5f);
            }
            
            // Reset time scale with 0
            if (Core.Input.Keyboard.WasKeyPressed(Keys.D0) || Core.Input.Keyboard.WasKeyPressed(Keys.NumPad0))
            {
                gameManager.TimeScale = 1f;
            }
            
            // Reset camera with R
            if (Core.Input.Keyboard.WasKeyPressed(Keys.R))
            {
                cameraController.ResetCamera();
            }
            
            // Log statistics with L
            if (Core.Input.Keyboard.WasKeyPressed(Keys.L))
            {
                gameWorld.LogStatistics();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // Clear background
            Core.GraphicsDevice.Clear(Color.CornflowerBlue);

            // ==================== WORLD RENDERING ====================
            
            // Begin sprite batch with camera transform
            Core.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: camera.Transform
            );
            
            // Draw tilemap
            tilemap.Draw(Core.SpriteBatch, Vector2.Zero, camera);
            
            // Draw game world (entities)
            var viewBounds = camera.GetViewBounds();
            gameWorld.Draw(Core.SpriteBatch, gameTime, viewBounds);
            
            Core.SpriteBatch.End();

            // ==================== UI RENDERING ====================
            
            // Begin sprite batch without camera transform for UI
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            // Draw UI
            DrawUI(gameTime);
            
            // Draw debug info
            if (showDebugInfo && debugFont != null)
            {
                DrawDebugInfo(gameTime);
            }
            
            Core.SpriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawUI(GameTime gameTime)
        {
            if (debugFont == null) return;
            
            int yPos = 10;
            int lineHeight = 20;
            
            // Game time
            string timeStr = $"Day {gameManager.CurrentDay} - {gameManager.GetTimeString()} ({(gameManager.IsNight() ? "Night" : "Day")})";
            Core.SpriteBatch.DrawString(debugFont, timeStr, new Vector2(10, yPos), Color.White);
            yPos += lineHeight;
            
            // Time scale
            if (gameManager.TimeScale != 1f)
            {
                string scaleStr = $"Time Scale: x{gameManager.TimeScale:F1}";
                Core.SpriteBatch.DrawString(debugFont, scaleStr, new Vector2(10, yPos), Color.Yellow);
                yPos += lineHeight;
            }
            
            // Game state
            if (gameManager.CurrentState == GameState.Paused)
            {
                string pausedStr = "PAUSED (Press P or ESC to resume)";
                var textSize = debugFont.MeasureString(pausedStr);
                Vector2 position = new Vector2(
                    (Core.GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    (Core.GraphicsDevice.Viewport.Height - textSize.Y) / 2
                );
                
                // Draw shadow
                Core.SpriteBatch.DrawString(debugFont, pausedStr, position + new Vector2(2, 2), Color.Black);
                // Draw text
                Core.SpriteBatch.DrawString(debugFont, pausedStr, position, Color.White);
            }
            
            // Resources collected
            yPos = Core.GraphicsDevice.Viewport.Height - 80;
            Core.SpriteBatch.DrawString(debugFont, $"Wood: {gameManager.WoodCollected}", new Vector2(10, yPos), Color.SaddleBrown);
            yPos += lineHeight;
            Core.SpriteBatch.DrawString(debugFont, $"Stone: {gameManager.StoneCollected}", new Vector2(10, yPos), Color.Gray);
            yPos += lineHeight;
            Core.SpriteBatch.DrawString(debugFont, $"Food: {gameManager.FoodCollected}", new Vector2(10, yPos), Color.YellowGreen);
            
            // Demo complete message
            if (gameManager.DemoComplete)
            {
                string completeStr = "DEMO OBJECTIVES COMPLETE!";
                var textSize = debugFont.MeasureString(completeStr);
                Vector2 position = new Vector2(
                    (Core.GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    50
                );
                
                // Draw with pulsing effect
                float pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3) * 0.3f + 0.7f;
                Color color = Color.Gold * pulse;
                
                Core.SpriteBatch.DrawString(debugFont, completeStr, position + new Vector2(2, 2), Color.Black);
                Core.SpriteBatch.DrawString(debugFont, completeStr, position, color);
            }
        }

        private void DrawDebugInfo(GameTime gameTime)
        {
            int yPos = Core.GraphicsDevice.Viewport.Height - 240;
            int lineHeight = 18;
            int xPos = Core.GraphicsDevice.Viewport.Width - 200;
            
            // FPS
            float fps = 1f / (float)gameTime.ElapsedGameTime.TotalSeconds;
            Core.SpriteBatch.DrawString(debugFont, $"FPS: {fps:F0}", new Vector2(xPos, yPos), Color.Lime);
            yPos += lineHeight;
            
            // Camera info
            Core.SpriteBatch.DrawString(debugFont, $"Camera: ({camera.Position.X:F0}, {camera.Position.Y:F0})", 
                new Vector2(xPos, yPos), Color.White);
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"Zoom: {camera.Zoom:F2}x", 
                new Vector2(xPos, yPos), Color.White);
            yPos += lineHeight;
            
            // Mouse world position
            Vector2 mouseWorldPos = cameraController.GetMouseWorldPosition();
            Core.SpriteBatch.DrawString(debugFont, $"Mouse: ({mouseWorldPos.X:F0}, {mouseWorldPos.Y:F0})", 
                new Vector2(xPos, yPos), Color.Cyan);
            yPos += lineHeight;
            
            // Dragging state
            if (cameraController.IsDragging)
            {
                Core.SpriteBatch.DrawString(debugFont, "Dragging Camera", 
                    new Vector2(xPos, yPos), Color.Yellow);
                yPos += lineHeight;
            }
            
            // World statistics
            var stats = gameWorld.GetStatistics();
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"Entities: {stats.TotalEntities}", 
                new Vector2(xPos, yPos), Color.White);
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"NPCs: {stats.NPCCount}", 
                new Vector2(xPos, yPos), Color.Cyan);
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"Animals: {stats.PassiveAnimalCount + stats.AggressiveAnimalCount}", 
                new Vector2(xPos, yPos), Color.Orange);
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"Resources: {stats.ResourceCount}", 
                new Vector2(xPos, yPos), Color.Green);
            yPos += lineHeight;
            
            // Task statistics
            yPos += lineHeight;
            Core.SpriteBatch.DrawString(debugFont, $"Tasks Pending: {stats.PendingTasks}", 
                new Vector2(xPos, yPos), Color.Yellow);
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"Tasks Active: {stats.ActiveTasks}", 
                new Vector2(xPos, yPos), Color.LightGreen);
            yPos += lineHeight;
            
            Core.SpriteBatch.DrawString(debugFont, $"Tasks Done: {stats.CompletedTasks}", 
                new Vector2(xPos, yPos), Color.Gray);
            
            // Controls hint
            DrawControlsHint();
        }

        private void DrawControlsHint()
        {
            if (debugFont == null) return;
            
            string[] controls = new string[]
            {
                "=== CAMERA CONTROLS ===",
                "WASD / Arrows: Move Camera",
                "Right Mouse: Drag Camera",
                "Mouse Wheel: Zoom",
                "Q/E: Keyboard Zoom",
                "R: Reset Camera",
                "",
                "=== GAME CONTROLS ===",
                "P / ESC: Pause",
                "+/-: Time Scale",
                "0: Reset Time Scale",
                "F3: Toggle Debug",
                "L: Log Statistics"
            };
            
            int yPos = 100;
            int lineHeight = 16;
            int xPos = 10;
            
            // Draw semi-transparent background
            var bgRect = new Rectangle(xPos - 5, yPos - 5, 220, controls.Length * lineHeight + 10);
            DrawRectangle(Core.SpriteBatch, bgRect, Color.Black * 0.5f);
            
            // Draw controls text
            foreach (var line in controls)
            {
                Color color = line.StartsWith("===") ? Color.Yellow : Color.White;
                Core.SpriteBatch.DrawString(debugFont, line, new Vector2(xPos, yPos), color);
                yPos += lineHeight;
            }
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            // Create a 1x1 white texture if needed
            Texture2D whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            
            spriteBatch.Draw(whitePixel, rect, color);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            
            // Cleanup
            gameManager?.Shutdown();
            
            //GameLogger.Instance?.Info("Scene", "GameplayScene unloaded");
        }
    }
}