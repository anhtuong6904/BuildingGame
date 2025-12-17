using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Behavior;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.PathFinding;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity.NPC.Animals
{
    /// <summary>
    /// Các loại động vật
    /// </summary>
    public enum AnimalType
    {
        Chicken,
        Sheep,
        Boar
    }

    /// <summary>
    /// Trạng thái của động vật
    /// </summary>
    public enum AnimalState
    {
        Idle,
        Wandering,  // Lang thang
        Fleeing,    // Bỏ chạy
        Attacking   // Tấn công
    }

    /// <summary>
    /// Base class cho tất cả động vật
    /// </summary>
    public abstract class AnimalEntity : Entity, IPosition
    {
        // Trạng thái và thông tin động vật
        public AnimalType Type { get; protected set; }
        public AnimalState State { get; set; }

        // Chỉ số của động vật
        public float Health { get; set; }
        public float MaxHealth { get; protected set; }
        public float Speed { get; protected set; }
        public bool IsAggressive { get; protected set; }

        // Pathfinding
        public PathFollower PathFollower { get; protected set; }

        // Movement
        protected Vector2 SpawnPosition;
        protected float WanderRadius = 200f;

        // Behavior
        protected BehaviorTree behaviorTree;
        protected BehaviorContext context;

        // Phát hiện vật thể khác trong lãnh thổ
        public Entity ThreatTarget { get; set; }
        protected float detectionRange = 150f;
        protected float fleeDistance = 200f;

        // Vật phẩm thu hoạch được từ động vật
        public string[] LootItems { get; protected set; }
        public int LootAmount { get; protected set; }

        // Hướng di chuyển của động vật
        public Direction Direction { get; protected set; }

        // Texture Atlas
        public TextureAtlas Atlas { get; protected set; }

        // Constructor
        public AnimalEntity(int id, Vector2 pos, AnimalType type, TextureAtlas atlas) 
            : base(id, pos)
        {
            Type = type;
            State = AnimalState.Idle;
            SpawnPosition = pos;
            Direction = Direction.Font;
            Velocity = Vector2.Zero;
            Atlas = atlas;
            
            PathFollower = new PathFollower(10f);
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            // Update pathfinding movement
            UpdatePathfindingMovement(gameTime);

            // Execute behavior tree
            if (behaviorTree != null)
            {
                context = new BehaviorContext(this, gameTime);
                behaviorTree.Tick(context);
            }

            // Update animation
            AnimatedSprite?.Update(gameTime);
        }

        protected virtual void UpdatePathfindingMovement(GameTime gameTime)
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
                Velocity = direction.Value * Speed;
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

        protected void UpdateDirection(Vector2 movementDir)
        {
            if (Math.Abs(movementDir.X) > Math.Abs(movementDir.Y))
            {
                Direction = movementDir.X > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                Direction = movementDir.Y > 0 ? Direction.Font : Direction.Back;
            }
        }

        /// <summary>
        /// Move to target using pathfinding
        /// </summary>
        public void MoveToWithPathfinding(Vector2 target)
        {
            var pathfinder = GameManager.Instance?.World?.Pathfinder;
            if (pathfinder != null)
            {
                var path = pathfinder.FindPath(Position, target);
                if (path != null && path.Count > 0)
                {
                    PathFollower.SetPath(path);
                }
            }
        }

        /// <summary>
        /// Simple move (fallback without pathfinding)
        /// </summary>
        public void MoveTo(Vector2 target)
        {
            PathFollower.SetPath(new System.Collections.Generic.List<Vector2> { target });
        }

        public void Stop()
        {
            PathFollower.ClearPath();
            Velocity = Vector2.Zero;
        }

        public bool IsMoving()
        {
            return PathFollower.HasPath && !PathFollower.ReachedDestination;
        }

        public virtual void TakeDamage(float damage, Entity attacker)
        {
            Health -= damage;

            if (Health <= 0)
            {
                Die();
            }
            else
            {
                ThreatTarget = attacker;
            }
        }

        protected virtual void Die()
        {
            IsActive = false;
            
            // TODO: Spawn loot
            //GameLogger.Instance?.GameEvent("Animal", $"{Type} died at ({Position.X:F0}, {Position.Y:F0})");
        }

        /// <summary>
        /// Get animation name based on state and direction
        /// </summary>
        protected virtual string GetAnimationName()
        {
            string state = State == AnimalState.Wandering ? "walk" : "idle";
            string direction = Direction.ToString().ToLower();
            return $"{state}-{direction}";
        }

        /// <summary>
        /// Get animated sprite from atlas
        /// </summary>
        protected virtual AnimatedSprite GetAnimatedSprite(string name)
        {
            if (Atlas != null)
            {
                return Atlas.CreateAnimatedSprite(name);
            }
            return null;
        }

        protected abstract void InitializeBehaviorTree();

        // IPosition implementation
        Vector2 IPosition.Position => Position;
    }
}