using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Spatial;
using TribeBuild.Entity.Resource;

namespace TribeBuild.Spatial
{
    /// <summary>
    /// Spatial indexing for game resources using MonoGameLibrary KD-Tree
    /// </summary>
    public class ResourceSpatialIndex
    {
        private KDTree<Tree> trees;
        private KDTree<Bush> bushes;
        private KDTree<Mine> mines;
        
        public ResourceSpatialIndex()
        {
            trees = new KDTree<Tree>();
            bushes = new KDTree<Bush>();
            mines = new KDTree<Mine>();
        }
        
        // ==================== ADD ====================
        
        public void AddTree(Tree tree)
        {
            trees.Insert(tree);
            //GameLogger.Instance?.Debug("Spatial", $"Added tree #{tree.ID}");
        }
        
        
        public void AddBush(Bush bush)
        {
            bushes.Insert(bush);
        }
        
        public void AddMine(Mine mine)
        {
            mines.Insert(mine);
        }
        
        // ==================== FIND ====================
        
        public Tree FindNearestTree(Vector2 position, float maxDistance = float.MaxValue)
        {
            return trees.FindNearest(
                position,
                t => t.IsActive && !t.IsBeingHarvested,out float distance
        );
    }
    
        // public Stone FindNearestStone(Vector2 position, float maxDistance = float.MaxValue)
        // {
        //     return stones.FindNearest(
        //         position,
        //         s => s.IsActive && !s.IsBeingHarvested,
        //         out _
        //     );
        // }
        
        public Bush FindNearestBush(Vector2 position, float maxDistance = float.MaxValue)
        {
            return bushes.FindNearest(
                position,
                b => b.IsActive && !b.IsBeingHarvested,
                out _
            );
        }
        
        public Mine FindNearestMine(Vector2 position, float maxDistance = float.MaxValue)
        {
            return mines.FindNearest(
                position,
                m => m.IsActive && !m.IsFull(),
                out _
            );
        }
        
        // ==================== STATISTICS ====================
        
        public void LogStatistics()
        {
            //GameLogger.Instance?.Info("Spatial", $"KD-Tree: {trees.Count} trees, {stones.Count} stones, {bushes.Count} bushes, {mines.Count} mines");
        }
    }
}