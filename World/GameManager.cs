using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using TribeBuild.Entity;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.Enemies;
using TribeBuild.Player;
using TribeBuild.UI;

namespace TribeBuild.World
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        DaySummary,  // ✅ NEW: Day summary screen
        GameOver
    }
    
    /// <summary>
    /// ✅ UPDATED: GameManager với chu kỳ ngày/đêm hoàn chỉnh
    /// </summary>
    public class GameManager
    {
        // Singleton
        public static GameManager Instance { get; private set; }
        
        // Game state
        public GameState CurrentState { get; private set; }
        
        // Systems
        public GameWorld World { get; private set; }
        public ResourceManager Resources { get; private set; }
        public DayNightCycleManager DayNightCycle { get; private set; } // ✅ NEW
        
        // UI
        private DaySummaryScreen daySummaryScreen; // ✅ NEW
        
        // Tilemap
        private Tilemap tilemap;
        
        // ✅ UPDATED: Time management now handled by DayNightCycleManager
        public int CurrentDay { get; set; }
        public float TimeOfDay { get; set; }
        public float TimeScale { get; set; }
        
        // Demo objectives (can be removed if not needed)
        public int WoodCollected { get; private set; }
        public int StoneCollected { get; private set; }
        public int FoodCollected { get; private set; }
        
        // Texture atlases
        private TextureAtlas atlasPlayer;
        private TextureAtlas atlasResource;
        private TextureAtlas atlasChicken;
        private TextureAtlas atlasSheep;
        private TextureAtlas atlasBoar;
        private TextureAtlas atlasAssassin;
        
        private Vector2 Scale;
        private SpawnZoneManager spawnManager;
        
        // ✅ Game over flag
        private bool isGameOver = false;
        
        public GameManager(Tilemap tilemap)
        {
            if (Instance != null)
            {
                throw new Exception("GameManager already exists!");
            }
            
            Instance = this;
            this.tilemap = tilemap;
            Scale = tilemap.Scale;
            
            Console.WriteLine("[GameManager] Initializing...");
            
            // Load texture atlases
            LoadAtlases();
            
            // Initialize state
            CurrentState = GameState.Playing;
            CurrentDay = 1;
            TimeOfDay = 6f; // Start at 6 AM
            TimeScale = 1f;
            
            // Initialize systems
            World = new GameWorld(
                (int)tilemap.ScaleMapWidth, 
                (int)tilemap.ScaleMapHeight, 
                Scale, 
                (int)tilemap.ScaleTileWidth
            );
            World.Tilemap = tilemap;
            
            Resources = ResourceManager.Instance;
            
            // ✅ NEW: Initialize day/night cycle manager
            DayNightCycle = DayNightCycleManager.Instance;
            
            spawnManager = SpawnZoneManager.Instance;
            
            Console.WriteLine("[GameManager] ✅ Systems initialized");
        }
        
        private void LoadAtlases()
        {
            try
            {
                atlasPlayer = TextureAtlas.FromFile(Core.Content, "Images/farmer.xml");
                atlasResource = TextureAtlas.FromFile(Core.Content, "Images/resource.xml");
                atlasChicken = TextureAtlas.FromFile(Core.Content, "Images/Chicken.xml");
                atlasSheep = TextureAtlas.FromFile(Core.Content, "Images/sheep.xml");
                atlasBoar = TextureAtlas.FromFile(Core.Content, "Images/boar.xml");
                atlasAssassin = TextureAtlas.FromFile(Core.Content, "Images/Assassin.xml");; // TODO: Load proper assassin atlas
                
                Console.WriteLine("[GameManager] ✅ All atlases loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameManager] ❌ ERROR loading atlases: {ex.Message}");
                throw;
            }
        }
        
        public void Initialize()
        {
            Console.WriteLine("[GameManager] Running initialization sequence...");
            
            // 1. Initialize world systems
            World.Initialize();
            Resources.Initialize(
                new Rectangle(0, 0, (int)tilemap.ScaleMapWidth, (int)tilemap.ScaleMapHeight), 
                Scale
            );
            
            // 2. Setup spawn system
            SetupSpawnSystem();
            
            // 3. Initialize day/night cycle
            DayNightCycle.Initialize(this);
            
            // 4. Subscribe to events
            SubscribeToEvents();
            
            // 5. Validate player position
            World.player?.ValidatePosition();
            
            // 6. Sync pathfinder
            World.SyncPathfinderWithTilemap();
            
            Console.WriteLine("[GameManager] ✅ Initialization complete!");
        }
        
        /// <summary>
        /// ✅ NEW: Initialize day summary UI
        /// Call this after you have loaded fonts
        /// </summary>
        public void InitializeUI(SpriteFont titleFont, SpriteFont normalFont, SpriteFont smallFont, GraphicsDevice graphicsDevice)
        {
            daySummaryScreen = new DaySummaryScreen(titleFont, normalFont, smallFont);
            daySummaryScreen.Initialize(graphicsDevice);
            
            Console.WriteLine("[GameManager] ✅ UI initialized");
        }
        
        /// <summary>
        /// ✅ NEW: Subscribe to day/night cycle events
        /// </summary>
        private void SubscribeToEvents()
        {
            DayNightCycle.OnDayStart += OnDayStart;
            DayNightCycle.OnNightStart += OnNightStart;
            DayNightCycle.OnDayEnd += OnDayEnd;
            DayNightCycle.OnShowDaySummary += OnShowDaySummary;
            
            Console.WriteLine("[GameManager] ✅ Subscribed to day/night events");
        }
        
        private void OnDayStart()
        {
            Console.WriteLine($"[GameManager] ☀️ Day {CurrentDay} started!");
            
            // ✅ Don't reset stats - they accumulate across days
            // WoodCollected, StoneCollected, FoodCollected keep growing
        }
        
        private void OnNightStart()
        {
            Console.WriteLine("[GameManager] 🌙 Night has fallen... be careful!");
            
            // You can add UI warning here
        }
        
        private void OnDayEnd()
        {
            Console.WriteLine($"[GameManager] 💤 Day {CurrentDay} ended at 2 AM");
        }
        
        private void OnShowDaySummary(DaySummary summary)
        {
            CurrentState = GameState.DaySummary;
            
            if (daySummaryScreen != null)
            {
                daySummaryScreen.Show(summary);
            }
            
            Console.WriteLine("[GameManager] 📊 Showing day summary screen");
        }
        
        private void SetupSpawnSystem()
        {
            Console.WriteLine("[GameManager] Setting up spawn system...");
            
            spawnManager.Initialize(World, tilemap);
            
            // Register atlases
            spawnManager.RegisterAtlas("player", atlasPlayer);
            spawnManager.RegisterAtlas("resource", atlasResource);
            spawnManager.RegisterAtlas("chicken", atlasChicken);
            spawnManager.RegisterAtlas("sheep", atlasSheep);
            spawnManager.RegisterAtlas("boar", atlasBoar);
            spawnManager.RegisterAtlas("assassin", atlasAssassin);
            
            // Register atlases in World too (for night enemy spawning)
            World.RegisterAtlas("assassin", atlasAssassin);
            World.RegisterAtlas("boar", atlasBoar);
            
            // Parse object layer
            spawnManager.ParseObjectLayer("Object Layer 1");
            
            // Spawn player
            SpawnPlayer();
            
            // Queue initial entities (trees, bushes, etc.)
            // Animals will spawn at dawn via DayNightCycleManager
            spawnManager.SpawnInitialEntities();
            
            Console.WriteLine("[GameManager] ✅ Spawn system setup complete");
        }
        
        private void SpawnPlayer()
        {
            Vector2 spawnPos = tilemap.TileToWorld(22, 18);
            var player = new PlayerCharacter(1, spawnPos, atlasPlayer, Scale);
            player.IsActive = true;
            
            World.AddEntity(player);
            World.player = player;
            
            Console.WriteLine($"[Player] ✅ Spawned at tile (22, 18) → world ({spawnPos.X:F0}, {spawnPos.Y:F0})");
        }
        
        public void Update(GameTime gameTime)
        {
            switch (CurrentState)
            {
                case GameState.Playing:
                    UpdatePlaying(gameTime);
                    break;
                    
                case GameState.DaySummary:
                    UpdateDaySummary(gameTime);
                    break;
                    
                case GameState.Paused:
                    // Don't update game logic when paused
                    break;
                    
                case GameState.GameOver:
                    // Handle game over state
                    break;
            }
            
            // ✅ Check game over condition
            CheckGameOver();
        }
        
        private void UpdatePlaying(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale;
            
            // ✅ Update time
            UpdateTime(deltaTime);
            
            // ✅ Update day/night cycle manager
            DayNightCycle.Update(gameTime);
            
            // Process batch spawning
            spawnManager.Update(gameTime);
            
            // Update world
            World.Update(gameTime);
            Resources.Update(gameTime);
        }
        
        private void UpdateDaySummary(GameTime gameTime)
        {
            // Update day summary screen
            daySummaryScreen?.Update(gameTime);
            
            // Check if screen was closed
            if (daySummaryScreen != null && !daySummaryScreen.IsVisible)
            {
                CurrentState = GameState.Playing;
            }
        }
        
        /// <summary>
        /// ✅ Update game time with proper 6 AM → 2 AM cycle
        /// </summary>
        private void UpdateTime(float deltaTime)
        {
            // Get day length from DayNightCycleManager
            // Day cycle is 20 game hours (6 AM to 2 AM next day)
            float dayLength = DayNightCycle.DayLengthInSeconds;
            float gameHoursPerSecond = 20f / dayLength; // 20 hours in a cycle
            
            TimeOfDay += gameHoursPerSecond * deltaTime;
            
            // ✅ CRITICAL: When reaching 24:00, wrap to 0:00 (midnight)
            if (TimeOfDay >= 24f)
            {
                TimeOfDay -= 24f;
                Console.WriteLine("[GameManager] ⏰ Time wrapped to midnight (00:00)");
            }
            
            // ✅ Time stops at 2 AM when day summary is shown
            if (CurrentState == GameState.DaySummary)
            {
                TimeOfDay = 2f; // Lock at 2 AM
            }
        }
        public void DrawUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (CurrentState == GameState.DaySummary && daySummaryScreen != null)
            {
                daySummaryScreen.Draw(spriteBatch, gameTime);
            }
        }
                
        /// <summary>
        /// ✅ Check if player is dead
        /// </summary>
        private void CheckGameOver()
        {
            if (isGameOver) return;
            
            if (World.player != null && World.player.Health <= 0)
            {
                isGameOver = true;
                CurrentState = GameState.GameOver;
                Console.WriteLine("[GameManager] ☠️ GAME OVER - Player died!");
                
                // TODO: Show game over screen
            }
        }
        
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Rectangle viewBound)
        {
            // Draw world
            World.Draw(spriteBatch, gameTime, viewBound);
            
            // Draw UI overlays
            if (CurrentState == GameState.DaySummary)
            {
                daySummaryScreen?.Draw(spriteBatch, gameTime);
            }
        }
        
        // ==================== RESOURCE TRACKING ====================
        
        public void OnResourceCollected(string resourceType, int amount)
        {
            switch (resourceType)
            {
                case "wood":
                case "softwood":
                    WoodCollected += amount;
                    break;
                case "stone":
                    StoneCollected += amount;
                    break;
                case "berry":
                case "meat":
                    FoodCollected += amount;
                    break;
            }
            
            // Notify day/night cycle
            DayNightCycle.OnResourceCollected(resourceType, amount);
        }
        
        /// <summary>
        /// ✅ NEW: Track enemy kills
        /// </summary>
        public void OnEnemyKilled(NightEnemyEntity enemy)
        {
            DayNightCycle.OnEnemyKilled();
            Console.WriteLine($"[GameManager] Enemy killed: {enemy.EnemyType}");
        }
        
        // ==================== STATE MANAGEMENT ====================
        
        public void StartGame()
        {
            CurrentState = GameState.Playing;
            Console.WriteLine("[GameManager] 🎮 Game started!");
        }
        
        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Console.WriteLine("[GameManager] ⏸️ Game paused");
            }
        }
        
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                Console.WriteLine("[GameManager] ▶️ Game resumed");
            }
        }
        
        public void Shutdown()
        {
            Console.WriteLine("[GameManager] Shutting down...");
        }
        
        // ==================== HELPERS ====================
        
        public bool IsNight()
        {
            return DayNightCycle.CurrentPhase == DayNightCycleManager.TimeOfDayPhase.Night;
        }
        
        public bool IsDawn()
        {
            return TimeOfDay >= 5.5f && TimeOfDay <= 6.5f;
        }
        
        public bool IsDusk()
        {
            return TimeOfDay >= 19.5f && TimeOfDay <= 20.5f;
        }
        
        public string GetTimeString()
        {
            return DayNightCycle.GetTimeString();
        }
        
        public string GetTimeOfDayName()
        {
            return DayNightCycle.CurrentPhase.ToString();
        }
        
        /// <summary>
        /// Get spawn queue count
        /// </summary>
        public int GetSpawnQueueCount()
        {
            return spawnManager.GetQueuedEntityCount();
        }
    }
}