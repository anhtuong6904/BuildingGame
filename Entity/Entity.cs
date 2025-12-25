using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;
using TribeBuild.World;

namespace TribeBuild.Entity
{

    /// <summary>
    /// ✅ Base Entity with continuous AABB collision checking
    /// </summary>
    public abstract class Entity : IPosition
    {
        // Core Properties
        public readonly int ID;
        public float KnockBack {get; set;}
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Scale { get; set; }
        public float Rotation { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }

        // ✅ Collision Properties
        public Rectangle Collider { get; set; }
        public bool BlocksMovement { get; set; }
        public bool IsPushable { get; set; }
        public CollisionLayer Layer { get; set; }

        // Rendering
        public Sprite Sprite { get; set; }
        public AnimatedSprite AnimatedSprite { get; set; }
        public virtual float Depth => GetFootY() / 20000f;
        public Vector2 KnockbackVelocity { get; set; } = Vector2.Zero;
        private float knockbackDecay = 8f; // Fade out speed

        protected Entity(int id, Vector2 pos)
        {
            ID = id;
            Position = pos;
            Velocity = Vector2.Zero;
            Scale = Vector2.One;
            Rotation = 0f;
            IsActive = true;
            BlocksMovement = false;
            IsPushable = false;
            Layer = CollisionLayer.Default;
            Collider = Rectangle.Empty;
        }

        // ==================== ✅ AABB COLLISION METHODS ====================
        
        /// <summary>
        /// Get world-space AABB
        /// </summary>
        public Rectangle GetWorldAABB()
        {
            if (Collider == Rectangle.Empty)
                return Rectangle.Empty;
                
            return new Rectangle(
                (int)Position.X + Collider.X,
                (int)Position.Y + Collider.Y,
                Collider.Width,
                Collider.Height
            );
        }
        
        /// <summary>
        /// ✅ Check collision with another entity
        /// </summary>
        public bool CollidesWithAABB(Entity other)
        {
            if (!IsActive || !other.IsActive) return false;
            if (this == other) return false;
            
            Rectangle thisAABB = GetWorldAABB();
            Rectangle otherAABB = other.GetWorldAABB();
            
            if (thisAABB == Rectangle.Empty || otherAABB == Rectangle.Empty)
                return false;
            
            return thisAABB.Intersects(otherAABB);
        }
        
        /// <summary>
        /// ✅ Check if moving to position would cause collision
        /// </summary>
        public bool WouldCollideAt(Vector2 newPosition, Entity other)
        {
            if (!IsActive || !other.IsActive) return false;
            if (this == other) return false;
            
            Rectangle futureAABB = new Rectangle(
                (int)newPosition.X + Collider.X,
                (int)newPosition.Y + Collider.Y,
                Collider.Width,
                Collider.Height
            );
            
            Rectangle otherAABB = other.GetWorldAABB();
            
            if (futureAABB == Rectangle.Empty || otherAABB == Rectangle.Empty)
                return false;
            
            return futureAABB.Intersects(otherAABB);
        }
        
        /// <summary>
        /// ✅ NEW: Check collision with tilemap at position
        /// </summary>
        protected bool IsTileWalkableAt(Vector2 pos, Tilemap tilemap)
        {
            if (tilemap == null || Collider == Rectangle.Empty) 
                return true;

            Rectangle futureAABB = new Rectangle(
                (int)pos.X + Collider.X,
                (int)pos.Y + Collider.Y,
                Collider.Width,
                Collider.Height
            );

            // Check 5 points: 4 corners + center
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(futureAABB.Left, futureAABB.Top),
                new Vector2(futureAABB.Right - 1, futureAABB.Top),
                new Vector2(futureAABB.Left, futureAABB.Bottom - 1),
                new Vector2(futureAABB.Right - 1, futureAABB.Bottom - 1),
                new Vector2(futureAABB.Center.X, futureAABB.Center.Y)
            };

            foreach (var point in checkPoints)
            {
                Point tile = tilemap.WorldToTile(point);
                
                if (tile.X < 0 || tile.X >= tilemap.Width ||
                    tile.Y < 0 || tile.Y >= tilemap.Height)
                    return false;
                
                if (!tilemap.IsTileWalkable(tile.X, tile.Y))
                    return false;
            }
            
            return true;
        }

       /// <summary>
        /// ✅ SEPARATION: Push overlapping entities apart
        /// Call this AFTER movement to prevent overlap
        /// </summary>
        public void ResolveOverlaps(GameWorld world, float pushStrength = 1f)
        {
            if (world == null || Collider == Rectangle.Empty)
                return;

            var nearby = world.GetEntitiesInRadius(Position, 100f);

            foreach (var other in nearby)
            {
                if (other == this || !other.IsActive)
                    continue;

                if (!BlocksMovement || !other.BlocksMovement)
                    continue;

                if (!Layer.CanCollideWith(other.Layer))
                    continue;

                if (!CollidesWithAABB(other))
                    continue;

                // ✅ CALCULATE SEPARATION VECTOR
                Vector2 thisCenter = GetAABBCenter();
                Vector2 otherCenter = other.GetAABBCenter();

                Vector2 separationDir = thisCenter - otherCenter;
                float distance = separationDir.Length();

                if (distance < 0.1f)
                {
                    // Random direction if exactly overlapping
                    Random rng = new Random();
                    float angle = (float)(rng.NextDouble() * Math.PI * 2);
                    separationDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    distance = 1f;
                }
                else
                {
                    separationDir.Normalize();
                }

                // ✅ CALCULATE OVERLAP AMOUNT
                Rectangle thisAABB = GetWorldAABB();
                Rectangle otherAABB = other.GetWorldAABB();
                Rectangle intersection = Rectangle.Intersect(thisAABB, otherAABB);

                float overlapAmount = Math.Max(intersection.Width, intersection.Height);

                // ✅ PUSH BASED ON PUSHABILITY
                if (IsPushable && other.IsPushable)
                {
                    // Both pushable → split 50/50
                    float halfPush = overlapAmount * 0.5f * pushStrength;
                    Position += separationDir * halfPush;
                    other.Position -= separationDir * halfPush;
                }
                else if (IsPushable && !other.IsPushable)
                {
                    // Only this is pushable
                    Position += separationDir * overlapAmount * pushStrength;
                }
                else if (!IsPushable && other.IsPushable)
                {
                    // Only other is pushable
                    other.Position -= separationDir * overlapAmount * pushStrength;
                }
                // else: both not pushable → no separation (shouldn't happen if BlocksMovement)
            }
        }

        /// <summary>
        /// ✅ Apply knockback force to entity
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (direction == Vector2.Zero) return;
            
            direction.Normalize();
            KnockbackVelocity = direction * force;
            
            Console.WriteLine($"[{Name}] Knockback applied: {force:F1}");
        }

        /// <summary>
        /// ✅ Update knockback (call in Update)
        /// </summary>
        protected void UpdateKnockback(float deltaTime)
        {
            if (KnockbackVelocity.LengthSquared() < 0.1f)
            {
                KnockbackVelocity = Vector2.Zero;
                return;
            }

            // Apply knockback movement with collision
            var world = GameManager.Instance?.World;
            if (world != null)
            {
                Vector2 desiredPos = Position + KnockbackVelocity * deltaTime;
                Position = MoveWithCollision(desiredPos, deltaTime, world);
            }
            else
            {
                Position += KnockbackVelocity * deltaTime;
            }

            // Decay knockback over time
            KnockbackVelocity -= KnockbackVelocity * knockbackDecay * deltaTime;
        }
        
        
        /// <summary>
        /// ✅ NEW: Continuous collision check with sliding
        /// Call this in Update() for any moving entity
        /// </summary>
        protected Vector2 MoveWithCollision(
            Vector2 desiredPosition,
            float deltaTime,
            GameWorld world)
        {
            if (world == null || Collider == Rectangle.Empty)
                return desiredPosition;

            Vector2 pos = Position;

            // =====================
            // 1️⃣ MOVE X AXIS
            // =====================
            Vector2 targetX = new Vector2(desiredPosition.X, pos.Y);

            if (IsTileWalkableAt(targetX, world.Tilemap))
            {
                bool blockedX = false;

                foreach (var e in world.GetEntitiesInRadius(targetX, 100f))
                {
                    if (e == this || !e.IsActive || !e.BlocksMovement)
                        continue;

                    if (!Layer.CanCollideWith(e.Layer))
                        continue;

                    if (WouldCollideAt(targetX, e))
                    {
                        blockedX = true;
                        break;
                    }
                }

                if (!blockedX)
                    pos.X = targetX.X;
            }

            // =====================
            // 2️⃣ MOVE Y AXIS
            // =====================
            Vector2 targetY = new Vector2(pos.X, desiredPosition.Y);

            if (IsTileWalkableAt(targetY, world.Tilemap))
            {
                bool blockedY = false;

                foreach (var e in world.GetEntitiesInRadius(targetY, 100f))
                {
                    if (e == this || !e.IsActive || !e.BlocksMovement)
                        continue;

                    if (!Layer.CanCollideWith(e.Layer))
                        continue;

                    if (WouldCollideAt(targetY, e))
                    {
                        blockedY = true;
                        break;
                    }
                }

                if (!blockedY)
                    pos.Y = targetY.Y;
            }

            return pos;
        }
        
        /// <summary>
        /// Get center point of AABB
        /// </summary>
        public Vector2 GetAABBCenter()
        {
            Rectangle aabb = GetWorldAABB();
            return new Vector2(
                aabb.X + aabb.Width / 2f,
                aabb.Y + aabb.Height / 2f
            );
        }
        
        /// <summary>
        /// Check if point is inside AABB
        /// </summary>
        public bool ContainsPoint(Vector2 point)
        {
            Rectangle aabb = GetWorldAABB();
            return aabb.Contains((int)point.X, (int)point.Y);
        }
        
        /// <summary>
        /// Get distance between two entities (from AABB centers)
        /// </summary>
        public float DistanceTo(Entity other)
        {
            return Vector2.Distance(GetAABBCenter(), other.GetAABBCenter());
        }

        // ==================== UPDATE / DRAW ====================
        
        public abstract void Update(GameTime gameTime);

        public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime = null)
        {
            if (!IsActive) return;

            if (AnimatedSprite != null)
                AnimatedSprite.Draw(spriteBatch, Position);
            else
                Sprite?.Draw(spriteBatch, Position);
                
            // #if DEBUG
            // DrawDebugAABB(spriteBatch);
            // #endif
        }

        // ==================== DEPTH SORTING ====================
        
        public virtual float GetFootY()
        {
            Rectangle aabb = GetWorldAABB();
            if (aabb == Rectangle.Empty)
                return Position.Y;
            return aabb.Bottom;
        }

        // ==================== DEBUG ====================
        
        #if DEBUG
        protected void DrawDebugAABB(SpriteBatch spriteBatch)
        {
            Rectangle aabb = GetWorldAABB();
            if (aabb == Rectangle.Empty) return;
            
            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            
            Color color = BlocksMovement ? Color.Red : Color.Lime;
            color *= 0.5f;
            
            int thickness = 2;
            
            // Box outline
            spriteBatch.Draw(pixel, 
                new Rectangle(aabb.X, aabb.Y, aabb.Width, thickness), color);
            spriteBatch.Draw(pixel, 
                new Rectangle(aabb.X, aabb.Bottom - thickness, aabb.Width, thickness), color);
            spriteBatch.Draw(pixel, 
                new Rectangle(aabb.X, aabb.Y, thickness, aabb.Height), color);
            spriteBatch.Draw(pixel, 
                new Rectangle(aabb.Right - thickness, aabb.Y, thickness, aabb.Height), color);
            
            // Position dot
            var posRect = new Rectangle((int)Position.X - 3, (int)Position.Y - 3, 6, 6);
            spriteBatch.Draw(pixel, posRect, Color.Yellow);
            
            pixel.Dispose();
        }
        #endif

        // ==================== INTERACTION ====================
        
        public virtual void Interact(Entity interactor) { }
        
        public virtual void Destroy()
        {
            IsActive = false;
        }
        
        Vector2 IPosition.Position => Position;
    }
    
    // ==================== COLLISION LAYER EXTENSIONS ====================
    

}