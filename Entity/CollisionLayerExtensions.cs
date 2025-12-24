using System;

namespace TribeBuild.Entity
{
    //// <summary>
    /// Collision layers for filtering
    /// </summary>
    [Flags]
    public enum CollisionLayer
    {
        None = 0,
        Default = 1 << 0,
        Player = 1 << 1,
        NPC = 1 << 2,
        Animal = 1 << 3,
        Resource = 1 << 4,
        Building = 1 << 5,
        Projectile = 1 << 6,
        All = ~0
    }

    /// <summary>
    /// ✅ Base Entity with continuous AABB collision checking
    /// </summary>
        public static class CollisionLayerExtensions
    {
        public static bool Contains(this CollisionLayer layer, CollisionLayer check)
        {
            return (layer & check) == check;
        }
        
        public static CollisionLayer Add(this CollisionLayer layer, CollisionLayer add)
        {
            return layer | add;
        }
        
        public static CollisionLayer Remove(this CollisionLayer layer, CollisionLayer remove)
        {
            return layer & ~remove;
        }
        
        /// <summary>
        /// ✅ Check if two layers can collide based on game rules
        /// </summary>
       public static bool CanCollideWith(this CollisionLayer layer, CollisionLayer other)
        {
            // Player
            if (layer.Contains(CollisionLayer.Player))
            {
                return !other.Contains(CollisionLayer.Projectile);
            }

            // NPC
            if (layer.Contains(CollisionLayer.NPC))
            {
                return other.Contains(CollisionLayer.Player) ||
                    other.Contains(CollisionLayer.NPC) ||
                    other.Contains(CollisionLayer.Animal) ||
                    other.Contains(CollisionLayer.Resource) ||
                    other.Contains(CollisionLayer.Building);
            }

            // ✅ Animal (FIX)
            if (layer.Contains(CollisionLayer.Animal))
            {
                return other.Contains(CollisionLayer.Player) ||
                    other.Contains(CollisionLayer.NPC) ||
                    //other.Contains(CollisionLayer.Animal) || // ✅ Animal vs Animal OK
                    other.Contains(CollisionLayer.Resource) ||
                    other.Contains(CollisionLayer.Building);
            }

            // Resource
            if (layer.Contains(CollisionLayer.Resource))
            {
                return !other.Contains(CollisionLayer.Projectile);
            }

            // Building
            if (layer.Contains(CollisionLayer.Building))
            {
                return !other.Contains(CollisionLayer.Projectile);
            }

            // Projectile
            if (layer.Contains(CollisionLayer.Projectile))
            {
                return other.Contains(CollisionLayer.NPC) ||
                    other.Contains(CollisionLayer.Animal);
            }

            return false;
        }

    }
}