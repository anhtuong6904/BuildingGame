using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary;
using Myra;
using TribeBuild.Scenes;
using TribeBuild.World;

namespace TribeBuild
{
    /// <summary>
    /// ✅ GIẢI PHÁP TẠM THỜI: Game chạy không cần font
    /// Sau này thêm font khi đã chạy được
    /// </summary>
    public class Game1 : Core
    {
        // Fonts - CÓ THỂ NULL, game vẫn chạy
        public SpriteFont DebugFont { get; private set; }
        public SpriteFont TitleFont { get; private set; }
        public SpriteFont NormalFont { get; private set; }
        public SpriteFont SmallFont { get; private set; }
        
        // Settings
        public GameSettings Settings { get; private set; }
        private Song themeSong;
        // Input tracking
        private KeyboardState previousKeyState;
        
        // State
        private bool isInitialized = false;
        private bool fontsLoaded = false;

        public Game1() : base("TribeBuild - Survival Strategy Game", 1920, 1080, false)
        {
            Settings = new GameSettings();
            Console.WriteLine("[Game1] ✅ Game instance created");
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            Console.WriteLine("[Game1] Initializing game systems...");
            
            // Initialize Myra UI environment
            MyraEnvironment.Game = this;
            Audio.PlaySong(themeSong);
            
            // Set graphics preferences
            Graphics.PreferMultiSampling = false;
            Graphics.SynchronizeWithVerticalRetrace = Settings.VSync;
            Graphics.ApplyChanges();
            Console.WriteLine("[Game1] ✅ Base systems initialized");
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            
            Console.WriteLine("[Game1] Loading content...");
            
            try
            {
                // ✅ TRY to load fonts, but DON'T CRASH if fails
                TryLoadFonts();
                LoadSound();
                
                // ✅ Start game anyway (even without fonts)
                StartGame();
                
                
                isInitialized = true;
                Console.WriteLine("[Game1] ✅ All content loaded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Game1] ❌ ERROR during LoadContent: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        private void LoadSound()
        {
             {
            Song loadSong = null;

                try
                {
                    loadSong = Content.Load<Song>("Sound/After a long journey there will always be someone returning home By Soundmashine");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Game1] ❌ Failed: {ex.Message.Split('\n')[0]}");
                }
                themeSong = loadSong;
        }
        }

        /// <summary>
        /// ✅ Try to load fonts, but don't crash if failed
        /// </summary>
        private void TryLoadFonts()
        {
            Console.WriteLine("\n[Game1] ========================================");
            Console.WriteLine("[Game1] ATTEMPTING TO LOAD FONTS...");
            Console.WriteLine("[Game1] ========================================\n");

            string[] fontPaths = new string[]
            {
                "Fonts/Debug",
                "Font/Debug", 
                "Fonts/Arial",
                "Font/Arial",
                "Font/Baskic8",
                "Fonts/Baskic8",
            };

            SpriteFont loadedFont = null;

            foreach (var path in fontPaths)
            {
                try
                {
                    Console.WriteLine($"[Game1] Trying: '{path}'...");
                    loadedFont = Content.Load<SpriteFont>(path);
                    Console.WriteLine($"[Game1] ✅ SUCCESS! Font loaded from: {path}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Game1] ❌ Failed: {ex.Message.Split('\n')[0]}");
                }
            }

            if (loadedFont != null)
            {
                // Success!
                DebugFont = loadedFont;
                TitleFont = loadedFont;
                NormalFont = loadedFont;
                SmallFont = loadedFont;
                fontsLoaded = true;
                
                Console.WriteLine($"\n[Game1] ✅ All fonts configured!");
            }
            else
            {
                // Failed, but continue anyway
                Console.WriteLine("\n[Game1] ⚠️ WARNING: No fonts loaded!");
                Console.WriteLine("[Game1] Game will run WITHOUT text rendering");
                Console.WriteLine("[Game1] To fix: Add Content/Fonts/Debug.spritefont\n");
                fontsLoaded = false;
            }
            
            Console.WriteLine("[Game1] ========================================\n");
        }

        private void StartGame()
        {
            
            // Create and transition to gameplay scene
            var gameplayScene = new GameplayScene();
            
            // ✅ Pass fonts ONLY if they loaded
            if (fontsLoaded && TitleFont != null)
            {
                gameplayScene.SetFonts(TitleFont, NormalFont, SmallFont);
                Console.WriteLine("[Game1] ✅ Fonts passed to gameplay scene");
            }
            else
            {
                Console.WriteLine("[Game1] ⚠️ Starting WITHOUT fonts (UI text will be invisible)");
            }
            
            ChangeScene(gameplayScene);
            
            Console.WriteLine("[Game1] ✅ Game started - transitioning to GameplayScene");
        }

        protected override void Update(GameTime gameTime)
        {
            if (!isInitialized)
            {
                base.Update(gameTime);
                return;
            }
            
            var keyState = Keyboard.GetState();
            HandleGlobalInput(keyState, gameTime);
            
            base.Update(gameTime);
            previousKeyState = keyState;
        }

        private void HandleGlobalInput(KeyboardState keyState, GameTime gameTime)
        {
            // F9 - Toggle VSync
            if (keyState.IsKeyDown(Keys.F9) && !previousKeyState.IsKeyDown(Keys.F9))
            {
                Settings.VSync = !Settings.VSync;
                Graphics.SynchronizeWithVerticalRetrace = Settings.VSync;
                Graphics.ApplyChanges();
                Console.WriteLine($"[Game1] VSync: {(Settings.VSync ? "ON" : "OFF")}");
            }

            // Alt+Enter - Toggle Fullscreen
            if (keyState.IsKeyDown(Keys.Enter) && 
                (keyState.IsKeyDown(Keys.LeftAlt) || keyState.IsKeyDown(Keys.RightAlt)))
            {
                if (!previousKeyState.IsKeyDown(Keys.Enter))
                {
                    ToggleFullscreen();
                }
            }
        }

        private void ToggleFullscreen()
        {
            Settings.Fullscreen = !Settings.Fullscreen;
            Graphics.IsFullScreen = Settings.Fullscreen;
            Graphics.ApplyChanges();
            
            Console.WriteLine($"[Game1] Fullscreen: {(Settings.Fullscreen ? "ON" : "OFF")}");
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            Console.WriteLine("[Game1] Unloading content...");
            Console.WriteLine("[Game1] ✅ Content unloaded");
        }

        public void ApplySettings()
        {
            Graphics.IsFullScreen = Settings.Fullscreen;
            Graphics.SynchronizeWithVerticalRetrace = Settings.VSync;
            Graphics.PreferredBackBufferWidth = Settings.ScreenWidth;
            Graphics.PreferredBackBufferHeight = Settings.ScreenHeight;
            Graphics.ApplyChanges();
            
            Console.WriteLine("[Game1] ✅ Settings applied");
        }
    }

    public class GameSettings
    {
        public int ScreenWidth { get; set; } = 1920;
        public int ScreenHeight { get; set; } = 1080;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = true;
        public float MasterVolume { get; set; } = 0.75f;
        public float MusicVolume { get; set; } = 0.5f;
        public float SFXVolume { get; set; } = 0.7f;
        public bool AntiAliasing { get; set; } = false;
        public bool ShowFPS { get; set; } = true;
        public float GameSpeed { get; set; } = 1.0f;
        public bool AutoPause { get; set; } = true;
        public bool EnableProfiling { get; set; } = false;
        public bool ShowDebugInfo { get; set; } = false;
        
    }
}