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

        private Vector2 Scale;

        public Bush(int id, Vector2 pos, Vector2 scale, BushType bushType = BushType.Berry, TextureAtlas atlas = null)
            : base(id, pos, ResourceType.Bush, atlas)
        {
            BushType = bushType;
            Scale = scale;
            
            if (atlas != null)
            {
                spriteWithBerries = atlas.CreateSprite("Sprite-0001 2");
                spriteWithoutBerries = atlas.CreateSprite("Sprite-0001 4");
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
            Sprite._scale = scale;
            
            // ✅ CRITICAL FIX: Store OFFSET, not absolute position
            float baseTileSize = 16f;
            float scaledTileSize = baseTileSize * scale.X;
            
            // Bush collider: centered horizontally, bottom half of sprite
            float colliderOffsetX = 0f;                      // Centered (sprite draws from top-left)
            float colliderOffsetY = scaledTileSize * 0.5f;  // Bottom half
            float colliderWidth = scaledTileSize;            // Full width
            float colliderHeight = scaledTileSize * 0.5f;   // Half height
            
            Collider = new Rectangle(
                (int)colliderOffsetX,
                (int)colliderOffsetY,
                (int)colliderWidth,
                (int)colliderHeight
            );
            
            Console.WriteLine($"[Bush] ID={id}, Pos=({pos.X:F1},{pos.Y:F1})");
            Console.WriteLine($"       Collider OFFSET: ({Collider.X},{Collider.Y}) size {Collider.Width}x{Collider.Height}");
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
            Sprite._scale = Scale;
            OnDepleted();
        }

        public override void OnDepleted()
        {
            IsBeingHarvested = false;
            Harvester = null;
            BlocksPath = false;
            berryGrowthTimer = 0f;
        }

        protected override void Respawn()
        {
            base.Respawn();
            hasBerries = true;
            berryGrowthTimer = 0f;
            Sprite = spriteWithBerries;
            Sprite._scale = Scale;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive)
                return;
            
            if (Sprite != null)
            {
                Sprite.Draw(spriteBatch, Position);
            }
            
            #if DEBUG
            if (!hasBerries && berryGrowthTime > 0)
            {
                DrawGrowthProgress(spriteBatch);
            }
            #endif
        }

        #if DEBUG
        private void DrawGrowthProgress(SpriteBatch spriteBatch)
        {
            float progress = berryGrowthTimer / berryGrowthTime;
            int barWidth = 30;
            int barHeight = 3;
            int barX = (int)Position.X - barWidth / 2;
            int barY = (int)Position.Y - 10;

            var whitePixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });

            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(whitePixel, bgRect, Color.Gray * 0.5f);

            var progressRect = new Rectangle(barX, barY, (int)(barWidth * progress), barHeight);
            spriteBatch.Draw(whitePixel, progressRect, Color.LightGreen);
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
            Sprite._scale = Scale;
            Health = MaxHealth;
        }
    }
}