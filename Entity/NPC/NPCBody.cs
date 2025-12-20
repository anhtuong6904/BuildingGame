using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.PathFinding;
using TribeBuild.Tasks;

namespace TribeBuild.Entity.NPC
{
    /// <summary>
    /// Physical representation of an NPC with movement, rendering, and combat
    /// </summary>
    public class NPCBody : Entity
    {
        // AI
        public NPC AI { get; private set; }
        
        // Visual
        public Direction Direction { get; set; }
        public TextureAtlas Atlas { get; private set; }
        public Color TintColor { get; set; }
        public float Alpha { get; set; }
        
        // Movement & Pathfinding
        public PathFollower PathFollower { get; private set; }
        public Vector2 Velocity { get; set; }
        
        // Combat
        public float Health { get; set; }
        public float MaxHealth { get; private set; }
        
        // Task tracking
        public Task CurrentTask { get; set; }

        private Direction lastDirection;
        private NPCState lastState;

        
        // World reference
        public GameWorld World { get; private set; }

        public NPCBody(int id, Vector2 pos, NPC ai, TextureAtlas atlas = null) : base(id, pos)
        {
            AI = ai;
            Atlas = atlas;
            Direction = Direction.Font;
            TintColor = Color.White;
            Alpha = 1f;
            
            MaxHealth = 100f;
            Health = MaxHealth;
            
            PathFollower = new PathFollower(10f);
            Velocity = Vector2.Zero;
            
            // Load initial animation
            if (atlas != null)
            {
                AnimatedSprite = atlas.CreateAnimatedSprite(GetAnimationName());
            }

            // Setup collider
            if (AnimatedSprite != null)
            {
                Collider = new Rectangle(
                    (int)(pos.X - AnimatedSprite._region.Width / 4),
                    (int)(pos.Y - AnimatedSprite._region.Height / 4),
                    AnimatedSprite._region.Width / 2,
                    AnimatedSprite._region.Height / 4
                );
            }
            else
            {
                Collider = new Rectangle((int)pos.X - 16, (int)pos.Y - 24, 32, 48);
            }
        }

        /// <summary>
        /// Set reference to game world (for pathfinding)
        /// </summary>
        public void SetWorld(GameWorld world)
        {
            World = world;
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            
            // Update pathfinding movement
            UpdatePathfindingMovement(gameTime);
            
            // Update AI
            AI?.Update(gameTime, this);
            
            // Update animation
            UpdateAnimationState();
            AnimatedSprite?.Update(gameTime);
            
            // Update collider position
            UpdateCollider();
        }

        private void UpdateAnimationState()
        {
            if (AI == null || Atlas == null) return;

            if (Direction == lastDirection && AI.CurrentState == lastState)
                return;

            AnimatedSprite = Atlas.CreateAnimatedSprite(GetAnimationName());
            lastDirection = Direction;
            lastState = AI.CurrentState;
        }


        private void UpdatePathfindingMovement(GameTime gameTime)
        {
            if (!PathFollower.HasPath)
            {
                Velocity = Vector2.Zero;
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Get direction from pathfollower
            Vector2? direction = PathFollower.Update(Position);

            if (direction.HasValue)
            {
                // Move towards waypoint
                float speed = AI?.Speed ?? 80f;
                Velocity = direction.Value * speed;
                Position += Velocity * deltaTime;

                // Update facing direction
                UpdateDirection(direction.Value);
            }
            else
            {
                // Reached destination
                Velocity = Vector2.Zero;
            }
        }

        private void UpdateDirection(Vector2 movementDir)
        {
            if (movementDir == Vector2.Zero)
                return;

            // Ưu tiên Up / Down cho top-down RPG
            if (Math.Abs(movementDir.Y) >= Math.Abs(movementDir.X))
            {
                Direction = movementDir.Y > 0 ? Direction.Font : Direction.Back;
            }
            else
            {
                Direction = movementDir.X > 0 ? Direction.Right : Direction.Left;
            }
        }


        private void UpdateCollider()
        {
            if (AnimatedSprite != null)
            {
                Collider = new Rectangle(
                    (int)(Position.X - AnimatedSprite._region.Width / 4),
                    (int)(Position.Y - AnimatedSprite._region.Height / 4),
                    AnimatedSprite._region.Width / 2,
                    AnimatedSprite._region.Height / 4
                );
            }
            else
            {
                Collider = new Rectangle(
                    (int)Position.X - 16,
                    (int)Position.Y - 24,
                    32, 
                    48
                );
            }
        }

        public override float GetFootY()
        {
            return Position.Y + Collider.Height;
        }


        // ==================== MOVEMENT METHODS ====================

        /// <summary>
        /// Move to target using A* pathfinding
        /// </summary>
        public void MoveToWithPathfinding(Vector2 target)
        {
            if (World == null)
            {
                //GameLogger.Instance?.Warning("NPC", $"NPC #{ID} has no World reference for pathfinding");
                MoveTo(target);
                return;
            }

            var path = World.FindPath(Position, target);
            if (path != null && path.Count > 0)
            {
                PathFollower.SetPath(path);
            }
            else
            {
                //GameLogger.Instance?.Warning("NPC", $"No path found for NPC #{ID}");
            }
        }

        /// <summary>
        /// Simple move (direct line without pathfinding)
        /// </summary>
        public void MoveTo(Vector2 target)
        {
            PathFollower.SetPath(new System.Collections.Generic.List<Vector2> { target });
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        public void Stop()
        {
            PathFollower.ClearPath();
            Velocity = Vector2.Zero;
        }

        /// <summary>
        /// Check if NPC is currently moving
        /// </summary>
        public bool IsMoving()
        {
            return PathFollower.HasPath && !PathFollower.ReachedDestination;
        }

        /// <summary>
        /// Check if NPC has reached target
        /// </summary>
        public bool HasReachedTarget()
        {
            return !PathFollower.HasPath || PathFollower.ReachedDestination;
        }

        /// <summary>
        /// Get remaining distance to target
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (!PathFollower.HasPath) 
                return 0f;
            return PathFollower.GetRemainingDistance(Position);
        }

        // ==================== COMBAT METHODS ====================

        /// <summary>
        /// Take damage from attacks
        /// </summary>
        public void TakeDamage(float damage, Entity attacker = null)
        {
            Health -= damage;
            
            //GameLogger.Instance?.LogCombat(attacker?.ID ?? -1, ID, damage);
            
            if (Health <= 0)
            {
                Die();
            }
            else
            {
                // Notify AI of attack
                if (AI is VillagerAI villager)
                {
                    villager.OnAttacked(attacker);
                }
                else if (AI is HunterAI hunter)
                {
                    hunter.OnAttacked(attacker);
                }
            }
        }

        /// <summary>
        /// Heal the NPC
        /// </summary>
        public void Heal(float amount)
        {
            Health = Math.Min(Health + amount, MaxHealth);
        }

        /// <summary>
        /// Get health percentage (0-1)
        /// </summary>
        public float GetHealthPercent()
        {
            return Health / MaxHealth;
        }

        // ==================== DEATH ====================

        protected virtual void Die()
        {
            IsActive = false;
            
            
            // Cancel current task
            if (CurrentTask != null)
            {
                TaskManager.Instance.CancelTask(CurrentTask);
                CurrentTask = null;
            }
            
            //GameLogger.Instance?.GameEvent("NPC", $"NPC #{ID} died at ({Position.X:F0}, {Position.Y:F0})");
        }

        // ==================== ANIMATION ====================

        private string GetAnimationName()
        {
            if (AI == null)
                return "idle-font";

            string state = AI.CurrentState == NPCState.Moving ? "walk" : "idle";
            string direction = Direction.ToString().ToLower();
            return $"{state}-{direction}";
        }

        // ==================== RENDERING ====================

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) 
                return;
            
            // Draw sprite
            if (AnimatedSprite != null)
            {
                AnimatedSprite._color = TintColor * Alpha;
                AnimatedSprite.Draw(spriteBatch, Position);
            }
            else
            {
                // Fallback: Draw colored rectangle
                DrawFallbackSprite(spriteBatch);
            }
            
            // Debug visualizations
            #if DEBUG
            DrawDebugInfo(spriteBatch);
            #endif
        }

        private void DrawFallbackSprite(SpriteBatch spriteBatch)
        {
            // Draw a simple colored rectangle as fallback
            var rect = new Rectangle((int)Position.X - 8, (int)Position.Y - 12, 16, 24);
            // You'll need a white pixel texture for this
            // spriteBatch.Draw(whitePixel, rect, TintColor * Alpha);
        }

        private void DrawDebugInfo(SpriteBatch spriteBatch)
        {
            // Health bar
            if (Health < MaxHealth)
            {
                DrawHealthBar(spriteBatch);
            }

            // Path visualization
            if (PathFollower.HasPath && PathFollower.CurrentPath != null)
            {
                DrawPath(spriteBatch);
            }

            // Collider
            DrawCollider(spriteBatch);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            int barWidth = 40;
            int barHeight = 4;
            Vector2 barPos = Position + new Vector2(-barWidth / 2, -50);
            
            // Background (red)
            var bgRect = new Rectangle((int)barPos.X, (int)barPos.Y, barWidth, barHeight);
            // spriteBatch.Draw(whitePixel, bgRect, Color.Red * 0.5f);
            
            // Foreground (green)
            int healthWidth = (int)(barWidth * (Health / MaxHealth));
            var fgRect = new Rectangle((int)barPos.X, (int)barPos.Y, healthWidth, barHeight);
            // spriteBatch.Draw(whitePixel, fgRect, Color.Green);
        }

        private void DrawPath(SpriteBatch spriteBatch)
        {
            // Draw waypoints
            foreach (var waypoint in PathFollower.CurrentPath)
            {
                var rect = new Rectangle((int)waypoint.X - 2, (int)waypoint.Y - 2, 4, 4);
                // spriteBatch.Draw(whitePixel, rect, Color.Yellow * 0.5f);
            }

            // Draw lines between waypoints
            // Requires a line drawing method
        }

        private void DrawCollider(SpriteBatch spriteBatch)
        {
            // Draw collider outline
            // spriteBatch.Draw(whitePixel, new Rectangle(Collider.X, Collider.Y, Collider.Width, 1), Color.Cyan * 0.5f);
            // ... draw other edges
        }

        // ==================== UTILITY ====================

        /// <summary>
        /// Get distance to another entity
        /// </summary>
        public float GetDistanceTo(Entity other)
        {
            return Vector2.Distance(Position, other.Position);
        }

        /// <summary>
        /// Check if another entity is in range
        /// </summary>
        public bool IsInRange(Entity other, float range)
        {
            return GetDistanceTo(other) <= range;
        }

        /// <summary>
        /// Get direction vector to another entity
        /// </summary>
        public Vector2 GetDirectionTo(Entity other)
        {
            Vector2 direction = other.Position - Position;
            if (direction != Vector2.Zero)
                direction.Normalize();
            return direction;
        }
    }
}