using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Behavior;
using MonoGameLibrary.PathFinding;
using TribeBuild.Player;
using TribeBuild.World;
using TribeBuild.Entity.NPC;

namespace TribeBuild.Entity.Enemies
{
    /// <summary>
    /// ✅ Enemies chỉ xuất hiện ban đêm
    /// Hành vi duy nhất: Tìm và tấn công player
    /// </summary>
    public class NightEnemyEntity : Entity
    {
        // Type
        public NightEnemyType EnemyType { get; protected set; }
        public EnemyState State { get; set; }
        
        // Combat Stats
        public float Health { get; set; }
        public float MaxHealth { get; protected set; }
        public float AttackDamage { get; protected set; }
        public float AttackRange { get; protected set; }
        public float AttackCooldown { get; protected set; }
        private float attackTimer = 0f;
        
        // Movement
        public float Speed { get; protected set; }
        protected Vector2 SpawnPosition;
        public PathFollower PathFollower { get; protected set; }
        
        // Detection
        protected float detectionRange = 300f; // Larger detection for night enemies
        protected float aggroRange = 400f; // Will chase within this range
        public PlayerCharacter Target { get; set; }
        
        // ✅ DAY/NIGHT BEHAVIOR
        private bool isDespawning = false;
        private float despawnTimer = 0f;
        private const float DESPAWN_DURATION = 2f;
        private float spawnFadeTimer = 0f;
        private const float SPAWN_FADE_DURATION = 1f;
        
        // Animation
        protected TextureAtlas Atlas;
        private Vector2 Scale;
        public Direction Direction { get; protected set; }
        private EnemyState lastState;
        private Direction lastDirection;
        
        // AI
        private BehaviorTree behaviorTree;
        private BehaviorContext behaviorContext;
        private float aiTickTimer = 0f;
        private const float AI_TICK_INTERVAL = 0.3f;
        
        // Stuck detection
        private float stuckCheckTimer = 0f;
        private Vector2 lastCheckedPosition;
        private const float STUCK_CHECK_INTERVAL = 2f;
        private const float STUCK_THRESHOLD = 10f;
        
        // Loot
        public string[] LootItems { get; protected set; }
        public int LootAmount { get; protected set; }
        
        // Visual effects
        public float Alpha { get; private set; } = 0f;
        public Color TintColor { get; private set; } = Color.White;

        public NightEnemyEntity(int id, Vector2 pos, NightEnemyType type, TextureAtlas atlas, Vector2 scale) 
            : base(id, pos)
        {
            EnemyType = type;
            State = EnemyState.Idle;
            SpawnPosition = pos;
            Direction = Direction.Font;
            Atlas = atlas;
            Scale = scale;
            
            BlocksMovement = true;
            IsPushable = true;
            Layer = CollisionLayer.Animal; // Use Animal layer
            
            PathFollower = new PathFollower(10f);
            lastCheckedPosition = pos;
            
            SetupStats(type);
            LoadAnimation();
            SetupCollider();
            ValidateSpawnPosition();
            InitializeBehaviorTree();
            
            behaviorContext = new BehaviorContext(target: this, gameTime: null);
            
            Console.WriteLine($"[NightEnemy] Spawned {type} at ({pos.X:F0}, {pos.Y:F0})");
        }

        /// <summary>
        /// ✅ Setup stats based on enemy type
        /// Night enemies are STRONGER than day animals
        /// </summary>
        private void SetupStats(NightEnemyType type)
        {
            switch (type)
            {
                case NightEnemyType.Assassin:
                    MaxHealth = 60f;
                    Speed = 80f; // Slow but tanky
                    AttackDamage = 20f;
                    AttackRange = 35f;
                    AttackCooldown = 2f;
                    detectionRange = 3000f;
                    aggroRange = 3000f;
                    LootItems = new[] { "sword", "gold" };
                    LootAmount = 3;
                    TintColor = new Color(150, 200, 150); // Greenish
                    break;

               
            }

            Health = MaxHealth;
        }

        private void LoadAnimation()
        {
            if (Atlas != null)
            {
                AnimatedSprite = Atlas.CreateAnimatedSprite("idle-font");
                if (AnimatedSprite != null)
                {
                    AnimatedSprite._scale = Scale;
                    AnimatedSprite._color = TintColor * Alpha;
                }
            }
        }

        private void SetupCollider()
        {
            if (AnimatedSprite != null)
            {
                Collider = new Rectangle(
                    (int)(AnimatedSprite._region.Width * Scale.X / 4),
                    (int)(AnimatedSprite._region.Height * Scale.Y / 4),
                    (int)(AnimatedSprite._region.Width * Scale.X / 2),
                    (int)(AnimatedSprite._region.Height * Scale.Y / 2)
                );
            }
            else
            {
                Collider = new Rectangle(0, 0, 48, 48);
            }
        }

        private void ValidateSpawnPosition()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            Vector2? validPos = world.FindNearestValidSpawnPosition(
                Position, 
                Collider, 
                Layer, 
                searchRadius: 150f
            );

            if (validPos.HasValue)
            {
                Position = validPos.Value;
                SpawnPosition = validPos.Value;
            }
        }

        /// <summary>
        /// ✅ Simple behavior tree: Hunt player only
        /// </summary>
        private void InitializeBehaviorTree()
        {
            var builder = new BehaviorTreeBuilder();

            behaviorTree = builder
                .Selector($"{EnemyType} AI")
                    // Only one behavior: Hunt and attack player
                    .Action(HuntPlayerAction, "Hunt Player")
                .End()
                .Build($"{EnemyType} Behavior Tree");
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ✅ 1. FADE IN when spawning
            if (spawnFadeTimer < SPAWN_FADE_DURATION)
            {
                spawnFadeTimer += dt;
                Alpha = Math.Min(1f, spawnFadeTimer / SPAWN_FADE_DURATION);
                
                if (AnimatedSprite != null)
                    AnimatedSprite._color = TintColor * Alpha;
            }

            // ✅ 2. CHECK IF DAYTIME - Start despawning
            if (!isDespawning && !IsNightTime())
            {
                StartDespawn();
            }

            // ✅ 3. DESPAWN FADE OUT
            if (isDespawning)
            {
                despawnTimer += dt;
                Alpha = Math.Max(0f, 1f - (despawnTimer / DESPAWN_DURATION));
                
                if (AnimatedSprite != null)
                    AnimatedSprite._color = TintColor * Alpha;

                if (despawnTimer >= DESPAWN_DURATION)
                {
                    IsActive = false;
                    Console.WriteLine($"[{EnemyType}] Despawned at dawn");
                    return;
                }
            }

            // 4. Knockback
            UpdateKnockback(dt);

            // 5. Timers
            if (attackTimer > 0f)
                attackTimer -= dt;

            // 6. AI (only if no knockback and not despawning)
            if (KnockbackVelocity.LengthSquared() < 0.1f && !isDespawning)
            {
                aiTickTimer -= dt;
                if (aiTickTimer <= 0f)
                {
                    // Always detect player
                    DetectPlayer();

                    behaviorContext.GameTime = gameTime;
                    behaviorTree?.Tick(behaviorContext);
                    aiTickTimer = AI_TICK_INTERVAL;
                }

                // 7. Movement
                UpdatePathfindingMovement(gameTime);
            }

            // 8. Resolve overlaps
            var world = GameManager.Instance?.World;
            if (world != null)
            {
                ResolveOverlaps(world, pushStrength: 1.0f);
            }

            // 9. Stuck detection
            CheckStuck(dt);

            // 10. Animation
            UpdateAnimation();
            AnimatedSprite?.Update(gameTime);
        }

        /// <summary>
        /// ✅ Check if it's night time
        /// </summary>
        private bool IsNightTime()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return false;

            float timeOfDay = gameManager.TimeOfDay;
            return timeOfDay < 6f || timeOfDay > 20f; // Night: 20:00 - 6:00
        }

        /// <summary>
        /// ✅ Start despawn animation
        /// </summary>
        private void StartDespawn()
        {
            if (isDespawning) return;
            
            isDespawning = true;
            despawnTimer = 0f;
            Console.WriteLine($"[{EnemyType}] Starting despawn at dawn");
        }

        /// <summary>
        /// ✅ Detect player - ALWAYS active
        /// </summary>
        private void DetectPlayer()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            var player = world.FindNearestPlayer(Position, detectionRange);
            
            if (player != null && player.IsActive)
            {
                // Check if within aggro range
                float distance = Vector2.Distance(Position, player.Position);
                
                if (distance <= aggroRange)
                {
                    Target = player;
                    
                    #if DEBUG
                    if (Target == null)
                        Console.WriteLine($"[{EnemyType}] 🎯 Player detected at {distance:F0}!");
                    #endif
                }
                else if (Target != null)
                {
                    // Lost aggro
                    Target = null;
                    Console.WriteLine($"[{EnemyType}] Lost player (out of range)");
                }
            }
            else
            {
                Target = null;
            }
        }

        /// <summary>
        /// ✅ Hunt player behavior - SIMPLE and DIRECT
        /// </summary>
        private NodeState HuntPlayerAction(BehaviorContext ctx)
        {
            // No target = wander back to spawn
            if (Target == null || !Target.IsActive)
            {
                // Return to spawn point
                float distFromSpawn = Vector2.Distance(Position, SpawnPosition);
                
                if (distFromSpawn > 50f)
                {
                    if (!IsMoving() || PathFollower.GetRemainingDistance(Position) < 20f)
                    {
                        MoveToWithPathfinding(SpawnPosition);
                    }
                    State = EnemyState.Walking;
                }
                else
                {
                    Stop();
                    State = EnemyState.Idle;
                }

                return NodeState.Running;
            }

            float distance = Vector2.Distance(Position, Target.Position);

            // Lost aggro
            if (distance > aggroRange)
            {
                Target = null;
                State = EnemyState.Idle;
                return NodeState.Success;
            }

            // ATTACK range
            if (distance <= AttackRange)
            {
                Stop();
                State = EnemyState.Attacking;

                if (attackTimer <= 0f)
                {
                    PerformAttack(Target);
                    attackTimer = AttackCooldown;
                }

                return NodeState.Running;
            }

            // CHASE player
            if (!IsMoving() || PathFollower.GetRemainingDistance(Position) < 30f)
            {
                MoveToWithPathfinding(Target.Position);
            }
            
            State = EnemyState.Walking;
            return NodeState.Running;
        }

        /// <summary>
        /// ✅ Perform attack with knockback
        /// </summary>
        private void PerformAttack(PlayerCharacter player)
        {
            Vector2 knockbackDir = player.Position - Position;
            player.TakeDamage(AttackDamage, knockbackDir, knockbackForce: 300f);
            
            Console.WriteLine($"[{EnemyType}] Attacked player for {AttackDamage} damage!");
        }

        /// <summary>
        /// ✅ Pathfinding movement
        /// </summary>
        private void UpdatePathfindingMovement(GameTime gameTime)
        {
            if (!PathFollower.HasPath)
            {
                Velocity = Vector2.Zero;
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector2? direction = PathFollower.Update(Position);

            if (direction.HasValue)
            {
                Velocity = direction.Value * Speed;
                
                var world = GameManager.Instance?.World;
                if (world != null)
                {
                    Vector2 desiredPos = Position + Velocity * deltaTime;
                    Position = MoveWithCollision(desiredPos, deltaTime, world);
                }
                else
                {
                    Position += Velocity * deltaTime;
                }

                UpdateDirection(direction.Value);
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }

        private void UpdateDirection(Vector2 movementDir)
        {
            if (movementDir == Vector2.Zero) return;

            if (Math.Abs(movementDir.Y) >= Math.Abs(movementDir.X))
            {
                Direction = movementDir.Y > 0 ? Direction.Font : Direction.Back;
            }
            else
            {
                Direction = movementDir.X > 0 ? Direction.Right : Direction.Left;
            }
        }

        private void MoveToWithPathfinding(Vector2 target)
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

        private void Stop()
        {
            PathFollower.ClearPath();
            Velocity = Vector2.Zero;
        }

        private bool IsMoving()
        {
            return PathFollower.HasPath && !PathFollower.ReachedDestination;
        }

        /// <summary>
        /// ✅ Stuck detection
        /// </summary>
        private void CheckStuck(float deltaTime)
        {
            stuckCheckTimer += deltaTime;
            
            if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                float distanceMoved = Vector2.Distance(Position, lastCheckedPosition);
                
                if (distanceMoved < STUCK_THRESHOLD && IsMoving())
                {
                    Console.WriteLine($"[{EnemyType}] ⚠️ STUCK! Attempting unstuck...");
                    AttemptUnstuck();
                }
                
                lastCheckedPosition = Position;
                stuckCheckTimer = 0f;
            }
        }

        private void AttemptUnstuck()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            Random rng = new Random();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (MathHelper.TwoPi / 8);
                Vector2 testPos = Position + new Vector2(
                    (float)Math.Cos(angle) * 50f,
                    (float)Math.Sin(angle) * 50f
                );

                // if (world.IsPositionValid(testPos, Collider, Layer))
                // {
                //     Position = testPos;
                //     Stop();
                //     Console.WriteLine($"[{EnemyType}] ✅ Unstuck successful");
                //     return;
                // }
            }

            Position = SpawnPosition;
            Stop();
        }

        /// <summary>
        /// ✅ Update animation
        /// </summary>
        private void UpdateAnimation()
        {
            if (State == lastState && Direction == lastDirection)
                return;

            if (Atlas != null)
            {
                string animName = GetAnimationName();
                AnimatedSprite = Atlas.CreateAnimatedSprite(animName);
                if (AnimatedSprite != null)
                {
                    AnimatedSprite._scale = Scale;
                    AnimatedSprite._color = TintColor * Alpha;
                }
            }

            lastState = State;
            lastDirection = Direction;
        }

        private string GetAnimationName()
        {
            string state = State switch
            {
                EnemyState.Walking => "walk",
                EnemyState.Attacking => "attack",
                _ => "idle"
            };

            string direction = Direction.ToString().ToLower();
            return $"{state}-{direction}";
        }

        /// <summary>
        /// ✅ Take damage
        /// </summary>
        public virtual void TakeDamage(float damage, Entity attacker)
        {
            Health -= damage;

            if (Health <= 0)
            {
                Die();
            }
            else
            {
                // Always aggro on attacker if it's player
                if (attacker is PlayerCharacter player)
                {
                    Target = player;
                }
                
                // Knockback
                if (attacker != null)
                {
                    Vector2 knockbackDir = Position - attacker.Position;
                    ApplyKnockback(knockbackDir, force: 180f);
                }
            }
        }

        protected virtual void Die()
        {
            IsActive = false;
            Console.WriteLine($"[{EnemyType}] Died at ({Position.X:F0}, {Position.Y:F0})");
            
            // TODO: Drop loot
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, GameTime gameTime = null)
        {
            if (!IsActive) return;

            if (AnimatedSprite != null)
            {
                AnimatedSprite.Draw(spriteBatch, Position);
            }

            #if DEBUG
            DrawDebugAABB(spriteBatch);
            #endif
        }
    }

    /// <summary>
    /// ✅ Night Enemy Types
    /// </summary>
    public enum NightEnemyType
    {
        Assassin,
    }

    public enum EnemyState
    {
        Idle,
        Walking,
        Attacking,
        Fleeing
    }
}