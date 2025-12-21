using System;
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
        Pine,
        Birch
    }

    public class Tree : ResourceEntity, IPosition
    {   
        public TreeType TreeType { get; private set; }
        public Sprite SpriteRoot { get; set; }
        private Vector2 Scale;

        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        
        private float damageFlashTimer = 0f;
        private const float DAMAGE_FLASH_DURATION = 0.1f;
        
        public Tree(int id, Vector2 pos, Vector2 scale, TreeType type = TreeType.Oak, TextureAtlas atlas = null) 
            : base(id, pos, ResourceType.Tree, atlas)
        {
            TreeType = type;
            Scale = scale;
            
            InitializeHealth();

            if (atlas != null)
            {
                Sprite = atlas.CreateSprite("Sprite-0001 1");  // Tree: 16x32
                Sprite._scale = Scale;
                SpriteRoot = atlas.CreateSprite("Sprite-0001 0");  // Stump: 16x16
                SpriteRoot._scale = Scale;
            }

            // ✅ CRITICAL FIX: Collider stores OFFSET, not absolute position!
            // Tree sprite: 16x32 (1 tile wide, 2 tiles tall)
            float baseTileSize = 16f;
            float scaledTileWidth = baseTileSize * scale.X;
            float scaledTileHeight = baseTileSize * scale.Y;
            
            // ✅ Collider OFFSET from entity position
            // For tree: collider is at bottom-center of the 2-tile sprite
            float colliderOffsetX = scaledTileWidth;   // 30% from left = centered
            float colliderOffsetY = scaledTileHeight * 1.5f;  // Bottom tile (after 1.5 tiles down)
            
            float colliderWidth = scaledTileWidth * 0.4f;     // 40% width
            float colliderHeight = scaledTileHeight * 0.5f;   // 50% height of bottom tile
            
            // ✅ CRITICAL: Store as OFFSET, not absolute position
            Collider = new Rectangle(
                (int)colliderOffsetX,
                (int)colliderOffsetY,
                (int)colliderWidth,
                (int)colliderHeight
            );
            
            Console.WriteLine($"[Tree] ID={id}, Pos=({pos.X:F0},{pos.Y:F0})");
            Console.WriteLine($"       Collider OFFSET: ({Collider.X},{Collider.Y}) size {Collider.Width}x{Collider.Height}");
            Console.WriteLine($"       World collider will be at: ({pos.X + Collider.X:F0},{pos.Y + Collider.Y:F0})");
        }
        
        private void InitializeHealth()
        {
            MaxHealth = TreeType switch
            {
                TreeType.Oak => 40f,
                TreeType.Pine => 35f,
                TreeType.Birch => 30f,
                _ => 30f
            };
            Health = MaxHealth;
        }
        
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (damageFlashTimer > 0)
            {
                damageFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
        
        public void TakeDamage(float damage, Entity attacker)
        {
            if (!IsActive || Health <= 0) return;
            
            Health -= damage;
            damageFlashTimer = DAMAGE_FLASH_DURATION;
            
            Console.WriteLine($"[Tree] Took {damage} damage ({Health:F0}/{MaxHealth:F0} HP)");
            
            if (Health <= 0)
            {
                Health = 0;
                OnTreeDestroyed(attacker);
            }
        }
        
        private void OnTreeDestroyed(Entity attacker)
        {
            IsActive = false;
            
            if (attacker is Player.PlayerCharacter player)
            {
                int woodAmount = TreeType switch
                {
                    TreeType.Oak => 5,
                    TreeType.Pine => 4,
                    TreeType.Birch => 3,
                    _ => 3
                };
                
                player.Inventory.AddItem("wood", woodAmount);
                Console.WriteLine($"[Tree] Destroyed! Dropped {woodAmount} wood");
            }
            
            if (CanRespawn)
            {
                StartRespawn();
            }
        }
        
        private void StartRespawn()
        {
            // TODO: Implement respawn timer
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive && Health <= 0)
            {
                if (CanRespawn && SpriteRoot != null)
                {
                    DrawStump(spriteBatch);
                }
                return;
            }
            
            if (Sprite != null && IsActive)
            {
                Color tint = damageFlashTimer > 0 ? Color.Red : Color.White;
                Sprite._color = tint;
                Sprite.Draw(spriteBatch, Position);
            }
            
            if (Health < MaxHealth)
            {
                DrawHealthBar(spriteBatch);
            }
        }

        private void DrawStump(SpriteBatch spriteBatch)
        {
            if (SpriteRoot != null)
            {
                SpriteRoot.Draw(spriteBatch, Position);
            }
        }
        
        public override float GetFootY()
        {
            return Position.Y + (Sprite?.Height ?? 48) * 0.6f;
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            float healthPercent = Health / MaxHealth; 
            int barWidth = 50;
            int barHeight = 6;
            int barX = (int)Position.X - barWidth / 2;
            int barY = (int)Position.Y - 30;

            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            
            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(whitePixel, bgRect, Color.DarkGray * 0.8f);

            Color healthColor = Color.Lerp(Color.Red, Color.Green, healthPercent);
            var fgRect = new Rectangle(barX, barY, (int)(barWidth * healthPercent), barHeight);
            spriteBatch.Draw(whitePixel, fgRect, healthColor);
            
            DrawRectOutline(spriteBatch, whitePixel, bgRect, Color.Black, 1);
        }
        
        private void DrawRectOutline(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color, int thickness)
        {
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
        
        Vector2 IPosition.Position => Position;
    }
}