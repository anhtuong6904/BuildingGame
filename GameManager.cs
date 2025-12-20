using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using TribeBuild.Entity;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Spatial;
using TribeBuild.Tasks;

namespace TribeBuild
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
    
    /// <summary>
    /// Central game manager for TribeBuild
    /// Manages game state, entities, and systems
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
        public TaskManager Task { get; private set; }
        public ResourceSpatialIndex SpatialIndex { get; private set; }
        
        // Tilemap
        private Tilemap tilemap;
        
        // Time
        public int CurrentDay { get; private set; }
        public float TimeOfDay { get; private set; }
        public float TimeScale { get; set; }
        private float dayLength = 120f; // 2 minutes = 1 day
        
        // Demo objectives
        public int WoodCollected { get; private set; }
        public int StoneCollected { get; private set; }
        public int FoodCollected { get; private set; }
        public bool DemoComplete { get; private set; }
        
        // Texture atlases
        private TextureAtlas atlasBoar;
        private TextureAtlas atlasBush;
        private TextureAtlas atlasChicken;
        private TextureAtlas atlasMine;
        private TextureAtlas atlasResource;
        private TextureAtlas atlasSheep;
        
        // Sprites
        private Sprite Mine;
        private Vector2 Scale;
        
        // Spawn settings (in tiles)
        private const int RESOURCE_SPACING = 1;
        private const int ANIMAL_SPACING = 1;
        private const int NPC_SPACING = 1;
        private const int MINE_SPACING = 6;
        
        public GameManager(Tilemap tilemap)
        {
            if (Instance != null)
            {
                throw new Exception("GameManager already exists!");
            }
            
            Instance = this;
            this.tilemap = tilemap;
            Scale = tilemap.Scale;
            
            // Load texture atlases
            atlasBoar = TextureAtlas.FromFile(Core.Content, "Images/boar.xml");
            atlasBush = TextureAtlas.FromFile(Core.Content, "Images/Bush.xml");
            atlasChicken = TextureAtlas.FromFile(Core.Content, "Images/Chicken.xml");
            atlasMine = TextureAtlas.FromFile(Core.Content, "Images/Mine.xml");
            atlasResource = TextureAtlas.FromFile(Core.Content, "Images/resource.xml");
            atlasSheep = TextureAtlas.FromFile(Core.Content, "Images/sheep.xml");
            
            Mine = atlasMine.CreateSprite("Sprite-0001 0");
            Mine._scale = Scale;
            
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
            Task = TaskManager.Instance;
            SpatialIndex = new ResourceSpatialIndex();
        }
        
        public void Initialize(int worldWidth, int worldHeight)
        {
            World.Initialize();
            Resources.Initialize(new Rectangle(0, 0, worldWidth, worldHeight), Scale);
            
            SpawnInitialEntities();
        }

        
        
        // ==================== ENTITY SPAWNING ====================
        
        private void SpawnInitialEntities()
        {
            var random = new Random();
            
            Console.WriteLine("[GameManager] Spawning entities...");
            
            SpawnVillagers(random, 3);
            SpawnHunter(random);
            SpawnResources(random, treeCount: 30, bushCount: 20);
            SpawnAnimals(random, chickens: 15, sheep: 15, boars: 8);
            SpawnMine(random);
            
            Console.WriteLine("[GameManager] Spawn complete");
        }
        
        private void SpawnVillagers(Random random, int count)
        {
            Point homeArea = new Point(10, 10);
            Vector2 homePos = tilemap.TileToWorld(homeArea.X, homeArea.Y);
            
            for (int i = 0; i < count; i++)
            {
                Point? tile = FindValidTile(homeArea, 5, random, NPC_SPACING);
                if (!tile.HasValue) continue;
                
                Vector2 pos = tilemap.TileToWorld(tile.Value.X, tile.Value.Y);
                var ai = new VillagerAI("Farmer") { HomePosition = homePos };
                var villager = new NPCBody(100 + i, pos, ai, null);
                
                World.AddEntity(villager);
            }
        }
        
        private void SpawnHunter(Random random)
        {
            Point homeArea = new Point(15, 10);
            Point? tile = FindValidTile(homeArea, 5, random, NPC_SPACING);
            if (!tile.HasValue) return;
            
            Vector2 pos = tilemap.TileToWorld(tile.Value.X, tile.Value.Y);
            Vector2 homePos = tilemap.TileToWorld(10, 10);
            var ai = new HunterAI() { HomePosition = homePos };
            var hunter = new NPCBody(200, pos, ai, null);
            
            World.AddEntity(hunter);
        }
        
        private void SpawnResources(Random random, int treeCount, int bushCount)
        {
            int treesSpawned = SpawnTrees(random, treeCount);
            int bushesSpawned = SpawnBushes(random, bushCount);
            
            Console.WriteLine($"[Resources] Trees: {treesSpawned}/{treeCount} | Bushes: {bushesSpawned}/{bushCount}");
            SpatialIndex.LogStatistics();
        }
        
        private int SpawnTrees(Random random, int count)
        {
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 100;
            
            while (spawned < count && attempts < maxAttempts)
            {
                attempts++;
                
                Point tile = new Point(
                    random.Next(0, tilemap.Width ),
                    random.Next(0, tilemap.Height - 0)
                );
                
                if (!CanPlaceResource(tile, 2, 2, RESOURCE_SPACING))
                    continue;
                
                Vector2 pos = tilemap.TileToWorld(tile.X, tile.Y);
                var tree = new Tree(2000 + spawned, pos, Scale, TreeType.Oak, atlasResource);
                
                World.AddEntity(tree);
                Resources.AddTree(pos, Scale, TreeType.Oak, atlasResource);
                SpatialIndex.AddTree(tree);
                tilemap.BlockTilesForResource(pos, 2, 2);
                
                spawned++;
            }
            
            return spawned;
        }
        
        private int SpawnBushes(Random random, int count)
        {
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 100;
            
            while (spawned < count && attempts < maxAttempts)
            {
                attempts++;
                
                Point tile = new Point(
                    random.Next(0, tilemap.Width ),
                    random.Next(0, tilemap.Height)
                );
                
                if (!CanPlaceResource(tile, 2, 2, RESOURCE_SPACING))
                    continue;
                
                Vector2 pos = tilemap.TileToWorld(tile.X, tile.Y);
                var bush = new Bush(4000 + spawned, pos, Scale, BushType.Berry, atlasResource);
                
                World.AddEntity(bush);
                Resources.AddBush(pos, BushType.Berry, atlasResource);
                SpatialIndex.AddBush(bush);
                tilemap.BlockTilesForResource(pos, 2, 2);
                
                spawned++;
            }
            
            return spawned;
        }
        
        private void SpawnAnimals(Random random, int chickens, int sheep, int boars)
        {
            int chickenCount = SpawnAnimalType(random, chickens, 5000, AnimalType.Chicken, atlasChicken);
            int sheepCount = SpawnAnimalType(random, sheep, 5010, AnimalType.Sheep, atlasSheep);
            int boarCount = SpawnAnimalType(random, boars, 6000, AnimalType.Boar, atlasBoar);
            
            Console.WriteLine($"[Animals] Chickens: {chickenCount}/{chickens} | Sheep: {sheepCount}/{sheep} | Boars: {boarCount}/{boars}");
        }
        
        private int SpawnAnimalType(Random random, int count, int startID, AnimalType type, TextureAtlas atlas)
        {
            int spawned = 0;
            
            for (int i = 0; i < count; i++)
            {
                Point center = new Point(
                    random.Next(10, tilemap.Width - 10),
                    random.Next(10, tilemap.Height - 10)
                );
                
                Point? tile = FindValidTile(center, 10, random, ANIMAL_SPACING);
                if (!tile.HasValue) continue;
                
                Vector2 pos = tilemap.TileToWorld(tile.Value.X, tile.Value.Y);
                
                if (type == AnimalType.Boar)
                {
                    var boar = new AggressiveAnimal(startID + i, pos, type, atlas, Scale);
                    World.AddEntity(boar);
                }
                else
                {
                    var animal = new PassiveAnimal(startID + i, pos, type, atlas, Scale);
                    World.AddEntity(animal);
                }
                
                spawned++;
            }
            
            return spawned;
        }
        
        private void SpawnMine(Random random)
        {
            Point center = new Point(tilemap.Width / 2, tilemap.Height / 2);
            Point? tile = FindValidTile(center, 10, random, MINE_SPACING);
            
            if (!tile.HasValue) return;
            
            Vector2 pos = tilemap.TileToWorld(tile.Value.X, tile.Value.Y);
            var mine = new Mine(1000, pos, Mine);
            
            World.AddEntity(mine);
            SpatialIndex.AddMine(mine);
            tilemap.BlockTilesForResource(pos, 3, 3);
        }
        
        // ==================== SPAWN VALIDATION ====================
        
        private Point? FindValidTile(Point center, int searchRadius, Random random, int minSpacing)
        {
            if (IsTileValid(center, minSpacing))
                return center;
            
            for (int r = 1; r <= searchRadius; r++)
            {
                for (int angle = 0; angle < 360; angle += 45)
                {
                    float rad = angle * MathHelper.ToRadians(1);
                    int x = center.X + (int)(Math.Cos(rad) * r);
                    int y = center.Y + (int)(Math.Sin(rad) * r);
                    
                    Point tile = new Point(x, y);
                    if (IsTileValid(tile, minSpacing))
                        return tile;
                }
            }
            
            return null;
        }
        
        private bool IsTileValid(Point tile, int minSpacing)
        {
            if (tile.X < 0 || tile.X >= tilemap.Width || 
                tile.Y < 0 || tile.Y >= tilemap.Height)
                return false;
            
            // 1. Không cho spawn trên nước
            if (tilemap.IsWaterTile(tile.X, tile.Y))
                return false;

            // 2. Không cho spawn trên tile bị block
            if (!tilemap.IsTileWalkable(tile.X, tile.Y))
                return false;
            
            Vector2 worldPos = tilemap.TileToWorld(tile.X, tile.Y);
            float minDist = minSpacing * tilemap.ScaleTileWidth;
            
            return World.GetEntitiesInRadius(worldPos, minDist).Count == 0;
        }
        
        private bool CanPlaceResource(Point tile, int width, int height, int minSpacing)
        {
            for (int y = tile.Y; y < tile.Y + height; y++)
            {
                for (int x = tile.X; x < tile.X + width; x++)
                {
                    // ⛔ chặn nước + collision + spacing
                    if (!IsTileValid(new Point(x, y), minSpacing))
                        return false;
                }
            }
            return true;
        }

        
        // ==================== UPDATE ====================
        
        public void Update(GameTime gameTime)
        {
            if (CurrentState == GameState.Playing)
            {
                UpdatePlaying(gameTime);
            }
        }
        
        private void UpdatePlaying(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale;
            
            UpdateTime(deltaTime);
            World.Update(gameTime);
            Resources.Update(gameTime);
            Task.Update(gameTime);
            CheckDemoObjectives();
        }
        
        private void UpdateTime(float deltaTime)
        {
            TimeOfDay += (24f / dayLength) * deltaTime;
            
            if (TimeOfDay >= 24f)
            {
                TimeOfDay -= 24f;
                CurrentDay++;
                OnNewDay();
            }
        }
        
        private void OnNewDay()
        {
            Console.WriteLine($"[GameManager] Day {CurrentDay} - Resources: Wood {WoodCollected}, Stone {StoneCollected}, Food {FoodCollected}");
        }
        
        private void CheckDemoObjectives()
        {
            if (WoodCollected >= 50 && StoneCollected >= 30 && FoodCollected >= 20)
            {
                if (!DemoComplete)
                {
                    DemoComplete = true;
                    Console.WriteLine("[GameManager] 🎉 DEMO OBJECTIVES COMPLETE! 🎉");
                }
            }
        }
        
        // ==================== DRAW ====================
        
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Rectangle viewBound)
        {
            World.Draw(spriteBatch, gameTime, viewBound);
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
        }
        
        // ==================== STATE MANAGEMENT ====================
        
        public void StartGame()
        {
            CurrentState = GameState.Playing;
        }
        
        public void PauseGame()
        {
            CurrentState = GameState.Paused;
        }
        
        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
        }
        
        public void Shutdown()
        {
            // Cleanup
        }
        
        // ==================== HELPERS ====================
        
        public bool IsNight()
        {
            return TimeOfDay < 6f || TimeOfDay > 20f;
        }
        
        public string GetTimeString()
        {
            int hours = (int)TimeOfDay;
            int minutes = (int)((TimeOfDay - hours) * 60);
            return $"{hours:D2}:{minutes:D2}";
        }
    }
}