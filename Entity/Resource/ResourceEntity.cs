using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity.Resource
{
    public enum ResourceType
    {
        Tree,
        Stone,
        Ore,
        Bush,
    }

    /// <summary>
    /// ✅ Base class for resources with proper collision management
    /// </summary>
    public abstract class ResourceEntity : Entity, IPosition
    {
        public ResourceType Type { get; protected set; }
        public float Health { get; set; }
        public float MaxHealth { get; protected set; }
        public int YieldAmount { get; protected set; }
        public string YieldItem { get; protected set; }

        protected TextureAtlas textureAtlas { get; set; }
        
        public bool IsBeingHarvested { get; set; }
        public Entity Harvester { get; set; }

        public bool CanRespawn { get; protected set; }
        public float RespawnTime { get; protected set; }

        private float respawnTimer;
    
        public ResourceEntity(int id, Vector2 pos, ResourceType type, TextureAtlas atlas = null) 
            : base(id, pos)
        {
            Type = type;
            IsBeingHarvested = false;
            CanRespawn = false;
            textureAtlas = atlas;
            
            // ✅ Collision setup - resources block by default
            BlocksMovement = true;
            IsPushable = false;
            Layer = CollisionLayer.Resource;
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive)
            {
                if (CanRespawn)
                {
                    respawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (respawnTimer >= RespawnTime)
                    {
                        Respawn();
                    }
                }
                return;
            }
            
            AnimatedSprite?.Update(gameTime);
        }

        public virtual void Harvest(float damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                OnDepleted();
            }
        }

        /// <summary>
        /// ✅ FIXED: Clear BlocksMovement when depleted
        /// </summary>
        public virtual void OnDepleted()
        {
            IsActive = false;
            IsBeingHarvested = false;
            Harvester = null;
            
            // ✅ CRITICAL: Don't block movement when destroyed
            BlocksMovement = false;
            
            Console.WriteLine($"[Resource] {Type} #{ID} depleted, BlocksMovement = {BlocksMovement}");
            
            if (CanRespawn)
            {
                respawnTimer = 0f;
            }
        }

        /// <summary>
        /// ✅ FIXED: Restore BlocksMovement when respawned
        /// </summary>
        protected virtual void Respawn()
        {
            Health = MaxHealth;
            IsActive = true;
            IsBeingHarvested = false;
            Harvester = null;
            respawnTimer = 0f;
            
            // ✅ CRITICAL: Block movement again when respawned
            BlocksMovement = true;
            
            Console.WriteLine($"[Resource] {Type} #{ID} respawned, BlocksMovement = {BlocksMovement}");
        }

        public override void Interact(Entity interactor)
        {
            if (!IsBeingHarvested && IsActive)
            {
                IsBeingHarvested = true;
                Harvester = interactor;
            }
        }

        public void StopHarvest()
        {
            IsBeingHarvested = false;
            Harvester = null;
        }
        
        Vector2 IPosition.Position => Position;
    }
}