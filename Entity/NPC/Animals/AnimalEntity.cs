using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Behavior;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.PathFinding;
using MonoGameLibrary.Spatial;
using Myra.Graphics2D.UI;
using TribeBuild.Player;
using TribeBuild.World;

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
        Walk,
        Attacking   // Tấn công
    }

    /// <summary>
    /// Base class cho tất cả động vật
    /// </summary>
    public abstract class AnimalEntity : Entity, IPosition
    {
        // Trạng thái và thông tin động vật
        public AnimalType Type { get; protected set; }
        protected Vector2 Scale = Vector2.One;

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

        private AnimalState lastState;
        private Direction lastDirection;

        public Direction Direction { get; protected set; }

        // Texture Atlas
        public TextureAtlas Atlas { get; protected set; }

        private float stuckCheckTimer = 0f;
        private Vector2 lastCheckedPosition;
        private const float STUCK_CHECK_INTERVAL = 2f;  // Check every 2 seconds
        private const float STUCK_THRESHOLD = 5f; 

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
            
            // ✅ INITIALIZE STUCK DETECTION
            lastCheckedPosition = pos;
            stuckCheckTimer = 0f;
            
            PathFollower = new PathFollower(10f);
        }


        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ✅ 1. KNOCKBACK (priority)
            UpdateKnockback(deltaTime);

            // ✅ 2. NORMAL MOVEMENT (only if no knockback)
            if (KnockbackVelocity.LengthSquared() < 0.1f)
            {
                UpdatePathfindingMovement(gameTime);
            }

            // ✅ 3. RESOLVE OVERLAPS
            var world = GameManager.Instance?.World;
            if (world != null)
            {
                ResolveOverlaps(world, pushStrength: 1.0f);
            }

            // ✅ 4. STUCK DETECTION
            stuckCheckTimer += deltaTime;
            if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                float distanceMoved = Vector2.Distance(Position, lastCheckedPosition);
                
                if (distanceMoved < STUCK_THRESHOLD && IsMoving())
                {
                    Console.WriteLine($"[{Type}] ⚠️ STUCK! Attempting unstuck...");
                    AttemptUnstuck();
                }
                
                lastCheckedPosition = Position;
                stuckCheckTimer = 0f;
            }
        }


        public  void TakeDamage(float damage, Entity attacker)
        {
            Health -= damage;

            if (Health <= 0)
            {
                Die();
            }
            else
            {
                ThreatTarget = attacker;
                
                // ✅ KNOCKBACK when hit
                if (attacker != null)
                {
                    Vector2 knockbackDir = Position - attacker.Position;
                    ApplyKnockback(knockbackDir, force: 150f);
                }
            }
        }

        

        /// <summary>
        /// ✅ Attempt to unstuck animal by trying different positions
        /// </summary>
        private void AttemptUnstuck()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            // Try 8 directions around current position
            Random rng = new Random();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (MathHelper.TwoPi / 8);
                Vector2 testPos = Position + new Vector2(
                    (float)Math.Cos(angle) * 50f,
                    (float)Math.Sin(angle) * 50f
                );

                // Check if position is valid
                if (IsTileWalkableAt(testPos, world.Tilemap))
                {
                    // Check entity collisions
                    bool blocked = false;
                    var nearby = world.GetEntitiesInRadius(testPos, 100f);
                    
                    foreach (var e in nearby)
                    {
                        if (e == this || !e.IsActive || !e.BlocksMovement)
                            continue;

                        if (!Layer.CanCollideWith(e.Layer))
                            continue;

                        if (WouldCollideAt(testPos, e))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked)
                    {
                        Position = testPos;
                        Stop();
                        Console.WriteLine($"[{Type}] ✅ Unstuck successful at angle {MathHelper.ToDegrees(angle):F0}°");
                        return;
                    }
                }
            }

            // Last resort: teleport to spawn position
            Console.WriteLine($"[{Type}] ⚠️ Could not find unstuck position, teleporting to spawn");
            Position = SpawnPosition;
            Stop();
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
            if (movementDir == Vector2.Zero)
                return;

            if (Math.Abs(movementDir.Y) >= Math.Abs(movementDir.X))
            {
                Direction = movementDir.Y > 0 ? Direction.Font : Direction.Back;
            }
            else
            {
                Direction = movementDir.X > 0 ? Direction.Right : Direction.Left;
            }
        }

        protected void UpdateAnimationState()
        {
            if (Atlas == null) return;

            if (State == lastState && Direction == lastDirection)
                return;

            AnimatedSprite = GetAnimatedSprite(GetAnimationName());

            lastState = State;
            lastDirection = Direction;
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


        public virtual void knockBack(Direction direction, float KnockBack)
        {
            float x = Position.X;
            float y = Position.Y;
            switch (direction)
            {
                case Direction.Font:
                    Position = new Vector2(x, y + KnockBack);
                    break;
                case Direction.Back:
                    Position = new Vector2(x, y - KnockBack);
                    break;
                case Direction.Right:
                    Position = new Vector2(x - KnockBack, y);
                    break;
                case Direction.Left:
                    Position = new Vector2(x + KnockBack, y);
                    break;
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
            string state = State == AnimalState.Walk ? "walk" : "idle";

            if(State == AnimalState.Attacking)
            {
                state = "attack";
            }
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