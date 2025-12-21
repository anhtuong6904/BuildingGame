using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.PathFinding;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;
using TribeBuild.Entity;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.Resource;

using TribeBuild.Tasks;
using TribeBuild.Player;

namespace TribeBuild.World
{
    /// <summary>
    /// Quản lý toàn bộ thế giới game:
    /// - Entities (NPCs, Animals, Resources, Buildings)
    /// - Pathfinding (A*)
    /// - Spatial indexing (KD-Tree)
    /// - Task management
    /// </summary>
    public class GameWorld
    {
        // World dimensions
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; }

        public Vector2 Scale {get; set;}
        
        // Entity management
        private Dictionary<int, Entity.Entity> allEntities;
        public PlayerCharacter player {get;  set;}
        private List<NPCBody> npcs;
        private List<PassiveAnimal> passiveAnimals;
        private List<AggressiveAnimal> aggressiveAnimals;
        private List<ResourceEntity> resources;
        private List<Mine> mines;
        
        // Systems
        public GridPathfinder Pathfinder { get; private set; }
        public TaskManager TaskManager { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
        
        // ✅ KD-Tree for fast spatial queries
        public KDTree<Entity.Entity> KDTree { get; private set; }
        
        // Entity ID counter
        private int nextEntityID = 1;
        
        // Texture atlases
        private Dictionary<string, TextureAtlas> atlases;

         
        public Tilemap Tilemap { get; set; }  // Reference to tilemap for collision matrix
        
        // Sorted lists for depth rendering
        private List<IDrawable> drawableEntities = new List<IDrawable>();
         private float debugLogTimer = 0f;
        private const float DEBUG_LOG_INTERVAL = 5f; // Log every 5 seconds

        public GameWorld(int worldWidth, int worldHeight , Vector2 scale, int tileSize = 16)
        {
            Width = worldWidth;
            Height = worldHeight;
            TileSize = tileSize;
            Scale = scale;
            // Initialize collections
            allEntities = new Dictionary<int, Entity.Entity>();
            player = null;
            npcs = new List<NPCBody>();
            passiveAnimals = new List<PassiveAnimal>();
            aggressiveAnimals = new List<AggressiveAnimal>();
            resources = new List<ResourceEntity>();
            mines = new List<Mine>();
            
            // Initialize systems
            Pathfinder = new GridPathfinder(worldWidth, worldHeight, tileSize);
            ResourceManager = ResourceManager.Instance;
            
            // ✅ Initialize KD-Tree
            KDTree = new KDTree<Entity.Entity>();
            
            atlases = new Dictionary<string, TextureAtlas>();
            
            //GameLogger.Instance?.Info("World", $"Created world {worldWidth}x{worldHeight} (tiles: {worldWidth/tileSize}x{worldHeight/tileSize})");
        }


        public void Initialize()
        {
            // Setup pathfinding grid
            InitializePathfinding();
            
            // Initialize ResourceManager
            ResourceManager.Initialize(new Rectangle(0, 0, Width, Height), Scale);
            
            
            //GameLogger.Instance?.Info("World", "World initialized");
        }

        private void InitializePathfinding()
        {
            // All tiles are walkable by default in GridPathfinder constructor
            // Buildings and obstacles will mark tiles as unwalkable when added
            
            //GameLogger.Instance?.Debug("Pathfinding", $"Pathfinding grid initialized: {Pathfinder.GridWidth}x{Pathfinder.GridHeight}");
        }

        // ==================== UPDATE ====================
        
       public void Update(GameTime gameTime)
        {
            // Update resource manager
            ResourceManager.Update(gameTime);
            
            // ✅ FIX: Rebuild KD-Tree FIRST with current positions
            RebuildSpatialTree();
            
            // Update all entities (they can now query accurate spatial data)
            UpdateEntities(gameTime);
            
            // Cleanup dead entities
            CleanupEntities();

            // Periodic debug logging
            debugLogTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (debugLogTimer >= DEBUG_LOG_INTERVAL)
            {
                DebugLog();
                debugLogTimer = 0f;
            }
        }




        private void UpdateEntities(GameTime gameTime)
        {
            // Update player
            player?.Update(gameTime);
            
            // Update NPCs
            foreach (var npc in npcs.ToList())
            {
                if (npc.IsActive)
                    npc.Update(gameTime);
            }
            
            // Update animals
            foreach (var animal in passiveAnimals.ToList())
            {
                if (animal.IsActive)
                    animal.Update(gameTime);
            }
            
            foreach (var animal in aggressiveAnimals.ToList())
            {
                if (animal.IsActive)
                    animal.Update(gameTime);
            }
            
            // Update mines
            foreach (var mine in mines.ToList())
            {
                if (mine.IsActive)
                    mine.Update(gameTime);
            }

            foreach (var resource in resources.ToList())
            {
                if (resource.IsActive)
                {
                    resource.Update(gameTime);
                }
            }
        }

        /// <summary>
        /// ✅ Rebuild KD-Tree with all active entities
        /// Called after entities move/update
        /// </summary>
       /// <summary>
        /// ✅ Rebuild KD-Tree with ALL active entities (including Player)
        /// </summary>
        private void RebuildSpatialTree()
        {
            var activeEntities = allEntities.Values.Where(e => e.IsActive).ToList();
            KDTree.Rebuild(activeEntities);
            
            // ✅ Verify Player is in KD-Tree (only log once per second to avoid spam)
            if (debugLogTimer >= DEBUG_LOG_INTERVAL - 0.1f && player != null)
            {
                var playerCheck = KDTree.FindInRadius(
                    player.Position, 
                    1f, 
                    e => e is PlayerCharacter
                );
                
                if (playerCheck.Count == 0)
                {
                    Console.WriteLine("⚠️ [GameWorld] BUG: Player NOT in KD-Tree after rebuild!");
                    Console.WriteLine($"   Player: ID={player.ID}, Active={player.IsActive}, Pos=({player.Position.X:F0},{player.Position.Y:F0})");
                    Console.WriteLine($"   KD-Tree size: {KDTree.Count}, allEntities: {allEntities.Count}");
                }
            }
        }


        private void CleanupEntities()
        {
            // Remove dead/inactive entities
            var toRemove = new List<int>();
            
            foreach (var kvp in allEntities)
            {
                if (!kvp.Value.IsActive)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove)
            {
                RemoveEntity(id);
            }
        }

        // ==================== DRAW ====================
        
       public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Rectangle viewportBounds)
        {
            drawableEntities.Clear();

            foreach (var entity in allEntities.Values)
            {
                if (!entity.IsActive)
                    continue;

                if (!IsInViewport(entity.Position, viewportBounds))
                    continue;

                drawableEntities.Add(new DrawableEntity(entity));
            }

            drawableEntities.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            foreach (var drawable in drawableEntities)
            {
                drawable.Entity.Draw(spriteBatch);
            }
        }


        private bool IsInViewport(Vector2 position, Rectangle viewport)
        {
            // Add margin for large sprites
            const int margin = 64;
            return viewport.X - margin <= position.X && position.X <= viewport.Right + margin &&
                   viewport.Y - margin <= position.Y && position.Y <= viewport.Bottom + margin;
        }
        

        // ==================== ENTITY MANAGEMENT ====================
        
        public PlayerCharacter GetPlayerCharacter => player;
        public int GetNextEntityID() => nextEntityID++;

        public void AddEntity(Entity.Entity entity)
        {
            if (entity == null)
            {
                Console.WriteLine("[GameWorld] ERROR: Attempted to add null entity!");
                return;
            }
            
            if (allEntities.ContainsKey(entity.ID))
            {
                Console.WriteLine($"[GameWorld] WARNING: Entity #{entity.ID} already exists!");
                return;
            }

            // ✅ Set entity as active FIRST
            entity.IsActive = true;
            
            // ✅ Add to main dictionary (includes Player!)
            allEntities[entity.ID] = entity;
            
            // ✅ Add to specific collections for faster queries
            if (entity is PlayerCharacter player)
            {
                this.player = player;  // ← Set ở đây
                Console.WriteLine($"[GameWorld] ✅ Added PLAYER #{player.ID}...");
            }
            else if (entity is NPCBody npc)
            {
                npcs.Add(npc);
                Console.WriteLine($"[GameWorld] Added NPC #{npc.ID} at ({npc.Position.X:F0}, {npc.Position.Y:F0})");
            }
            else if (entity is AggressiveAnimal aggressiveAnimal)
            {
                aggressiveAnimals.Add(aggressiveAnimal);
                Console.WriteLine($"[GameWorld] Added {aggressiveAnimal.Type} #{aggressiveAnimal.ID} at ({aggressiveAnimal.Position.X:F0}, {aggressiveAnimal.Position.Y:F0})");
            }
            else if (entity is PassiveAnimal passiveAnimal)
            {
                passiveAnimals.Add(passiveAnimal);
                Console.WriteLine($"[GameWorld] Added {passiveAnimal.Type} #{passiveAnimal.ID} at ({passiveAnimal.Position.X:F0}, {passiveAnimal.Position.Y:F0})");
            }
            else if (entity is Mine mine)
            {
                mines.Add(mine);
                Console.WriteLine($"[GameWorld] Added Mine #{mine.ID}");
            }
            else if (entity is ResourceEntity resource)
            {
                resources.Add(resource);
                
                // Update pathfinding - resources might block paths
                UpdatePathfindingForResource(resource, false);
                
                // Only log every 10th resource to avoid spam
                if (resources.Count % 10 == 0)
                {
                    Console.WriteLine($"[GameWorld] Added {resources.Count} resources so far...");
                }
            }
        }

         public void setPlayer(PlayerCharacter playerCharacter)
        {
            player = playerCharacter;
            Console.WriteLine($"[GameWorld] ✅ Set Player reference: #{playerCharacter.ID}");
            // Verify player is in allEntities
            if (!allEntities.ContainsKey(playerCharacter.ID))
            {
                Console.WriteLine($"⚠️ [GameWorld] WARNING: Player #{playerCharacter.ID} not in allEntities!");
            }
        }

        public void RemoveEntity(int entityID)
        {
            if (!allEntities.TryGetValue(entityID, out Entity.Entity entity))
                return;
            
            allEntities.Remove(entityID);
            
            // Remove from specific collections
            if (entity is PlayerCharacter)
            {
                // Player reference is managed separately
                Console.WriteLine($"[GameWorld] Removed PLAYER #{entityID}");
            }
            else if (entity is NPCBody npc)
            {
                npcs.Remove(npc);
            }
            else if (entity is PassiveAnimal passiveAnimal)
            {
                passiveAnimals.Remove(passiveAnimal);
            }
            else if (entity is AggressiveAnimal aggressiveAnimal)
            {
                aggressiveAnimals.Remove(aggressiveAnimal);
            }
            else if (entity is Mine mine)
            {
                mines.Remove(mine);
            }
            else if (entity is ResourceEntity resource)
            {
                resources.Remove(resource);
                UpdatePathfindingForResource(resource, true);
            }
        }

        public Entity.Entity GetEntity(int id)
        {
            return allEntities.TryGetValue(id, out Entity.Entity entity) ? entity : null;
        }

        // ==================== ENTITY QUERIES ====================
        
        public List<T> GetEntitiesOfType<T>() where T : Entity.Entity
        {
            return allEntities.Values.OfType<T>().Where(e => e.IsActive).ToList();
        }

        public List<Entity.Entity> GetEntitiesInRadius(Vector2 position, float radius)
        {
            var result = new List<Entity.Entity>();
            float radiusSquared = radius * radius;
            
            foreach (var entity in allEntities.Values)
            {
                if (!entity.IsActive) continue;
                
                float distSquared = Vector2.DistanceSquared(position, entity.Position);
                if (distSquared <= radiusSquared)
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }

        // Add to GameWorld.cs - Document 4

        /// <summary>
        /// ✅ SAFE: Query entities with validation
        /// </summary>
        public List<T> GetEntitiesInRadiusSafe<T>(Vector2 position, float radius, Predicate<T> predicate = null) 
            where T : Entity.Entity
        {
            if (KDTree == null || KDTree.Count == 0)
            {
                Console.WriteLine("[GameWorld] WARNING: KD-Tree is empty!");
                return new List<T>();
            }
            
            var results = KDTree.FindInRadius(position, radius);
            var filtered = new List<T>();
            
            foreach (var result in results)
            {
                if (result.Item is T entity && entity.IsActive)
                {
                    if (predicate == null || predicate(entity))
                    {
                        filtered.Add(entity);
                    }
                }
            }
            
            return filtered;
        }

        /// <summary>
        /// ✅ Use this in AggressiveAnimal detection logic
        /// </summary>
        public PlayerCharacter FindNearestPlayer(Vector2 position, float maxRange)
        {
            var players = GetEntitiesInRadiusSafe<PlayerCharacter>(
                position, 
                maxRange, 
                p => p.IsActive
            );
            
            if (players.Count == 0)
                return null;
            
            // Return closest player
            PlayerCharacter nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var p in players)
            {
                float dist = Vector2.Distance(position, p.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = p;
                }
            }
            
            return nearest;
        }



        public NPCBody GetNearestNPC(Vector2 position, float maxDistance = float.MaxValue)
        {
            NPCBody nearest = null;
            float nearestDist = maxDistance * maxDistance;
            
            foreach (var npc in npcs)
            {
                if (!npc.IsActive) continue;
                
                float distSquared = Vector2.DistanceSquared(position, npc.Position);
                if (distSquared < nearestDist)
                {
                    nearestDist = distSquared;
                    nearest = npc;
                }
            }
            
            return nearest;
        }

        // ==================== PATHFINDING ====================
        
        /// <summary>
        /// Find path between two positions using A* pathfinding
        /// </summary>
        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            return Pathfinder.FindPath(start, end);
        }

        /// <summary>
        /// Check if a position is walkable
        /// </summary>
        public bool IsWalkable(Vector2 position)
        {
            return Pathfinder.IsWalkable(position);
        }

        /// <summary>
        /// Update pathfinding grid when a resource is added/removed
        /// </summary>
        private void UpdatePathfindingForResource(ResourceEntity resource, bool removing)
        {
            if (Pathfinder == null || resource.Collider == Rectangle.Empty) 
                return;
            
            // Mark tiles occupied by the resource's collider as unwalkable
            bool walkable = removing || !resource.BlocksPath;
            
            // Use SetAreaWalkable to mark the entire collider area
            Pathfinder.SetAreaWalkable(resource.Collider, walkable);
            
            //GameLogger.Instance?.Debug("Pathfinding", 
            //    $"Updated pathfinding for resource #{resource.ID}: walkable={walkable}");
        }

        /// <summary>
        /// Update pathfinding grid for a specific area
        /// </summary>
        public void SetAreaWalkable(Rectangle area, bool walkable)
        {
            Pathfinder.SetAreaWalkable(area, walkable);
        }

        /// <summary>
        /// Update pathfinding for a single grid cell
        /// </summary>
        public void SetWalkable(int gridX, int gridY, bool walkable)
        {
            Pathfinder.SetWalkable(gridX, gridY, walkable);
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Point WorldToGrid(Vector2 worldPos)
        {
            return Pathfinder.WorldToGrid(worldPos);
        }

        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector2 GridToWorld(Point gridPos)
        {
            return Pathfinder.GridToWorld(gridPos);
        }

        // ==================== SPAWNING ====================
        
        public NPCBody SpawnVillager(Vector2 position, TextureAtlas atlas = null)
        {
            var villager = new NPCBody(GetNextEntityID(), position, new VillagerAI(), atlas);
            AddEntity(villager);
            return villager;
        }

        public NPCBody SpawnHunter(Vector2 position, TextureAtlas atlas = null)
        {
            var hunter = new NPCBody(GetNextEntityID(), position, new HunterAI(), atlas);
            AddEntity(hunter);
            return hunter;
        }

        public NPCBody SpawnMiner(Vector2 position, TextureAtlas atlas = null)
        {
            var miner = new NPCBody(GetNextEntityID(), position, new VillagerAI(), atlas);
            AddEntity(miner);
            return miner;
        }

        public PassiveAnimal SpawnPassiveAnimal(Vector2 position, AnimalType type, TextureAtlas atlas = null)
        {
            var animal = new PassiveAnimal(GetNextEntityID(), position, type, atlas, Scale);
            AddEntity(animal);
            return animal;
        }

        public AggressiveAnimal SpawnAggressiveAnimal(Vector2 position, AnimalType type, TextureAtlas atlas = null)
        {
            var animal = new AggressiveAnimal(GetNextEntityID(), position, type, atlas, Scale);
            AddEntity(animal);
            return animal;
        }

        public Tree SpawnTree(Vector2 position, TreeType type = TreeType.Oak, TextureAtlas sprite = null)
        {
            var tree = ResourceManager.AddTree(position, Scale, type, sprite);
            AddEntity(tree);
            return tree;
        }

        public Bush SpawnBush(Vector2 position, BushType type = BushType.Berry, TextureAtlas sprite = null)
        {
            var bush = ResourceManager.AddBush(position, type, sprite);
            AddEntity(bush);
            return bush;
        }

        public Mine SpawnMine(Vector2 position, Sprite sprite = null)
        {
            var mine = ResourceManager.AddMine(position, sprite);
            AddEntity(mine);
            return mine;
        }

        /// <summary>
        /// Spawn multiple random resources in an area
        /// </summary>
        public void SpawnRandomResources(Rectangle area, int treeCount = 50, int bushCount = 30)
        {
            ResourceManager.SpawnRandomTrees(area, treeCount);
            ResourceManager.SpawnRandomBushes(area, bushCount);
        }

        // ==================== TEXTURE MANAGEMENT ====================
        
        public void RegisterAtlas(string name, TextureAtlas atlas)
        {
            atlases[name] = atlas;
        }

        public TextureAtlas GetAtlas(string name)
        {
            return atlases.TryGetValue(name, out TextureAtlas atlas) ? atlas : null;
        }

        // ==================== STATISTICS ====================
        
        public WorldStatistics GetStatistics()
        {
            return new WorldStatistics
            {
                TotalEntities = allEntities.Count,
                NPCCount = npcs.Count(n => n.IsActive),
                PassiveAnimalCount = passiveAnimals.Count(a => a.IsActive),
                AggressiveAnimalCount = aggressiveAnimals.Count(a => a.IsActive),
                ResourceCount = ResourceManager.GetTotalActiveResources(),
                MineCount = mines.Count(m => m.IsActive),
                PendingTasks = TaskManager.GetTaskCount(TaskStatus.Pending),
                ActiveTasks = TaskManager.GetTaskCount(TaskStatus.InProgress),
                CompletedTasks = TaskManager.CompletedTasksCount,
                SpatialTreeSize = KDTree.Count
            };
        }

        public void LogStatistics()
        {
            var stats = GetStatistics();
            //GameLogger.Instance?.Info("World", 
            //    $"Entities: {stats.TotalEntities} | NPCs: {stats.NPCCount} | " +
            //    $"Passive: {stats.PassiveAnimalCount} | Aggressive: {stats.AggressiveAnimalCount} | " +
            //    $"Resources: {stats.ResourceCount} | Mines: {stats.MineCount}");
            
            //GameLogger.Instance?.Info("Tasks", 
            //    $"Pending: {stats.PendingTasks} | Active: {stats.ActiveTasks} | Completed: {stats.CompletedTasks}");
            
            ResourceManager.LogStatistics();
        }

        /// <summary>
        /// Clear all entities and reset world
        /// </summary>
        public void Clear()
        {
            allEntities.Clear();
            npcs.Clear();
            passiveAnimals.Clear();
            aggressiveAnimals.Clear();
            resources.Clear();
            mines.Clear();
            
            ResourceManager.Clear();
            TaskManager.ClearAllTasks();
            
            // Reset pathfinding grid
            Pathfinder = new GridPathfinder(Width, Height, TileSize);
            
            // Reset KD-Tree
            KDTree.Clear();
            
            nextEntityID = 1;
        }

         
        /// <summary>
        /// ✅ FIXED: Now actually gets called every 5 seconds
        /// </summary>
        private void DebugLog()
        {
            Console.WriteLine("\n========== GAMEWORLD DEBUG ==========");
            Console.WriteLine($"Total Entities: {allEntities.Count}");
            Console.WriteLine($"KD-Tree Size: {KDTree.Count}");
            
            if (player != null)
            {
                Console.WriteLine($"Player: #{player.ID} at ({player.Position.X:F0}, {player.Position.Y:F0}), Active: {player.IsActive}");
                
                // ✅ Test KD-Tree player query
                var playersInTree = KDTree.FindInRadius(
                    Vector2.Zero, 
                    float.MaxValue, 
                    e => e is PlayerCharacter
                );
                Console.WriteLine($"  - Players in KD-Tree: {playersInTree.Count}");
                
                if (playersInTree.Count == 0)
                {
                    Console.WriteLine("  ⚠️ WARNING: Player NOT in KD-Tree!");
                }
            }
            else
            {
                Console.WriteLine("Player: NULL");
            }
            
            Console.WriteLine($"Aggressive Animals: {aggressiveAnimals.Count(a => a.IsActive)}");
            foreach (var animal in aggressiveAnimals.Where(a => a.IsActive).Take(3))
            {
                Console.WriteLine($"  - {animal.Type} #{animal.ID} at ({animal.Position.X:F0}, {animal.Position.Y:F0})");
                
                // Test if this animal can find player
                if (player != null)
                {
                    var nearbyPlayers = KDTree.FindInRadius(
                        animal.Position,
                        300f,
                        e => e is PlayerCharacter p && p.IsActive
                    );
                    float distance = Vector2.Distance(animal.Position, player.Position);
                    Console.WriteLine($"    Distance to player: {distance:F1}, Can detect: {nearbyPlayers.Count > 0}");
                    
                    if (nearbyPlayers.Count == 0 && distance < 300f)
                    {
                        Console.WriteLine($"    ⚠️ BUG: Player within range but NOT detected by KD-Tree!");
                    }
                }
            }
            
            Console.WriteLine($"Passive Animals: {passiveAnimals.Count(a => a.IsActive)}");
            Console.WriteLine($"Resources: {resources.Count(r => r.IsActive)} (Trees: {resources.OfType<Tree>().Count(t => t.IsActive)})");
            Console.WriteLine("=====================================\n");
        }

        /// <summary>
        /// ✅ ADD: Manual debug command for immediate logging
        /// </summary>
        public void ForceDebugLog()
        {
            DebugLog();
        }


        
        public void SyncPathfinderWithTilemap()
        {
            var tilemap = GameManager.Instance.World.Tilemap;
            var pathfinder = GameManager.Instance.World.Pathfinder;
            
            Console.WriteLine("[GameWorld] Syncing pathfinder with tilemap...");
            
            for (int y = 0; y < tilemap.Height; y++)
            {
                for (int x = 0; x < tilemap.Width; x++)
                {
                    // ✅ Mark water tiles as non-walkable in pathfinder
                    if (tilemap.IsWaterTile(x, y))
                    {
                        pathfinder.SetWalkable(x, y, false);
                    }
                    
                    // ✅ Also check collision matrix
                    if (!tilemap.IsTileWalkable(x, y))
                    {
                        pathfinder.SetWalkable(x, y, false);
                    }
                }
            }
            
            Console.WriteLine("[GameWorld] Pathfinder sync complete");
        }

        internal bool CanMoveTo(AggressiveAnimal aggressiveAnimal, Vector2 newPosition)
        {
            throw new NotImplementedException();
        }

        private interface IDrawable
        {
            Entity.Entity Entity { get; }
            float Depth { get; }
        }
    
     private class DrawableEntity : IDrawable
        {
            public Entity.Entity Entity { get; }
            public float Depth => Entity.GetFootY();

            public DrawableEntity(Entity.Entity entity)
            {
                Entity = entity;
            }
        }
    }
    
    

    // ==================== STATISTICS ====================
    
    public struct WorldStatistics
    {
        public int TotalEntities;
        public int NPCCount;
        public int PassiveAnimalCount;
        public int AggressiveAnimalCount;
        public int ResourceCount;
        public int MineCount;
        public int PendingTasks;
        public int ActiveTasks;
        public int CompletedTasks;
        public int SpatialTreeSize;
    }
    
}
