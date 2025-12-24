using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity.Resource
{
    public enum BushType
    {
        Berry,
        NonBerry,
    }
    
    public class Bush : ResourceEntity, IPosition
    {
        public BushType BushType { get; private set; }
        
        private Sprite spriteWithBerries;
        private Sprite spriteWithoutBerries;
        
        private bool hasBerries;
        private float berryGrowthTimer;
        private float berryGrowthTime = 30f;

        private Vector2 _scale;

        public Bush(int id, Vector2 pos, Vector2 scale, BushType bushType = BushType.Berry, TextureAtlas atlas = null)
            : base(id, pos, ResourceType.Bush, atlas)
        {
            BushType = bushType;
            _scale = scale;
            
            // ✅ Collision setup - bushes don't block movement
            BlocksMovement = true;  // Bushes don't block!
            IsPushable = false;
            Layer = CollisionLayer.Resource;
            
            if (atlas != null)
            {
                spriteWithBerries = atlas.CreateSprite("Sprite-0001 2");
                spriteWithoutBerries = atlas.CreateSprite("Sprite-0001 4");
                
                spriteWithBerries._origin = Vector2.Zero;
                spriteWithBerries._scale = scale;
                
                spriteWithoutBerries._origin = Vector2.Zero;
                spriteWithoutBerries._scale = scale;
            }

            switch (bushType)
            {
                case BushType.Berry:
                    MaxHealth = 1f;
                    YieldAmount = 3;
                    YieldItem = "berry";
                    RespawnTime = 60f;
                    CanRespawn = true;
                    berryGrowthTime = 30f;
                    break;
                    
                case BushType.NonBerry:
                    MaxHealth = 1f;
                    YieldAmount = 0;
                    YieldItem = "";
                    CanRespawn = false;
                    break;
            }

            Health = MaxHealth;
            hasBerries = true;
            berryGrowthTimer = 0f;
            
            Sprite = hasBerries ? spriteWithBerries : spriteWithoutBerries;
            
            // ✅ Calculate collider (full sprite size for interaction)
            float scaledSize = 16f * scale.X;
            
            Collider = new Rectangle(
                (int)(scaledSize *0.2f),
                (int)(scaledSize * 0.5f),
                (int)(scaledSize - 2 *scaledSize *0.2f),
                (int)(scaledSize * 0.5f)
            );
            
            Console.WriteLine($"[Bush] #{id} created at ({pos.X:F0},{pos.Y:F0})");
            Console.WriteLine($"       AABB: {Collider.Width}x{Collider.Height}, BlocksMovement: {BlocksMovement}");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (!IsActive)
                return;
            
            if (!hasBerries && BushType == BushType.Berry)
            {
                berryGrowthTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                if (berryGrowthTimer >= berryGrowthTime)
                {
                    hasBerries = true;
                    berryGrowthTimer = 0f;
                    Sprite = spriteWithBerries;
                    Health = MaxHealth;
                    Console.WriteLine($"[Bush] #{ID} berries regrown!");
                }
            }
        }

        public override void Harvest(float damage)
        {
            if (!hasBerries)
                return;
            
            hasBerries = false;
            berryGrowthTimer = 0f;
            Health = 0;
            Sprite = spriteWithoutBerries;
            OnDepleted();
            
            Console.WriteLine($"[Bush] #{ID} harvested!");
        }

        public override void OnDepleted()
        {
            IsBeingHarvested = false;
            Harvester = null;
            berryGrowthTimer = 0f;
        }

        protected override void Respawn()
        {
            base.Respawn();
            hasBerries = true;
            berryGrowthTimer = 0f;
            Sprite = spriteWithBerries;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive)
                return;
            
            if (Sprite != null)
            {
                Sprite.Draw(spriteBatch, Position);
            }
            
            // #if DEBUG
            // DrawDebugAABB(spriteBatch);
            
            // // Draw growth progress
            // if (!hasBerries && berryGrowthTime > 0)
            // {
            //     DrawGrowthBar(spriteBatch);
            // }
            // #endif
        }

        #if DEBUG
        private void DrawGrowthBar(SpriteBatch spriteBatch)
        {
            float progress = berryGrowthTimer / berryGrowthTime;
            int barWidth = 30;
            int barHeight = 3;
            int barX = (int)Position.X + Collider.Width / 2 - barWidth / 2;
            int barY = (int)Position.Y - 10;

            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(pixel, bgRect, Color.Gray * 0.5f);

            var progressRect = new Rectangle(barX, barY, (int)(barWidth * progress), barHeight);
            spriteBatch.Draw(pixel, progressRect, Color.LightGreen);
            
            pixel.Dispose();
        }
        #endif

        public bool HasBerries() => hasBerries;
        
        public float GetGrowthProgress()
        {
            if (hasBerries)
                return 1f;
            return berryGrowthTimer / berryGrowthTime;
        }

        public void ForceRegrowBerries()
        {
            hasBerries = true;
            berryGrowthTimer = 0f;
            Sprite = spriteWithBerries;
            Health = MaxHealth;
            Console.WriteLine($"[Bush] #{ID} force regrow berries");
        }
        
        Vector2 IPosition.Position => Position;
    }
}