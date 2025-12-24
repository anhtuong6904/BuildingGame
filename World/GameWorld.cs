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
using TribeBuild.Entity.Enemies;
using TribeBuild.Player;

namespace TribeBuild.World
{
    /// <summary>
    /// ✅ OPTIMIZED: GameWorld with KD-Tree rebuild optimization
    /// - Rebuild only 2 times/second instead of every frame
    /// - Added missing methods for enemy spawning
    /// </summary>
    public class GameWorld
    {
        // World dimensions
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; }
        public Vector2 Scale { get; set; }
        
        // Entity management
        private Dictionary<int, Entity.Entity> allEntities;
        public PlayerCharacter player { get; set; }
        private List<NPCBody> npcs;
        private List<PassiveAnimal> passiveAnimals;
        private List<AggressiveAnimal> aggressiveAnimals;
        private List<NightEnemyEntity> nightEnemies;
        private List<ResourceEntity> resources;
        
        // Systems
        public GridPathfinder Pathfinder { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
        
        // ✅ OPTIMIZED: KD-Tree rebuild control
        public KDTree<Entity.Entity> KDTree { get; private set; }
        private bool needsKDRebuild = false;
        private float kdRebuildTimer = 0f;
        private const float KD_REBUILD_INTERVAL = 0.5f; // 2 times/second instead of 60!
        
        // Entity ID counter
        private int nextEntityID = 1;
        
        // Texture atlases
        private Dictionary<string, TextureAtlas> atlases;
        
        // Tilemap reference for collision matrix
        public Tilemap Tilemap { get; set; }
        
        // Sorted lists for depth rendering
        private List<IDrawable> drawableEntities = new List<IDrawable>();

        public GameWorld(int worldWidth, int worldHeight, Vector2 scale, int tileSize = 16)
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
            nightEnemies = new List<NightEnemyEntity>();
            resources = new List<ResourceEntity>();
            
            // Initialize systems
            Pathfinder = new GridPathfinder(worldWidth, worldHeight, tileSize);
            ResourceManager = ResourceManager.Instance;
            
            // Initialize KD-Tree
            KDTree = new KDTree<Entity.Entity>();
            
            atlases = new Dictionary<string, TextureAtlas>();
            
            Console.WriteLine($"[GameWorld] Created world {worldWidth}x{worldHeight} (Scale: {scale})");
        }

        public void Initialize()
        {
            InitializePathfinding();
            ResourceManager.Initialize(new Rectangle(0, 0, Width, Height), Scale);
            Console.WriteLine("[GameWorld] World initialized");
        }

        private void InitializePathfinding()
        {
            Console.WriteLine($"[GameWorld] Pathfinding grid initialized: {Pathfinder.GridWidth}x{Pathfinder.GridHeight}");
        }

        // ==================== UPDATE ====================
        
        public void Update(GameTime gameTime)
        {
            // Update resource manager
            ResourceManager.Update(gameTime);
            
            // ✅ OPTIMIZED: Only rebuild KD-Tree when needed
            kdRebuildTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (needsKDRebuild || kdRebuildTimer >= KD_REBUILD_INTERVAL)
            {
                RebuildSpatialTree();
                kdRebuildTimer = 0f;
                needsKDRebuild = false;
            }
            
            // Update all entities
            UpdateEntities(gameTime);
            
            // Cleanup dead entities
            CleanupEntities();
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
            
            // ✅ NEW: Update night enemies
            foreach (var enemy in nightEnemies.ToList())
            {
                if (enemy.IsActive)
                    enemy.Update(gameTime);
            }

            // Update resources
            foreach (var resource in resources.ToList())
            {
                if (resource.IsActive)
                    resource.Update(gameTime);
            }
        }

        /// <summary>
        /// ✅ OPTIMIZED: Rebuild KD-Tree with active entities
        /// Now called only 2 times/second instead of 60 times/second!
        /// </summary>
        private void RebuildSpatialTree()
        {
            var activeEntities = allEntities.Values.Where(e => e.IsActive).ToList();
            KDTree.Rebuild(activeEntities);
        }

        private void CleanupEntities()
        {
            var toRemove = allEntities.Values
                .Where(e => !e.IsActive)
                .Select(e => e.ID)
                .ToList();
            
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
                if (!entity.IsActive || !IsInViewport(entity.Position, viewportBounds))
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
            return viewport.X <= position.X && position.X <= viewport.Right &&
                   viewport.Y <= position.Y && position.Y <= viewport.Bottom;
        }

        // ==================== ENTITY MANAGEMENT ====================
        
        public PlayerCharacter GetPlayerCharacter => player;
        public int GetNextEntityID() => nextEntityID++;

        /// <summary>
        /// ✅ OPTIMIZED: Mark for rebuild instead of immediate rebuild
        /// </summary>
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

            entity.IsActive = true;
            allEntities[entity.ID] = entity;
            
            // Add to specific collections
            if (entity is PlayerCharacter playerChar)
            {
                this.player = playerChar;
                Console.WriteLine($"[GameWorld] ✅ Added PLAYER #{playerChar.ID}");
            }
            else if (entity is NPCBody npc)
            {
                npcs.Add(npc);
            }
            else if (entity is AggressiveAnimal aggressiveAnimal)
            {
                aggressiveAnimals.Add(aggressiveAnimal);
            }
            else if (entity is PassiveAnimal passiveAnimal)
            {
                passiveAnimals.Add(passiveAnimal);
            }
            else if (entity is NightEnemyEntity enemy)
            {
                nightEnemies.Add(enemy);
                Console.WriteLine($"[GameWorld] ✅ Added ENEMY #{enemy.ID} ({enemy.EnemyType})");
            }
            else if (entity is ResourceEntity resource)
            {
                resources.Add(resource);
                UpdatePathfindingForResource(resource, false);
            }
            
            // ✅ OPTIMIZED: Mark for rebuild instead of rebuilding immediately
            needsKDRebuild = true;
        }

        public void RemoveEntity(int entityID)
        {
            if (!allEntities.TryGetValue(entityID, out Entity.Entity entity))
                return;
            
            allEntities.Remove(entityID);
            
            // Remove from specific collections
            if (entity is PlayerCharacter)
            {
                player = null;
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
            else if (entity is NightEnemyEntity enemy)
            {
                nightEnemies.Remove(enemy);
            }
            else if (entity is ResourceEntity resource)
            {
                resources.Remove(resource);
                UpdatePathfindingForResource(resource, true);
            }
            
            // ✅ Mark for rebuild
            needsKDRebuild = true;
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

        public List<T> GetEntitiesInRadiusSafe<T>(Vector2 position, float radius, Predicate<T> predicate = null) 
            where T : Entity.Entity
        {
            if (KDTree == null || KDTree.Count == 0)
            {
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
        
        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            return Pathfinder.FindPath(start, end);
        }

        public PlayerCharacter FindNearestPlayer(Vector2 position, float maxDistance = float.MaxValue)
        {
            try
            {
                if (KDTree == null)
                {
                    return player?.IsActive == true ? player : null;
                }

                var results = KDTree.FindInRadius(
                    position, 
                    maxDistance, 
                    entity => entity is PlayerCharacter p && p.IsActive
                );

                if (results.Count > 0)
                {
                    return results[0].Item as PlayerCharacter;
                }
                
                // Fallback
                if (player?.IsActive == true && Vector2.Distance(position, player.Position) <= maxDistance)
                {
                    return player;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameWorld] ERROR in FindNearestPlayer: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// ✅ NEW: Find nearest valid spawn position for enemies
        /// </summary>
        public Vector2? FindNearestValidSpawnPosition(Vector2 startPos, Rectangle collider, CollisionLayer layer, float searchRadius = 150f)
        {
            // Check if start position is valid
            if (IsPositionValid(startPos, collider, layer))
                return startPos;

            // Search in expanding circles
            Random rng = new Random();
            for (float radius = 32f; radius <= searchRadius; radius += 32f)
            {
                for (int angle = 0; angle < 360; angle += 45)
                {
                    float rad = angle * MathHelper.ToRadians(1);
                    Vector2 testPos = startPos + new Vector2(
                        (float)Math.Cos(rad) * radius,
                        (float)Math.Sin(rad) * radius
                    );

                    if (IsPositionValid(testPos, collider, layer))
                        return testPos;
                }
            }

            return null;
        }

        /// <summary>
        /// ✅ NEW: Check if position is valid for spawning
        /// </summary>
        public bool IsPositionValid(Vector2 position, Rectangle collider, CollisionLayer layer)
        {
            // Check bounds
            if (position.X < 0 || position.X > Width || position.Y < 0 || position.Y > Height)
                return false;

            // Check tilemap walkability
            if (Tilemap != null)
            {
                Point tile = Tilemap.WorldToTile(position);
                if (!Tilemap.IsTileWalkable(tile.X, tile.Y))
                    return false;
            }

            // Check pathfinder
            if (!Pathfinder.IsWalkable(position))
                return false;

            // Check for overlapping entities
            Rectangle worldCollider = new Rectangle(
                (int)position.X + collider.X,
                (int)position.Y + collider.Y,
                collider.Width,
                collider.Height
            );

            foreach (var entity in allEntities.Values)
            {
                if (!entity.IsActive || !entity.BlocksMovement)
                    continue;

                Rectangle entityBox = new Rectangle(
                    (int)entity.Position.X + entity.Collider.X,
                    (int)entity.Position.Y + entity.Collider.Y,
                    entity.Collider.Width,
                    entity.Collider.Height
                );

                if (worldCollider.Intersects(entityBox))
                    return false;
            }

            return true;
        }

        public bool IsWalkable(Vector2 position)
        {
            return Pathfinder.IsWalkable(position);
        }

        private void UpdatePathfindingForResource(ResourceEntity resource, bool removing)
        {
            if (Pathfinder == null || Tilemap == null || resource.Collider == Rectangle.Empty) 
                return;
            
            Point tilePosTop = Tilemap.WorldToTile(resource.Position);
            
            int tileWidth = 1;
            int tileHeight = 1;
            
            if (resource is Tree)
            {
                tileWidth = 1;
                tileHeight = 2;
            }
            else if (resource is Bush)
            {
                tileWidth = 1;
                tileHeight = 1;
            }
            
            bool walkable = removing || !resource.BlocksMovement;
            
            for (int y = tilePosTop.Y; y < tilePosTop.Y + tileHeight; y++)
            {
                for (int x = tilePosTop.X; x < tilePosTop.X + tileWidth; x++)
                {
                    if (x >= 0 && x < Pathfinder.GridWidth && y >= 0 && y < Pathfinder.GridHeight)
                    {
                        Pathfinder.SetWalkable(x, y, walkable);
                    }
                }
            }
        }

        public void SetAreaWalkable(Rectangle area, bool walkable)
        {
            Pathfinder.SetAreaWalkable(area, walkable);
        }

        public void SetWalkable(int gridX, int gridY, bool walkable)
        {
            Pathfinder.SetWalkable(gridX, gridY, walkable);
        }

        public Point WorldToGrid(Vector2 worldPos)
        {
            return Pathfinder.WorldToGrid(worldPos);
        }

        public Vector2 GridToWorld(Point gridPos)
        {
            return Pathfinder.GridToWorld(gridPos);
        }

        // ==================== SPAWNING ====================
        
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
            var bush = ResourceManager.AddBush(position, Scale, type, sprite);
            AddEntity(bush);
            return bush;
        }

        /// <summary>
        /// ✅ NEW: Spawn night enemy
        /// </summary>
        public NightEnemyEntity SpawnNightEnemy(Vector2 position, NightEnemyType type, TextureAtlas atlas)
        {
            var enemy = new NightEnemyEntity(GetNextEntityID(), position, type, atlas, Scale);
            AddEntity(enemy);
            return enemy;
        }

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

        // ==================== TILEMAP SYNC ====================
        
        public void SyncPathfinderWithTilemap()
        {
            if (Tilemap == null)
            {
                Console.WriteLine("[GameWorld] ERROR: Cannot sync - Tilemap is null!");
                return;
            }
            
            Console.WriteLine("[GameWorld] Syncing pathfinder with tilemap...");
            
            for (int y = 0; y < Tilemap.Height; y++)
            {
                for (int x = 0; x < Tilemap.Width; x++)
                {
                    bool walkable = Tilemap.CollisionMatrix.GetTile(x, y) == TileCollisionType.Walkable;
                    Pathfinder.SetWalkable(x, y, walkable);
                }
            }
            
            Console.WriteLine("[GameWorld] Pathfinder sync complete");
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
                NightEnemyCount = nightEnemies.Count(e => e.IsActive),
                ResourceCount = ResourceManager.GetTotalActiveResources(),
                SpatialTreeSize = KDTree.Count
            };
        }

        public void LogStatistics()
        {
            var stats = GetStatistics();
            Console.WriteLine($"[GameWorld] Entities: {stats.TotalEntities} | NPCs: {stats.NPCCount} | " +
                $"Passive: {stats.PassiveAnimalCount} | Aggressive: {stats.AggressiveAnimalCount} | " +
                $"Enemies: {stats.NightEnemyCount} | Resources: {stats.ResourceCount}");
            
            ResourceManager.LogStatistics();
        }

        public void Clear()
        {
            allEntities.Clear();
            npcs.Clear();
            passiveAnimals.Clear();
            aggressiveAnimals.Clear();
            nightEnemies.Clear();
            resources.Clear();
            
            ResourceManager.Clear();
            
            Pathfinder = new GridPathfinder(Width, Height, TileSize);
            KDTree.Clear();
            
            nextEntityID = 1;
        }

        public void ForceDebugLog()
        {
            Console.WriteLine("\n========== GAMEWORLD DEBUG ==========");
            Console.WriteLine($"Total Entities: {allEntities.Count}");
            Console.WriteLine($"KD-Tree Size: {KDTree.Count}");
            Console.WriteLine($"KD-Tree Rebuild Rate: {1f / KD_REBUILD_INTERVAL:F1} times/sec");
            
            if (player != null)
            {
                Console.WriteLine($"Player: #{player.ID} at ({player.Position.X:F0}, {player.Position.Y:F0})");
            }
            
            Console.WriteLine($"Night Enemies: {nightEnemies.Count(e => e.IsActive)}");
            Console.WriteLine($"Aggressive Animals: {aggressiveAnimals.Count(a => a.IsActive)}");
            Console.WriteLine($"Passive Animals: {passiveAnimals.Count(a => a.IsActive)}");
            Console.WriteLine($"Resources: {resources.Count(r => r.IsActive)}");
            Console.WriteLine("=====================================\n");
        }

        // ==================== HELPER CLASSES ====================

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
        public int NightEnemyCount;
        public int ResourceCount;
        public int MineCount;
        public int PendingTasks;
        public int ActiveTasks;
        public int CompletedTasks;
        public int SpatialTreeSize;
    }
}