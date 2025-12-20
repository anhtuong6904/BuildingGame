using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace TribeBuild.Entity
{
    public abstract class Entity
    {
        // Properties
        public readonly int ID;
        public Vector2 Position { get; set; }
        public Sprite Sprite { get; set; }
        public AnimatedSprite AnimatedSprite { get; set; }
        public Rectangle Collider { get; set; }
        public bool IsActive { get; set; }
        public virtual float Depth => (Position.Y + Collider.Height) / 20000f;
        
        public string Name { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        
        // Constructor
        public Entity(int id, Vector2 pos)
        {
            ID = id;
            Position = pos;
            IsActive = true;
            Velocity = Vector2.Zero;
            Rotation = 0f;
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
        
        public virtual bool CollidesWith(Entity other)
        {
            if (!IsActive || !other.IsActive) return false;
            
            Rectangle thisCollider = GetWorldCollider();
            Rectangle otherCollider = other.GetWorldCollider();
            
            return thisCollider.Intersects(otherCollider);
        }
        
        public Rectangle GetWorldCollider()
        {
            return new Rectangle(
                (int)(Position.X + Collider.X),
                (int)(Position.Y + Collider.Y),
                Collider.Width,
                Collider.Height
            );
        }

        public virtual float GetFootY()
        {
            return Position.Y;
        }
        
        public virtual void Interact(Entity interactor) { }
        
        public virtual void Destroy()
        {
            IsActive = false;
        }
    }
}