using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity
{
    public abstract class Entity : IPosition
    {
        // Properties
        public readonly int ID;
        public Vector2 Position { get; set; }
        public Sprite Sprite { get; set; }
        public AnimatedSprite AnimatedSprite { get; set; }
        public Rectangle Collider { get; set; }
        public bool IsActive { get; set; }
        public virtual float Depth => (Position.Y + Collider.Height) / 20000f;
        public Vector2 Scale { get; set; }
        
        public string Name { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public bool BlocksPath { get; internal set; }

        // ✅ Constructor
        public Entity(int id, Vector2 pos)
        {
            ID = id;
            Position = pos;
            IsActive = true;
            Velocity = Vector2.Zero;
            Rotation = 0f;
            Scale = Vector2.One; // ✅ FIXED: Default scale
        }

        
        // Methods
        public abstract void Update(GameTime gameTime);
        
        public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime = null)
        {
            if (!IsActive) return;
            
            if (AnimatedSprite != null)
            {
                AnimatedSprite.Draw(spriteBatch, Position);
                return;
            }
            Sprite.Draw(spriteBatch, Position);
        
        }
        
        /// <summary>
        /// ✅ Check collision with another entity
        /// </summary>
        public virtual bool CollidesWith(Entity other)
        {
            if (!IsActive || !other.IsActive) return false;
            
            Rectangle thisCollider = GetWorldCollider();
            Rectangle otherCollider = other.GetWorldCollider();
            
            return thisCollider.Intersects(otherCollider);
        }
            
            /// ✅ FIXED: Calculate world-space collider with proper offset
        /// </summary>
        public Rectangle GetWorldCollider()
        {
            // Default scale if not set
            Vector2 effectiveScale = Scale != Vector2.Zero ? Scale : Vector2.One;
            
            return new Rectangle(
                (int)(Position.X + Collider.X * effectiveScale.X),  // ✅ Add offset
                (int)(Position.Y + Collider.Y * effectiveScale.Y),  // ✅ Add offset
                (int)(Collider.Width * effectiveScale.X),
                (int)(Collider.Height * effectiveScale.Y)
            );
        }



        public virtual float GetFootY()
        {
            Vector2 effectiveScale = Scale != Vector2.Zero ? Scale : Vector2.One;
            return Position.Y + Collider.Y * effectiveScale.Y + Collider.Height * effectiveScale.Y;
        }
        
        public virtual void Interact(Entity interactor) { }
        
        public virtual void Destroy()
        {
            IsActive = false;
        }
    }
}