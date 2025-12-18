using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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
        
        // Sprites
        private Sprite spriteWithBerries;    // Sprite khi có quả
        private Sprite spriteWithoutBerries;  // Sprite khi không có quả
        
        // Berry growth
        private bool hasBerries;              // Có quả hay không
        private float berryGrowthTimer;       // Timer để mọc quả lại
        private float berryGrowthTime = 30f;  // 30 giây để mọc quả lại

        private Vector2 Scale;

        public Bush(int id, Vector2 pos, Vector2 scale, BushType bushType = BushType.Berry, TextureAtlas atlas = null)
            : base(id, pos, ResourceType.Bush, atlas)
        {
            BushType = bushType;
            // Load sprites from atlas
            if (atlas != null)
            {
                spriteWithBerries = atlas.CreateSprite("Sprite-0001 2");     // Bush with berries
                spriteWithoutBerries = atlas.CreateSprite("Sprite-0001 4");  // Bush without berries
            }

            // Setup based on bush type
            switch (bushType)
            {
                case BushType.Berry:
                    MaxHealth = 1f;
                    YieldAmount = 3;
                    YieldItem = "berry";
                    RespawnTime = 60f;
                    CanRespawn = true;
                    berryGrowthTime = 30f;  // 30 seconds to regrow berries
                    break;
                    
                case BushType.NonBerry:
                    MaxHealth = 1f;
                    YieldAmount = 0;
                    YieldItem = "";
                    CanRespawn = false;
                    break;
            }

            Health = MaxHealth;
            hasBerries = true;           // Start with berries
            berryGrowthTimer = 0f;
            
            // Set initial sprite
            Sprite = hasBerries ? spriteWithBerries : spriteWithoutBerries;
            Sprite._scale = scale;
            
            // Setup collider at world coordinates
            if (Sprite != null)
            {
                int width = (int)Sprite.Width;
                int height = (int)Sprite.Height / 2;
                
                Collider = new Rectangle(
                    (int)pos.X,
                    (int)pos.Y + height,
                    width,
                    height
                );
            }
            else
            {
                Collider = new Rectangle((int)pos.X, (int)pos.Y, 32, 16);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (!IsActive)
                return;
            
            // Berry regrowth logic
            if (!hasBerries && BushType == BushType.Berry)
            {
                berryGrowthTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                if (berryGrowthTimer >= berryGrowthTime)
                {
                    // Berries have regrown
                    hasBerries = true;
                    berryGrowthTimer = 0f;
                    Sprite = spriteWithBerries;
                    Health = MaxHealth;  // Reset health
                    
                    //GameLogger.Instance?.Debug("Bush", $"Bush #{ID} berries regrown");
                }
            }
        }

        public override void Harvest(float damage)
        {
            if (!hasBerries)
            {
                // Can't harvest if no berries
                return;
            }
            
            // Harvest berries
            hasBerries = false;
            berryGrowthTimer = 0f;
            Health = 0;
            Sprite = spriteWithoutBerries;
            Sprite._scale = Scale;
            OnDepleted();
        }

        public override void OnDepleted()
        {
            // Don't set IsActive to false - bush stays active
            // Just switch to empty sprite
            IsBeingHarvested = false;
            Harvester = null;
            BlocksPath = false;
            
            // Start berry regrowth timer
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
            
            // Draw current sprite (with or without berries)
            if (Sprite != null)
            {
                Sprite.Draw(spriteBatch, Position);
            }
            
            // Optional: Draw growth progress indicator
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

            // Create white pixel texture
            var whitePixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });

            // Background
            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(whitePixel, bgRect, Color.Gray * 0.5f);

            // Progress
            var progressRect = new Rectangle(barX, barY, (int)(barWidth * progress), barHeight);
            spriteBatch.Draw(whitePixel, progressRect, Color.LightGreen);
        }
        #endif

        // ==================== UTILITY ====================

        /// <summary>
        /// Check if bush has berries available to harvest
        /// </summary>
        public bool HasBerries()
        {
            return hasBerries;
        }

        /// <summary>
        /// Get berry growth progress (0-1)
        /// </summary>
        public float GetGrowthProgress()
        {
            if (hasBerries)
                return 1f;
            
            return berryGrowthTimer / berryGrowthTime;
        }

        /// <summary>
        /// Force berries to regrow (for testing/debug)
        /// </summary>
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