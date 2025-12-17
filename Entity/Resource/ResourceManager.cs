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
    /// Manages all resources in the game - trees, bushes, mines, etc.
    /// Uses KD-Tree for efficient spatial queries
    /// </summary>
    public class ResourceManager
    {
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
        private List<ResourceEntity> allResources;

        // Spatial indexing with KD-Trees
        private KDTree<Tree> treeIndex;
        private KDTree<Bush> bushIndex;
        private KDTree<Mine> mineIndex;

        // ID management
        private int nextTreeID = 0;
        private int nextBushID = 1000;
        private int nextMineID = 2000;

        // Statistics
        public int TotalTreesHarvested { get; private set; }
        public int TotalBushesHarvested { get; private set; }
        public int TotalMiningOperations { get; private set; }

        private Rectangle worldBounds;

        private ResourceManager()
        {
            trees = new List<Tree>();
            bushes = new List<Bush>();
            mines = new List<Mine>();
            allResources = new List<ResourceEntity>();

            treeIndex = new KDTree<Tree>();
            bushIndex = new KDTree<Bush>();
            mineIndex = new KDTree<Mine>();

            TotalTreesHarvested = 0;
            TotalBushesHarvested = 0;
            TotalMiningOperations = 0;
        }

        /// <summary>
        /// Initialize the resource manager with world bounds
        /// </summary>
        public void Initialize(Rectangle worldBounds)
        {
            this.worldBounds = worldBounds;
            //GameLogger.Instance?.Debug("ResourceManager", $"Initialized with bounds: {worldBounds}");
        }

        #region Add Resources

        /// <summary>
        /// Add a tree to the world
        /// </summary>
        public Tree AddTree(Vector2 position, TreeType type = TreeType.Oak, Sprite sprite = null)
        {
            var tree = new Tree(nextTreeID++, position, type, sprite);
            trees.Add(tree);
            allResources.Add(tree);
            treeIndex.Insert(tree);

            //GameLogger.Instance?.Debug("ResourceManager", $"Added tree #{tree.ID} at ({position.X:F0}, {position.Y:F0})");
            return tree;
        }

        /// <summary>
        /// Add a bush to the world
        /// </summary>
        public Bush AddBush(Vector2 position, BushType type = BushType.Berry, Sprite sprite = null)
        {
            var bush = new Bush(nextBushID++, position, type, sprite);
            bushes.Add(bush);
            allResources.Add(bush);
            bushIndex.Insert(bush);

            //GameLogger.Instance?.Debug("ResourceManager", $"Added bush #{bush.ID} at ({position.X:F0}, {position.Y:F0})");
            return bush;
        }

        /// <summary>
        /// Add a mine to the world
        /// </summary>
        public Mine AddMine(Vector2 position, Sprite sprite = null)
        {
            var mine = new Mine(nextMineID++, position, sprite);
            mines.Add(mine);
            mineIndex.Insert(mine);

            //GameLogger.Instance?.Debug("ResourceManager", $"Added mine #{mine.ID} at ({position.X:F0}, {position.Y:F0})");
            return mine;
        }

        #endregion

        #region Query Resources (Using KD-Tree)

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
            else
            {
                return treeIndex.FindNearest(position, out _);
            }
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
            else
            {
                return bushIndex.FindNearest(position, out _);
            }
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
        /// Find nearest resource of any type (legacy method)
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
            
            return results.Select(r => r.Item).ToList();
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
            
            return results.Select(r => r.Item).ToList();
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
            
            return results.Select(r => r.Item).ToList();
        }

        /// <summary>
        /// Find all resources within radius (legacy method)
        /// </summary>
        public List<ResourceEntity> FindResourcesInRadius(Vector2 position, float radius, ResourceType? filterType = null)
        {
            var results = new List<ResourceEntity>();

            if (filterType == null || filterType == ResourceType.Tree)
            {
                results.AddRange(FindTreesInRadius(position, radius));
            }

            if (filterType == null || filterType == ResourceType.Bush)
            {
                results.AddRange(FindBushesInRadius(position, radius));
            }

            return results.OrderBy(r => Vector2.Distance(r.Position, position)).ToList();
        }

        /// <summary>
        /// Get all available resources of a specific type
        /// </summary>
        public List<ResourceEntity> GetAvailableResources(ResourceType type)
        {
            return allResources
                .Where(r => r.Type == type && r.IsActive && !r.IsBeingHarvested)
                .ToList();
        }

        /// <summary>
        /// Get all active mines
        /// </summary>
        public List<Mine> GetActiveMines()
        {
            return mines.Where(m => m.IsActive).ToList();
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Update all resources
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Update trees
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                trees[i].Update(gameTime);
            }

            // Update bushes
            for (int i = bushes.Count - 1; i >= 0; i--)
            {
                bushes[i].Update(gameTime);
            }

            // Update mines
            for (int i = mines.Count - 1; i >= 0; i--)
            {
                mines[i].Update(gameTime);
            }

            // Clean up destroyed resources that can't respawn
            CleanupDestroyedResources();
        }

        /// <summary>
        /// Draw all resources
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Draw trees
            foreach (var tree in trees)
            {
                if (tree.IsActive || tree.CanRespawn)
                    tree.Draw(spriteBatch, gameTime);
            }

            // Draw bushes
            foreach (var bush in bushes)
            {
                if (bush.IsActive || bush.CanRespawn)
                    bush.Draw(spriteBatch, gameTime);
            }

            // Draw mines
            foreach (var mine in mines)
            {
                if (mine.IsActive)
                    mine.Draw(spriteBatch, gameTime);
            }
        }

        /// <summary>
        /// Draw only resources in camera view (optimized)
        /// </summary>
        public void DrawInView(SpriteBatch spriteBatch, GameTime gameTime, Rectangle viewBounds)
        {
            // Draw trees in view
            foreach (var tree in trees)
            {
                if ((tree.IsActive || tree.CanRespawn) && viewBounds.Intersects(tree.Collider))
                    tree.Draw(spriteBatch, gameTime);
            }

            // Draw bushes in view
            foreach (var bush in bushes)
            {
                if ((bush.IsActive || bush.CanRespawn) && viewBounds.Intersects(bush.Collider))
                    bush.Draw(spriteBatch, gameTime);
            }

            // Draw mines in view
            foreach (var mine in mines)
            {
                if (mine.IsActive && viewBounds.Intersects(mine.Collider))
                    mine.Draw(spriteBatch, gameTime);
            }
        }

        #endregion

        #region Resource Events

        /// <summary>
        /// Called when a resource is harvested
        /// </summary>
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

            //GameLogger.Instance?.GameEvent("Resource", $"{resource.Type} #{resource.ID} harvested");
        }

        /// <summary>
        /// Called when mining operation completes
        /// </summary>
        public void OnMiningComplete(Mine mine, int workerID)
        {
            TotalMiningOperations++;
            //GameLogger.Instance?.GameEvent("Mining", $"Worker #{workerID} completed mining at mine #{mine.ID}");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Remove resources that are destroyed and can't respawn, rebuild KD-Trees
        /// </summary>
        private void CleanupDestroyedResources()
        {
            bool needsRebuild = false;

            // Remove destroyed trees
            int treeCount = trees.Count;
            trees.RemoveAll(t => !t.IsActive && !t.CanRespawn);
            if (trees.Count < treeCount)
            {
                needsRebuild = true;
                RebuildTreeIndex();
            }

            // Remove destroyed bushes
            int bushCount = bushes.Count;
            bushes.RemoveAll(b => !b.IsActive && !b.CanRespawn);
            if (bushes.Count < bushCount)
            {
                needsRebuild = true;
                RebuildBushIndex();
            }

            // Update all resources list
            allResources.RemoveAll(r => !r.IsActive && !r.CanRespawn);
        }

        /// <summary>
        /// Rebuild tree KD-Tree index
        /// </summary>
        private void RebuildTreeIndex()
        {
            treeIndex = new KDTree<Tree>(trees);
            //GameLogger.Instance?.Debug("ResourceManager", $"Rebuilt tree index with {trees.Count} trees");
        }

        /// <summary>
        /// Rebuild bush KD-Tree index
        /// </summary>
        private void RebuildBushIndex()
        {
            bushIndex = new KDTree<Bush>(bushes);
            //GameLogger.Instance?.Debug("ResourceManager", $"Rebuilt bush index with {bushes.Count} bushes");
        }

        /// <summary>
        /// Rebuild mine KD-Tree index
        /// </summary>
        private void RebuildMineIndex()
        {
            mineIndex = new KDTree<Mine>(mines);
            //GameLogger.Instance?.Debug("ResourceManager", $"Rebuilt mine index with {mines.Count} mines");
        }

        /// <summary>
        /// Rebuild all spatial indexes
        /// </summary>
        public void RebuildAllIndexes()
        {
            RebuildTreeIndex();
            RebuildBushIndex();
            RebuildMineIndex();
        }

        /// <summary>
        /// Get total count of resources by type
        /// </summary>
        public int GetResourceCount(ResourceType type)
        {
            return allResources.Count(r => r.Type == type && r.IsActive);
        }

        /// <summary>
        /// Get total count of all active resources
        /// </summary>
        public int GetTotalActiveResources()
        {
            return allResources.Count(r => r.IsActive);
        }

        /// <summary>
        /// Check if position has a resource nearby
        /// </summary>
        public bool HasResourceNearby(Vector2 position, float radius, ResourceType? filterType = null)
        {
            return FindResourcesInRadius(position, radius, filterType).Count > 0;
        }

        /// <summary>
        /// Get resource by ID
        /// </summary>
        public ResourceEntity GetResourceByID(int id)
        {
            return allResources.FirstOrDefault(r => r.ID == id);
        }

        /// <summary>
        /// Get mine by ID
        /// </summary>
        public Mine GetMineByID(int id)
        {
            return mines.FirstOrDefault(m => m.ID == id);
        }

        /// <summary>
        /// Clear all resources
        /// </summary>
        public void Clear()
        {
            trees.Clear();
            bushes.Clear();
            mines.Clear();
            allResources.Clear();
            
            treeIndex = new KDTree<Tree>();
            bushIndex = new KDTree<Bush>();
            mineIndex = new KDTree<Mine>();

            nextTreeID = 0;
            nextBushID = 1000;
            nextMineID = 2000;

            //GameLogger.Instance?.Debug("ResourceManager", "Cleared all resources");
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStatistics()
        {
            TotalTreesHarvested = 0;
            TotalBushesHarvested = 0;
            TotalMiningOperations = 0;
        }

        /// <summary>
        /// Spawn random trees in area
        /// </summary>
        public void SpawnRandomTrees(Rectangle area, int count, TreeType type = TreeType.Oak, Sprite sprite = null)
        {
            Random random = new Random();
            for (int i = 0; i < count; i++)
            {
                Vector2 position = new Vector2(
                    random.Next(area.Left, area.Right),
                    random.Next(area.Top, area.Bottom)
                );
                AddTree(position, type, sprite);
            }
        }

        /// <summary>
        /// Spawn random bushes in area
        /// </summary>
        public void SpawnRandomBushes(Rectangle area, int count, BushType type = BushType.Berry, Sprite sprite = null)
        {
            Random random = new Random();
            for (int i = 0; i < count; i++)
            {
                Vector2 position = new Vector2(
                    random.Next(area.Left, area.Right),
                    random.Next(area.Top, area.Bottom)
                );
                AddBush(position, type, sprite);
            }
        }

        /// <summary>
        /// Log spatial index statistics
        /// </summary>
        public void LogStatistics()
        {
            //GameLogger.Instance?.Info("ResourceManager", 
            //    $"KD-Trees: {treeIndex.Count} trees, {bushIndex.Count} bushes, {mineIndex.Count} mines");
            //GameLogger.Instance?.Info("ResourceManager", 
            //    $"Total harvested: {TotalTreesHarvested} trees, {TotalBushesHarvested} bushes, {TotalMiningOperations} mining ops");
        }

        #endregion
    }
}