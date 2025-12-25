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

            BlocksMovement = true;
            IsPushable = true;
            Layer = CollisionLayer.Resource;

            if (atlas != null)
            {
                Sprite = atlas.CreateSprite("Sprite-0001 1");  // Tree: 16x32
                SpriteRoot = atlas.CreateSprite("Sprite-0001 0");  // Stump: 16x16
                
                // ✅ TOP-LEFT ORIGIN CONVENTION
                Sprite._origin = Vector2.Zero;
                Sprite._scale = Scale;
                
                SpriteRoot._origin = Vector2.Zero;
                SpriteRoot._scale = Scale;
            }
            
            // Calculate scaled dimensions
         
            Collider = new Rectangle(
                (int)(Sprite.Width  * 0.25f),                              // No X offset (aligned with left edge)
                (int)(Sprite.Height * 0.5f),     // Bottom 50% starts here
                (int)(Sprite.Width  * 0.5f),               // Full width
                (int)(Sprite.Height * 0.75f)      // Bottom 50% height
            );
            
            Console.WriteLine($"[Tree] ID={id}, Pos=({pos.X:F0},{pos.Y:F0}), Type={type}");
            Console.WriteLine($"       Sprite size: {Sprite.Width}x{Sprite.Height}, scaled: {Sprite.Width * Scale.X}x{Sprite.Height * Scale.Y}");
            Console.WriteLine($"       Sprite Origin: ({Sprite._origin.X}, {Sprite._origin.Y})");
            Console.WriteLine($"       Collider OFFSET: ({Collider.X},{Collider.Y}) size {Collider.Width}x{Collider.Height}");
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
            
            Console.WriteLine($"[Tree] #{ID} took {damage} damage ({Health:F0}/{MaxHealth:F0} HP)");
            
            if (Health <= 0)
            {
                Health = 0;
                OnTreeDestroyed(attacker);
            }
        }
        
        private void OnTreeDestroyed(Entity attacker)
        {

            BlocksMovement = false;  // ✅ PHẢI CÓ dòng này
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
                Console.WriteLine($"[Tree] #{ID} destroyed! Dropped {woodAmount} wood");
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
            
            // #if DEBUG
            // DrawDebugInfo(spriteBatch);
            // #endif
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
            // Position is top-left, foot is at bottom
            return Position.Y + (Sprite != null ? Collider.Height * Scale.Y : 96f);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            float healthPercent = Health / MaxHealth; 
            int barWidth = 50;
            int barHeight = 6;
            
            // Bar above tree (top-left origin)
            int barX = (int)Position.X + (int)(Sprite.Width * Scale.X / 2f) - barWidth / 2;
            int barY = (int)Position.Y - 10;

            var whitePixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            
            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(whitePixel, bgRect, Color.DarkGray * 0.8f);

            Color healthColor = Color.Lerp(Color.Red, Color.Green, healthPercent);
            var fgRect = new Rectangle(barX, barY, (int)(barWidth * healthPercent), barHeight);
            spriteBatch.Draw(whitePixel, fgRect, healthColor);
            
            // DrawRectOutline(spriteBatch, whitePixel, bgRect, Color.Black, 1);
            
            whitePixel.Dispose();
        }


        // private void DrawRectOutline(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color, int thickness)
        // {
        //     sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        //     sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        //     sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        //     sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        // }

        // #if DEBUG
        // private void DrawDebugInfo(SpriteBatch spriteBatch)
        // {
        //     var whitePixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
        //     whitePixel.SetData(new[] { Color.White });

        //     // Draw position point (yellow)
        //     var posRect = new Rectangle((int)Position.X - 3, (int)Position.Y - 3, 6, 6);
        //     spriteBatch.Draw(whitePixel, posRect, Color.Yellow);

        //     // Draw collider (cyan)
        //     Rectangle worldCollider = new Rectangle(
        //         (int)Position.X + Collider.X,
        //         (int)Position.Y + Collider.Y,
        //         Collider.Width,
        //         Collider.Height
        //     );

        //     int thickness = 2;
        //     spriteBatch.Draw(whitePixel, new Rectangle(worldCollider.X, worldCollider.Y, worldCollider.Width, thickness), Color.Cyan);
        //     spriteBatch.Draw(whitePixel, new Rectangle(worldCollider.X, worldCollider.Bottom - thickness, worldCollider.Width, thickness), Color.Cyan);
        //     spriteBatch.Draw(whitePixel, new Rectangle(worldCollider.X, worldCollider.Y, thickness, worldCollider.Height), Color.Cyan);
        //     spriteBatch.Draw(whitePixel, new Rectangle(worldCollider.Right - thickness, worldCollider.Y, thickness, worldCollider.Height), Color.Cyan);

        //     whitePixel.Dispose();
        // }
        // #endif

        Vector2 IPosition.Position => Position;
    }
}