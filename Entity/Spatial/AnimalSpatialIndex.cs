using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Spatial;
using MonoGameLibrary.PathFinding;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.NPC;
//using TribeBuild.Logging;

namespace TribeBuild.Spatial
{
    /// <summary>
    /// Spatial indexing for animals using KD-Tree
    /// </summary>
    public class AnimalSpatialIndex
    {
        private KDTree<PassiveAnimal> passiveAnimals;
        private KDTree<AggressiveAnimal> aggressiveAnimals;

        public AnimalSpatialIndex()
        {
            passiveAnimals = new KDTree<PassiveAnimal>();
            aggressiveAnimals = new KDTree<AggressiveAnimal>();
        }

        // ==================== ADD ====================

        public void AddPassiveAnimal(PassiveAnimal animal)
        {
            passiveAnimals.Insert(animal);
            //GameLogger.Instance?.Debug("Spatial", $"Added passive {animal.Type} to KD-Tree");
        }

        public void AddAggressiveAnimal(AggressiveAnimal animal)
        {
            aggressiveAnimals.Insert(animal);
            //GameLogger.Instance?.Debug("Spatial", $"Added aggressive {animal.Type} to KD-Tree");
        }

        // ==================== FIND ====================

        /// <summary>
        /// Find nearest passive animal (for hunting)
        /// </summary>
        public PassiveAnimal FindNearestPassiveAnimal(Vector2 position, float maxDistance = 300f)
        {
            return passiveAnimals.FindNearest(
                position,
                a => a.IsActive,
                out float distance
            );
        }

        /// <summary>
        /// Find all passive animals in radius
        /// </summary>
        public List<PassiveAnimal> FindPassiveAnimalsInRadius(Vector2 position, float radius)
        {
            var results = passiveAnimals.FindInRadius(position, radius);
            return results
                .Where(r => r.Item.IsActive)
                .Select(r => r.Item)
                .ToList();
        }

        /// <summary>
        /// Find nearest NPC for aggressive animal to attack
        /// </summary>
        public NPCBody FindNearestNPCForAttack(Vector2 position, float maxDistance = 150f)
        {
            // This should use a KD-Tree for NPCs
            
            // For now, return null (will be implemented when we add NPC spatial index)
            return null;
        }

        // ==================== STATISTICS ====================

        public void LogStatistics()
        {
            //GameLogger.Instance?.Info("Spatial", $"Animal KD-Tree: {passiveAnimals.Count} passive, {aggressiveAnimals.Count} aggressive");
        }
    }
}