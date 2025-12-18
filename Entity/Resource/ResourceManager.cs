using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity.Resource
{
    /// <summary>
    /// Manages all resources in the game - trees, bushes, mines
    /// Uses KD-Tree for efficient spatial queries
    /// </summary>
    public class ResourceManager
    {
        // Singleton
        private static ResourceManager instance;
        public static ResourceManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ResourceManager();
                return instance;
            }
        }

        // Resource collections
        private List<Tree> trees;
        private List<Bush> bushes;
        private List<Mine> mines;

        // Spatial indexing with KD-Trees
        private KDTree<Tree> treeIndex;
        private KDTree<Bush> bushIndex;
        private KDTree<Mine> mineIndex;

        // ID counters
        private int nextTreeID = 10000;
        private int nextBushID = 20000;
        private int nextMineID = 30000;

        // World settings
        public Rectangle WorldBounds { get; private set; }
        public Vector2 Scale { get; private set; }

        // Statistics
        public int TotalTreesHarvested { get; private set; }
        public int TotalBushesHarvested { get; private set; }
        public int TotalMiningOperations { get; private set; }

        private ResourceManager()
        {
            trees = new List<Tree>();
            bushes = new List<Bush>();
            mines = new List<Mine>();

            treeIndex = new KDTree<Tree>();
            bushIndex = new KDTree<Bush>();
            mineIndex = new KDTree<Mine>();
        }

        /// <summary>
        /// Initialize the resource manager with world bounds
        /// </summary>
        public void Initialize(Rectangle worldBounds, Vector2 scale)
        {
            WorldBounds = worldBounds;
            Scale = scale;
            Console.WriteLine($"[ResourceManager] Initialized: {worldBounds.Width}x{worldBounds.Height}");
        }

        // ==================== ADD RESOURCES ====================

        public Tree AddTree(Vector2 position, Vector2 scale, TreeType type = TreeType.Oak, TextureAtlas atlas = null)
        {
            var tree = new Tree(nextTreeID++, position, scale, type, atlas);
            trees.Add(tree);
            treeIndex.Insert(tree);
            return tree;
        }

        public Bush AddBush(Vector2 position, BushType type = BushType.Berry, TextureAtlas atlas = null)
        {
            var bush = new Bush(nextBushID++, position, Scale, type, atlas);
            bushes.Add(bush);
            bushIndex.Insert(bush);
            return bush;
        }

        public Mine AddMine(Vector2 position, Sprite sprite = null)
        {
            var mine = new Mine(nextMineID++, position, sprite);
            mines.Add(mine);
            mineIndex.Insert(mine);
            return mine;
        }

        // ==================== QUERY RESOURCES (KD-Tree) ====================

        /// <summary>
        /// Find nearest tree using KD-Tree
        /// </summary>
        public Tree FindNearestTree(Vector2 position, float maxRange = float.MaxValue, bool onlyAvailable = true)
        {
            if (onlyAvailable)
            {
                return treeIndex.FindNearest(
                    position,
                    t => t.IsActive && !t.IsBeingHarvested,
                    out float distance
                );
            }
            return treeIndex.FindNearest(position, out _);
        }

        /// <summary>
        /// Find nearest bush using KD-Tree
        /// </summary>
        public Bush FindNearestBush(Vector2 position, float maxRange = float.MaxValue, bool onlyAvailable = true)
        {
            if (onlyAvailable)
            {
                return bushIndex.FindNearest(
                    position,
                    b => b.IsActive && !b.IsBeingHarvested,
                    out _
                );
            }
            return bushIndex.FindNearest(position, out _);
        }

        /// <summary>
        /// Find nearest mine with available slots using KD-Tree
        /// </summary>
        public Mine FindNearestAvailableMine(Vector2 position, float maxRange = float.MaxValue)
        {
            return mineIndex.FindNearest(
                position,
                m => m.IsActive && !m.IsFull(),
                out _
            );
        }

        /// <summary>
        /// Find nearest resource of any type (legacy compatibility)
        /// </summary>
        public ResourceEntity FindNearestResource(Vector2 position, ResourceType type, float maxRange = float.MaxValue, bool onlyAvailable = true)
        {
            switch (type)
            {
                case ResourceType.Tree:
                    return FindNearestTree(position, maxRange, onlyAvailable);
                case ResourceType.Bush:
                    return FindNearestBush(position, maxRange, onlyAvailable);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Find all trees within radius using KD-Tree
        /// </summary>
        public List<Tree> FindTreesInRadius(Vector2 position, float radius, bool onlyAvailable = true)
        {
            var results = treeIndex.FindInRadius(position, radius);
            
            if (onlyAvailable)
            {
                return results
                    .Where(r => r.Item.IsActive && !r.Item.IsBeingHarvested)
                    .OrderBy(r => r.Distance)
                    .Select(r => r.Item)
                    .ToList();
            }
            
            return results
                .OrderBy(r => r.Distance)
                .Select(r => r.Item)
                .ToList();
        }

        /// <summary>
        /// Find all bushes within radius using KD-Tree
        /// </summary>
        public List<Bush> FindBushesInRadius(Vector2 position, float radius, bool onlyAvailable = true)
        {
            var results = bushIndex.FindInRadius(position, radius);
            
            if (onlyAvailable)
            {
                return results
                    .Where(r => r.Item.IsActive && !r.Item.IsBeingHarvested)
                    .OrderBy(r => r.Distance)
                    .Select(r => r.Item)
                    .ToList();
            }
            
            return results
                .OrderBy(r => r.Distance)
                .Select(r => r.Item)
                .ToList();
        }

        /// <summary>
        /// Find all mines within radius using KD-Tree
        /// </summary>
        public List<Mine> FindMinesInRadius(Vector2 position, float radius, bool onlyAvailable = true)
        {
            var results = mineIndex.FindInRadius(position, radius);
            
            if (onlyAvailable)
            {
                return results
                    .Where(r => r.Item.IsActive && !r.Item.IsFull())
                    .OrderBy(r => r.Distance)
                    .Select(r => r.Item)
                    .ToList();
            }
            
            return results
                .OrderBy(r => r.Distance)
                .Select(r => r.Item)
                .ToList();
        }

        // ==================== GET LISTS ====================

        public List<Tree> GetAllTrees() => trees.Where(t => t.IsActive).ToList();
        public List<Bush> GetAllBushes() => bushes.Where(b => b.IsActive).ToList();
        public List<Mine> GetAllMines() => mines.Where(m => m.IsActive).ToList();

        public List<Tree> GetAvailableTrees() => trees.Where(t => t.IsActive && !t.IsBeingHarvested).ToList();
        public List<Bush> GetAvailableBushes() => bushes.Where(b => b.IsActive && !b.IsBeingHarvested).ToList();

        // ==================== UPDATE & DRAW ====================

        public void Update(GameTime gameTime)
        {
            // Update trees
            foreach (var tree in trees.ToList())
            {
                if (tree.IsActive || tree.CanRespawn)
                    tree.Update(gameTime);
            }

            // Update bushes
            foreach (var bush in bushes.ToList())
            {
                if (bush.IsActive || bush.CanRespawn)
                    bush.Update(gameTime);
            }

            // Update mines
            foreach (var mine in mines.ToList())
            {
                if (mine.IsActive)
                    mine.Update(gameTime);
            }

            // Cleanup destroyed resources
            CleanupDestroyedResources();
        }

        /// <summary>
        /// Draw only resources in viewport (optimized)
        /// </summary>
        public void DrawInView(SpriteBatch spriteBatch, GameTime gameTime, Rectangle viewBounds)
        {
            // Expand viewport bounds for large sprites
            Rectangle expandedBounds = new Rectangle(
                viewBounds.X - 64,
                viewBounds.Y - 64,
                viewBounds.Width + 128,
                viewBounds.Height + 128
            );

            // Draw trees in view
            foreach (var tree in trees)
            {
                if ((tree.IsActive || tree.CanRespawn) && 
                    expandedBounds.Contains(tree.Position))
                {
                    tree.Draw(spriteBatch, gameTime);
                }
            }

            // Draw bushes in view
            foreach (var bush in bushes)
            {
                if ((bush.IsActive || bush.CanRespawn) && 
                    expandedBounds.Contains(bush.Position))
                {
                    bush.Draw(spriteBatch, gameTime);
                }
            }

            // Draw mines in view
            foreach (var mine in mines)
            {
                if (mine.IsActive && expandedBounds.Contains(mine.Position))
                {
                    mine.Draw(spriteBatch, gameTime);
                }
            }
        }

        // ==================== RESOURCE EVENTS ====================

        public void OnResourceHarvested(ResourceEntity resource)
        {
            switch (resource.Type)
            {
                case ResourceType.Tree:
                    TotalTreesHarvested++;
                    break;
                case ResourceType.Bush:
                    TotalBushesHarvested++;
                    break;
            }
        }

        public void OnMiningComplete(Mine mine, int workerID)
        {
            TotalMiningOperations++;
        }

        // ==================== CLEANUP & MAINTENANCE ====================

        private void CleanupDestroyedResources()
        {
            // Remove trees that can't respawn
            int treeCountBefore = trees.Count;
            trees.RemoveAll(t => !t.IsActive && !t.CanRespawn);
            if (trees.Count < treeCountBefore)
            {
                RebuildTreeIndex();
            }

            // Remove bushes that can't respawn
            int bushCountBefore = bushes.Count;
            bushes.RemoveAll(b => !b.IsActive && !b.CanRespawn);
            if (bushes.Count < bushCountBefore)
            {
                RebuildBushIndex();
            }

            // Mines don't get destroyed, but check anyway
            int mineCountBefore = mines.Count;
            mines.RemoveAll(m => !m.IsActive);
            if (mines.Count < mineCountBefore)
            {
                RebuildMineIndex();
            }
        }

        private void RebuildTreeIndex()
        {
            treeIndex = new KDTree<Tree>(trees);
            Console.WriteLine($"[ResourceManager] Rebuilt tree index: {trees.Count} trees");
        }

        private void RebuildBushIndex()
        {
            bushIndex = new KDTree<Bush>(bushes);
            Console.WriteLine($"[ResourceManager] Rebuilt bush index: {bushes.Count} bushes");
        }

        private void RebuildMineIndex()
        {
            mineIndex = new KDTree<Mine>(mines);
            Console.WriteLine($"[ResourceManager] Rebuilt mine index: {mines.Count} mines");
        }

        public void RebuildAllIndexes()
        {
            RebuildTreeIndex();
            RebuildBushIndex();
            RebuildMineIndex();
        }

        // ==================== SPAWN RANDOM RESOURCES ====================

        public void SpawnRandomTrees(Rectangle area, int count, TreeType type = TreeType.Oak, TextureAtlas atlas = null)
        {
            var random = new Random();
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 50;

            while (spawned < count && attempts < maxAttempts)
            {
                attempts++;
                Vector2 position = new Vector2(
                    random.Next(area.Left, area.Right),
                    random.Next(area.Top, area.Bottom)
                );

                // Check if position is valid (not too close to other trees)
                var nearbyTrees = FindTreesInRadius(position, 48f, false);
                if (nearbyTrees.Count == 0)
                {
                    AddTree(position, Scale, type, atlas);
                    spawned++;
                }
            }

            Console.WriteLine($"[ResourceManager] Spawned {spawned} trees (attempted {attempts})");
        }

        public void SpawnRandomBushes(Rectangle area, int count, BushType type = BushType.Berry, TextureAtlas atlas = null)
        {
            var random = new Random();
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 50;

            while (spawned < count && attempts < maxAttempts)
            {
                attempts++;
                Vector2 position = new Vector2(
                    random.Next(area.Left, area.Right),
                    random.Next(area.Top, area.Bottom)
                );

                // Check if position is valid
                var nearbyBushes = FindBushesInRadius(position, 32f, false);
                if (nearbyBushes.Count == 0)
                {
                    AddBush(position, type, atlas);
                    spawned++;
                }
            }

            Console.WriteLine($"[ResourceManager] Spawned {spawned} bushes (attempted {attempts})");
        }

        // ==================== UTILITY ====================

        public int GetTotalActiveResources()
        {
            return trees.Count(t => t.IsActive) + 
                   bushes.Count(b => b.IsActive) + 
                   mines.Count(m => m.IsActive);
        }

        public Tree GetTreeByID(int id) => trees.FirstOrDefault(t => t.ID == id);
        public Bush GetBushByID(int id) => bushes.FirstOrDefault(b => b.ID == id);
        public Mine GetMineByID(int id) => mines.FirstOrDefault(m => m.ID == id);

        public bool HasResourceNearby(Vector2 position, float radius)
        {
            return FindTreesInRadius(position, radius, false).Count > 0 ||
                   FindBushesInRadius(position, radius, false).Count > 0;
        }

        public void Clear()
        {
            trees.Clear();
            bushes.Clear();
            mines.Clear();
            
            treeIndex = new KDTree<Tree>();
            bushIndex = new KDTree<Bush>();
            mineIndex = new KDTree<Mine>();

            nextTreeID = 10000;
            nextBushID = 20000;
            nextMineID = 30000;

            ResetStatistics();
            
            Console.WriteLine("[ResourceManager] Cleared all resources");
        }

        public void ResetStatistics()
        {
            TotalTreesHarvested = 0;
            TotalBushesHarvested = 0;
            TotalMiningOperations = 0;
        }

        public void LogStatistics()
        {
            Console.WriteLine($"[ResourceManager] Trees: {trees.Count} | Bushes: {bushes.Count} | Mines: {mines.Count}");
            Console.WriteLine($"[ResourceManager] Harvested: {TotalTreesHarvested} trees, {TotalBushesHarvested} bushes, {TotalMiningOperations} mining ops");
        }
    }
}