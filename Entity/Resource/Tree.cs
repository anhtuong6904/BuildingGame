using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity.Resource
{
    public enum TreeType
    {
        Oak,
        Other,
    }


    public class Tree : ResourceEntity, IPosition
    {   
        public TreeType treeType { get; private set; }
        public Sprite spriteRoot { get; set; }
        private Vector2 Scale;
        
        public Tree(int id, Vector2 pos, Vector2 scale, TreeType type = TreeType.Oak, TextureAtlas atlas = null) 
            : base(id, pos, ResourceType.Tree, atlas)
        {
            treeType = type;
            Scale = scale;

            // Load sprites from atlas if available
            if (atlas != null)
            {
                Sprite = atlas.CreateSprite("Sprite-0001 1");   
                Sprite._scale = Scale;     // Tree sprite
                spriteRoot = atlas.CreateSprite("Sprite-0001 0");    // Stump sprite
                spriteRoot._scale = Scale;
            }

            switch (treeType) 
            {
                case TreeType.Oak:      
                    MaxHealth = 100f;
                    YieldAmount = 5;
                    YieldItem = "wood";
                    RespawnTime = 120f;
                    CanRespawn = false;  // Trees don't respawn by default
                    break;
            }
            
            Health = MaxHealth;
            
            // Setup collider at world coordinates
            if (Sprite != null)
            {
                int offsetX = (int)Sprite.Width / 3;
                int offsetY = (int)Sprite.Height * 2 / 3;
                int width = (int)Sprite.Width / 3;
                int height = (int)Sprite.Height / 3;
                
                Collider = new Rectangle(
                    (int)pos.X + offsetX,
                    (int)pos.Y + offsetY,
                    width,
                    height
                );
            }
            else
            {
                Collider = new Rectangle((int)pos.X, (int)pos.Y, 32, 48);
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Don't draw if completely destroyed
            if (!IsActive && Health <= 0)
            {
                if (CanRespawn && spriteRoot != null)
                {
                    DrawStump(spriteBatch);
                }
                return;
            }
            
            // Draw tree sprite
            if (Sprite != null && IsActive)
            {
                Sprite.Draw(spriteBatch, Position);
            }
            
            // Draw health bar if being harvested
            if (IsBeingHarvested && Health < MaxHealth)
            {
                DrawHealthBar(spriteBatch);
            }
        }

        private void DrawStump(SpriteBatch spriteBatch)
        {
            if (spriteRoot != null)
            {
                spriteRoot.Draw(spriteBatch, Position);
            }
        }
        public override float GetFootY()
        {
            // thân cây ở nửa dưới sprite
            return Position.Y + Sprite.Height * 0.6f;
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            float healthPercent = Health / MaxHealth; 
            int barWidth = 40;
            int barHeight = 4;
            int barX = (int)Position.X - barWidth / 2;
            int barY = (int)Position.Y - 20;

            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            
            // Background (red)
            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(whitePixel, bgRect, Color.Red);

            // Foreground (green)
            var fgRect = new Rectangle(barX, barY, (int)(barWidth * healthPercent), barHeight);
            spriteBatch.Draw(whitePixel, fgRect, Color.Green);
        }
        
    }
    
}