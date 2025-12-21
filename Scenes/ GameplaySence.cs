using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using TribeBuild.World;
using TribeBuild.UI;
using TribeBuild.Player;




namespace TribeBuild.Scenes
{
    public class GameplayScene : Scene
    {
        // Game systems
        private MyraUIManager uiManager;
        private GameManager gameManager;
        private GameWorld gameWorld;
        public RPGCamera rpgCamera {get; set;}
        private TextureAtlas PlayerAtlas;
        
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

            
            uiManager = new MyraUIManager(Core.Instance);
            uiManager.ShowMainMenu();
            


            PlayerAtlas = TextureAtlas.FromFile(Core.Content, "Images/farmer.xml");
            
            // Load tilemap first
            tilemap = Tilemap.FromFile(
                Core.Content, 
                "Images/samplemap5.tmx", 
                "Images/punyworld-overworld-tiles.xml",
                waterTileIdStart: 260
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
            gameManager.Initialize((int)tilemap.ScaleMapWidth, (int)tilemap.ScaleMapHeight);
            gameWorld = gameManager.World;

            gameWorld.SyncPathfinderWithTilemap();
            

            rpgCamera = new RPGCamera(camera, gameWorld.GetPlayerCharacter);
            
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
            // Update tilemap animations
            tilemap.UpdateAnimations(gameTime);

            rpgCamera.Update(gameTime);
            
            
            // Update camera controller (handles WASD, mouse drag, zoom)
            
            // Update game systems
            if (gameManager.CurrentState == GameState.Playing)
            {
                gameManager.Update(gameTime);

                uiManager?.Update(gameTime);

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
            
           uiManager?.Draw();
            
            Core.SpriteBatch.End();

            base.Draw(gameTime);
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