using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.PathFinding;
using MonoGameLibrary.Graphics;
using TribeBuild.Entity;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.Resource;
using TribeBuild.Spatial;
using TribeBuild.Tasks;

namespace TribeBuild
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
        private List<NPCBody> npcs;
        private List<PassiveAnimal> passiveAnimals;
        private List<AggressiveAnimal> aggressiveAnimals;
        private List<ResourceEntity> resources;
        private List<Mine> mines;
        
        // Systems
        public GridPathfinder Pathfinder { get; private set; }
        public AnimalSpatialIndex AnimalSpatialIndex { get; private set; }
        public TaskManager TaskManager { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
        
        // Entity ID counter
        private int nextEntityID = 1;
        
        // Texture atlases
        private Dictionary<string, TextureAtlas> atlases;

         
        public Tilemap Tilemap { get; set; }  // Reference to tilemap for collision matrix
        
        // Sorted lists for depth rendering
        private List<IDrawable> drawableEntities = new List<IDrawable>();

        public GameWorld(int worldWidth, int worldHeight , Vector2 scale, int tileSize = 16)
        {
            Width = worldWidth;
            Height = worldHeight;
            TileSize = tileSize;
            Scale = scale;
            // Initialize collections
            allEntities = new Dictionary<int, Entity.Entity>();
            npcs = new List<NPCBody>();
            passiveAnimals = new List<PassiveAnimal>();
            aggressiveAnimals = new List<AggressiveAnimal>();
            resources = new List<ResourceEntity>();
            mines = new List<Mine>();
            
            // Initialize systems
            Pathfinder = new GridPathfinder(worldWidth, worldHeight, tileSize);
            AnimalSpatialIndex = new AnimalSpatialIndex();
            TaskManager = TaskManager.Instance;
            ResourceManager = ResourceManager.Instance;
            
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
            // Update task manager
            TaskManager.Update(gameTime);
            
            // Update resource manager
            ResourceManager.Update(gameTime);
            
            // Update all entities
            UpdateEntities(gameTime);
            
            // Update spatial indices
            UpdateSpatialIndices();
            
            // Cleanup dead entities
            CleanupEntities();
        }

        private void UpdateEntities(GameTime gameTime)
        {
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

        private void UpdateSpatialIndices()
        {
            // Spatial indices are updated dynamically when entities move/change
            // KD-Trees are rebuilt as needed in ResourceManager
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
            // Clear and populate drawable list with entities in viewport
            drawableEntities.Clear();
            
            // Add resources
            foreach (var tree in ResourceManager.GetAllTrees())
            {
                if (tree.IsActive && IsInViewport(tree.Position, viewportBounds))
                    drawableEntities.Add(new DrawableEntity(tree, tree.Position.Y));
            }
            
            foreach (var bush in ResourceManager.GetAllBushes())
            {
                if (bush.IsActive && IsInViewport(bush.Position, viewportBounds))
                    drawableEntities.Add(new DrawableEntity(bush, bush.Position.Y));
            }
            
            // Add mines
            foreach (var mine in mines)
            {
                if (mine.IsActive && IsInViewport(mine.Position, viewportBounds))
                    drawableEntities.Add(new DrawableEntity(mine, mine.Position.Y));
            }
            
            // Add passive animals
            foreach (var animal in passiveAnimals)
            {
                if (animal.IsActive && IsInViewport(animal.Position, viewportBounds))
                    drawableEntities.Add(new DrawableEntity(animal, animal.Position.Y));
            }
            
            // Add aggressive animals
            foreach (var animal in aggressiveAnimals)
            {
                if (animal.IsActive && IsInViewport(animal.Position, viewportBounds))
                    drawableEntities.Add(new DrawableEntity(animal, animal.Position.Y));
            }
            
            // Add NPCs
            foreach (var npc in npcs)
            {
                if (npc.IsActive && IsInViewport(npc.Position, viewportBounds))
                    drawableEntities.Add(new DrawableEntity(npc, npc.Position.Y));
            }
            
            // Sort by Y position (higher Y = drawn later = appears in front)
            drawableEntities.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            
            // Draw sorted entities
            foreach (var drawable in drawableEntities)
            {
                drawable.Entity.Draw(spriteBatch, gameTime);
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
        
        public int GetNextEntityID() => nextEntityID++;

        public void AddEntity(Entity.Entity entity)
        {
            if (entity == null || allEntities.ContainsKey(entity.ID))
                return;
            
            allEntities[entity.ID] = entity;
            
            // Add to specific collections
            if (entity is NPCBody npc)
            {
                npcs.Add(npc);
                //GameLogger.Instance?.Debug("World", $"Added NPC #{npc.ID} at ({npc.Position.X:F0}, {npc.Position.Y:F0})");
            }
            else if (entity is PassiveAnimal passiveAnimal)
            {
                passiveAnimals.Add(passiveAnimal);
                AnimalSpatialIndex.AddPassiveAnimal(passiveAnimal);
                //GameLogger.Instance?.Debug("World", $"Added {passiveAnimal.Type} #{passiveAnimal.ID}");
            }
            else if (entity is AggressiveAnimal aggressiveAnimal)
            {
                aggressiveAnimals.Add(aggressiveAnimal);
                AnimalSpatialIndex.AddAggressiveAnimal(aggressiveAnimal);
                //GameLogger.Instance?.Debug("World", $"Added {aggressiveAnimal.Type} #{aggressiveAnimal.ID}");
            }
            else if (entity is Mine mine)
            {
                mines.Add(mine);
                // Mines don't block paths - NPCs enter them
                //GameLogger.Instance?.Debug("World", $"Added mine #{mine.ID}");
            }
            else if (entity is ResourceEntity resource)
            {
                resources.Add(resource);
                
                // Update pathfinding - resources might block paths
                //UpdatePathfindingForResource(resource, false);
                
                //GameLogger.Instance?.Debug("World", $"Added resource #{resource.ID}");
            }
        }

        public void RemoveEntity(int entityID)
        {
            if (!allEntities.TryGetValue(entityID, out Entity.Entity entity))
                return;
            
            allEntities.Remove(entityID);
            
            // Remove from specific collections
            if (entity is NPCBody npc)
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
            
            //GameLogger.Instance?.Debug("World", $"Removed entity #{entityID}");
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
                CompletedTasks = TaskManager.CompletedTasksCount
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
            
            nextEntityID = 1;
            
            //GameLogger.Instance?.Info("World", "World cleared");
        }
         private interface IDrawable
        {
            Entity.Entity Entity { get; }
            float Depth { get; }
        }
        
        private class DrawableEntity : IDrawable
        {
            public Entity.Entity Entity { get; }
            public float Depth { get; }
            
            public DrawableEntity(Entity.Entity entity, float depth)
            {
                Entity = entity;
                Depth = depth;
            }
        }

        public void SyncPathfinderWithTilemap(Tilemap tilemap, GridPathfinder pathfinder)
{
            for (int y = 0; y < tilemap.Height; y++)
            {
                for (int x = 0; x < tilemap.Width; x++)
                {
                    bool walkable = tilemap.IsTileWalkable(x, y);
                    pathfinder.SetWalkable(x, y, walkable);
                }
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
    }
    
}