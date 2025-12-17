using System;
using Microsoft.Xna.Framework;
using TribeBuild.Entity;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Spatial;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using TribeBuild.Tasks;
// using MonoGameLibrary.Logging;

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
        
        // Time
        public int CurrentDay { get; private set; }
        public float TimeOfDay { get; private set; }
        public float TimeScale { get; set; }
        
        // Demo objectives
        public int WoodCollected { get; private set; }
        public int StoneCollected { get; private set; }
        public int FoodCollected { get; private set; }
        public bool DemoComplete { get; private set; }
        
        private TextureAtlas atlasObject;

        private Vector2 Scale;
        private Sprite tree;
        private Sprite bush;
        private Sprite bushBerry;
        private Sprite root;
        private Sprite Mine;
        private TextureAtlas atlasMine;
        private TextureAtlas atlasSheep;
        private TextureAtlas atlasChicken;
        private TextureAtlas atlasBush;
        private TextureAtlas atlasBoar;
        private float dayLength = 120f; // 2 minutes = 1 day
        
        public GameManager(Tilemap tilemap)
        {
            if (Instance != null)
            {
                throw new Exception("GameManager already exists!");
            }

            atlasBoar = TextureAtlas.FromFile(Core.Content, "Images/boar.xml");
            atlasBush = TextureAtlas.FromFile(Core.Content, "Images/Bush.xml");
            atlasChicken = TextureAtlas.FromFile(Core.Content, "Images/Chicken.xml");
            atlasMine = TextureAtlas.FromFile(Core.Content, "Images/Mine.xml");
            atlasObject = TextureAtlas.FromFile(Core.Content, "Images/object.xml");
            atlasSheep = TextureAtlas.FromFile(Core.Content, "Images/sheep.xml");
            tree = atlasObject.CreateSprite("Sprite-0001 0");
            root = atlasObject.CreateSprite("Sprite-0001 1");
            bush = atlasBush.CreateSprite("bush 0");
            bushBerry= atlasBush.CreateSprite("bush 2");            
            Mine = atlasMine.CreateSprite("Sprite-0001 0");
            
            Instance = this;
            
            CurrentState = GameState.MainMenu;
            CurrentDay = 1;
            TimeOfDay = 6f; // Start at 6 AM
            TimeScale = 1f;
            
            World = new GameWorld((int)tilemap.ScaleMapWidth, (int)tilemap.ScaleMapHeight, (int) tilemap.ScaleTileWidth);
            Resources = ResourceManager.Instance;
            Task = TaskManager.Instance;
            SpatialIndex = new ResourceSpatialIndex();
            
            //GameLogger.Instance.Info("GameManager", "GameManager created");
        }
        
        public void Initialize(int worldWidth, int worldHeight)
        {
            //GameLogger.Instance.Info("GameManager", $"Initializing game world: {worldWidth}x{worldHeight}");
            
            World.Initialize();
            Resources.Initialize(new Rectangle(0,0, worldWidth, worldHeight));
            
            SpawnInitialEntities();
            
            //GameLogger.Instance.GameEvent("GameManager", "Game initialization complete");
        }
        
        private void SpawnInitialEntities()
        {
            //GameLogger.Instance.Info("Spawning", "Spawning initial entities...");
            
            var random = new Random();
            
            // Spawn villagers
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(200 + i * 50, 200);
                var villagerAI = new VillagerAI("Farmer");
                villagerAI.HomePosition = new Vector2(150, 150);
                
                var villager = new NPCBody(100 + i, pos,villagerAI, null);
                World.AddEntity(villager);
                
                //GameLogger.Instance.LogEntitySpawned("Villager", 100 + i, pos);
            }
            
            // Spawn hunter
            var hunterAI = new HunterAI();
            hunterAI.HomePosition = new Vector2(150, 150);
            var hunter = new NPCBody(200, new Vector2(300, 200),  hunterAI,null);
            World.AddEntity(hunter);
            
            //GameLogger.Instance.LogEntitySpawned("Hunter", 200, new Vector2(300, 200));
            
            // Spawn resources
            SpawnResources(random);
            
            // Spawn animals
            SpawnAnimals(random);
            
            // Spawn mine
            var mine = new Mine(1000, new Vector2(600, 400), Mine);
            World.AddEntity(mine);
            SpatialIndex.AddMine(mine);
            
            //GameLogger.Instance.LogEntitySpawned("Mine", 1000, new Vector2(600, 400));
            
            //GameLogger.Instance.Info("Spawning", "Entity spawning complete");
        }
        
        private void SpawnResources(Random random)
        {
            // Trees
            for (int i = 0; i < 15; i++)
            {
                Vector2 pos = new Vector2(random.Next(100, 1400), random.Next(100, 900));
                var _tree = new Tree(2000 + i, pos, TreeType.Oak, tree);
                World.AddEntity(_tree);
                SpatialIndex.AddTree(_tree);
            }
            
            // // Stones
            // for (int i = 0; i < 10; i++)
            // {
            //     Vector2 pos = new Vector2(random.Next(100, 1400), random.Next(100, 900));
            //     var stone = new Stone(3000 + i, pos, null);
            //     World.AddEntity(stone);
            //     SpatialIndex.AddStone(stone);
            // }
            
            // Bushes
            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = new Vector2(random.Next(100, 1400), random.Next(100, 900));
                var _bush = new Bush(4000 + i, pos, BushType.Berry, bush);
                _bush.spriteCanHarvested = bushBerry;
                World.AddEntity(_bush);
                SpatialIndex.AddBush(_bush);
            }
            
            SpatialIndex.LogStatistics();
        }
        
        private void SpawnAnimals(Random random)
        {
            // Passive animals
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = new Vector2(random.Next(200, 1200), random.Next(200, 800));
                var Chicken = new PassiveAnimal(5000 + i, pos, AnimalType.Chicken, atlasChicken);
                World.AddEntity(Chicken);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = new Vector2(random.Next(200, 1200), random.Next(200, 800));
                var Sheep = new PassiveAnimal(5000 + i, pos, AnimalType.Sheep, atlasSheep);
                World.AddEntity(Sheep);
            }
            
            // Aggressive animals
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = new Vector2(random.Next(400, 1000), random.Next(400, 700));
                var Boar = new AggressiveAnimal(6000 + i, pos, AnimalType.Boar, atlasBoar);
                World.AddEntity(Boar);
            }
        }
        
        public void Update(GameTime gameTime)
        {
            switch (CurrentState)
            {
                case GameState.Playing:
                    UpdatePlaying(gameTime);
                    break;
            }


        }
        
        private void UpdatePlaying(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale;
            
            // Update time
            UpdateTime(deltaTime);
            
                        // Update world
            World.Update(gameTime);
            
            // Update jobs
            Task.Update(gameTime);
            
            // Check objectives
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
            //GameLogger.Instance.LogDayChange(CurrentDay);
            //GameLogger.Instance.Info("Stats", $"Resources - Wood: {WoodCollected}, Stone: {StoneCollected}, Food: {FoodCollected}");
        }
        
        private void CheckDemoObjectives()
        {
            if (WoodCollected >= 50 && StoneCollected >= 30 && FoodCollected >= 20)
            {
                if (!DemoComplete)
                {
                    DemoComplete = true;
                    //GameLogger.Instance.LogDemoObjective("Collect Resources (50 wood, 30 stone, 20 food)", true);
                    //GameLogger.Instance.GameEvent("Demo", "🎉 DEMO OBJECTIVES COMPLETE! 🎉");
                }
            }
        }
        
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Rectangle viewBound)
        {
            World.Draw(spriteBatch, gameTime, viewBound);
        }
        
        // ==================== RESOURCE TRACKING ====================
        
        public void OnResourceCollected(string resourceType, int amount)
        {
            //Resources.AddResource(resourceType, amount);
            
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
            //GameLogger.Instance.GameEvent("GameManager", "Game started");
        }
        
        public void PauseGame()
        {
            CurrentState = GameState.Paused;
            //GameLogger.Instance.Info("GameManager", "Game paused");
        }
        
        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
            //GameLogger.Instance.Info("GameManager", "Game resumed");
        }
        
        public void Shutdown()
        {
            //GameLogger.Instance.Info("GameManager", "Shutting down...");
            //GameLogger.Instance.Close();
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