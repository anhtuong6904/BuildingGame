using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;
using MonoGameLibrary.PathFinding;
namespace TribeBuild.Entity.Resource
{
    public enum ResourceType
    {
        Tree,
        Stone,
        Ore,
        Bush,
    }

    public abstract class ResourceEntity : Entity, IPosition
    {
        /// <summary>
        /// các thông tin chỉ số của tài nguyên
        /// </summary>
        public ResourceType Type { get; protected set;}

        public bool CanHarvested{get; protected set;}
        public float Health{get; set;}
        public float MaxHealth{get; protected set;}
        public int YieldAmount {get; protected set;} // Giới hạn số lượng tài nguyên được thu thập
        public string YieldItem {get; protected set;} //tên của loai tài nguyên
        
        /// <summary>
        /// trạng thái để thu hoạch 
        /// </summary>
        public bool IsBeingHarvested {get; set;}
        public Entity Harvester {get; set;}

        /// <summary>
        /// respawn tài nguyên
        /// </summary>
        public bool CanRespawn {get; protected set;}
        public float RespawnTime {get; protected set;}
        public bool BlocksPath { get; internal set; }

        private float respawnTimer;

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceEntity(int id, Vector2 Pos, ResourceType type) : base(id, Pos)
        {
            Type = type;
            IsBeingHarvested = false;
            CanRespawn = false;
            respawnTimer = 0f;
            BlocksPath = true;

        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive)
            {
                if (CanRespawn)
                {
                    respawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if(respawnTimer >= RespawnTime)
                    {
                        Respawn();
                    }
                }
                return;
            }
            AnimatedSprite?.Update(gameTime);
        }

        /// <summary>
        /// ham thu hoach
        /// </summary>
        /// <param name="damage"></param>

        public virtual void Harvest(float damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                OnDepleted();
            }
        }

        public virtual void OnDepleted()
        {
            IsActive = false;
            IsBeingHarvested = false;
            Harvester = null;
            BlocksPath = true;
            if (CanRespawn)
            {
                respawnTimer = 0f;
            }
        }

        protected virtual void Respawn()
        {
            Health = MaxHealth;
            IsActive = true;
            IsBeingHarvested = false;
            BlocksPath = true;
            Harvester = null;
            respawnTimer = 0f;
        }
        public override void Interact(Entity interactor)
        {
            if(!IsBeingHarvested && IsActive)
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
    }
}
