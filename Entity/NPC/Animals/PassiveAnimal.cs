using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Behavior;
using TribeBuild.Player;
using TribeBuild.World;

namespace TribeBuild.Entity.NPC.Animals
{
    /// <summary>
    /// ✅ Động vật ôn hòa với chu kỳ ngày/đêm
    /// - Ban ngày: Wander bình thường
    /// - Khi trời tối: Di chuyển về spawn zone
    /// - Vào spawn zone: Despawn (biến mất)
    /// - Sáng: Respawn tại spawn zone
    /// </summary>
    public class PassiveAnimal : AnimalEntity
    {
        private Vector2 Scale;
        private AnimalState lastState;
        private static readonly Random rng = new Random();

        // Walk system
        private Vector2? currentWanderTarget = null;
        private float wanderPauseTimer = 0f;
        private const float MIN_WANDER_DISTANCE = 30f;
        private const float MAX_WANDER_DISTANCE = 80f;
        private const float MIN_PAUSE_TIME = 2f;
        private const float MAX_PAUSE_TIME = 5f;
        private Direction lastDirection;

        private float territoryRadius = 250f;
        private BehaviorTree behaviorTree;
        private BehaviorContext behaviorContext;
        
        // Grazing
        private float grazeTimer = 0f;
        private const float GRAZE_DURATION = 3f;

        // AI tick
        private float aiTickTimer = 0f;
        private const float AI_TICK_INTERVAL = 0.4f;

        // ✅ DAY/NIGHT CYCLE
        private bool isReturningHome = false;
        private bool isDespawning = false;
        private float despawnTimer = 0f;
        private const float DESPAWN_DURATION = 1.5f;
        public float Alpha { get; private set; } = 1f;
        
        // ✅ Spawn zone (from SpawnZoneManager)
        private Rectangle spawnZone = Rectangle.Empty;
        private const float DESPAWN_ZONE_RADIUS = 50f; // Radius to check if inside spawn zone

        

        public PassiveAnimal(int id, Vector2 pos, AnimalType type, TextureAtlas atlas, Vector2 scale)
            : base(id, pos, type, atlas)
        {
            IsAggressive = false;
            Scale = scale;
            BlocksMovement = true;
            IsPushable = true;
            Layer = CollisionLayer.Animal;

            // Setup stats based on type
            switch (type)
            {
                case AnimalType.Chicken:
                    MaxHealth = 50f;
                    Speed = 50f;
                    LootItems = new[] { "meat", "feather" };
                    LootAmount = 2;
                    detectionRange = 100f;
                    WanderRadius = 150f;
                    break;

                case AnimalType.Sheep:
                    MaxHealth = 50f;
                    Speed = 60f;
                    LootItems = new[] { "meat", "wool" };
                    LootAmount = 3;
                    detectionRange = 120f;
                    WanderRadius = 180f;
                    break;
            }

            Health = MaxHealth;

            // Load initial animation
            AnimatedSprite = GetAnimatedSprite(GetAnimationName());
            AnimatedSprite._scale = Scale;

            // Set collider based on sprite
            if (AnimatedSprite != null)
            {
                Collider = new Rectangle(
                    (int)(AnimatedSprite._region.Width * Scale.X / 4),
                    (int)(AnimatedSprite._region.Height * Scale.Y / 2),
                    (int)(AnimatedSprite._region.Width * Scale.X / 2),
                    (int)(AnimatedSprite._region.Height * Scale.Y / 2)
                );
            }
            else
            {
                Collider = new Rectangle(0, 0, 32, 32);
            }

            InitializeBehaviorTree();
    
            behaviorContext = new BehaviorContext(
                target: this,
                gameTime: null
            );
        }

        /// <summary>
        /// ✅ Set spawn zone from SpawnZoneManager
        /// </summary>
        public void SetSpawnZone(Rectangle zone)
        {
            spawnZone = zone;
            Console.WriteLine($"[{Type}] Spawn zone set: ({zone.X}, {zone.Y}) {zone.Width}x{zone.Height}");
        }

        protected override void InitializeBehaviorTree()
        {
            var builder = new BehaviorTreeBuilder();

            behaviorTree = builder
                .Selector($"{Type} Behavior")
                    // ✅ 1. NIGHT - Return home and despawn
                    .Sequence("Night Behavior")
                        .Condition(ctx => IsNight() && !isDespawning, "Is Night?")
                        .Action(ReturnHomeAction, "Return to Spawn")
                    .End()

                    // 2. FLEE from threats
                    .Sequence("Flee")
                        .Condition(ctx => ThreatTarget != null && ThreatTarget.IsActive, "Threat Detected?")
                        .Action(FleeAction, "Run Away")
                    .End()

                    // 3. WANDER and graze (day only)
                    .Action(WanderAction, "Wander")
                .End()
                .Build($"{Type} Behavior Tree");
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ✅ DESPAWN FADE OUT
            if (isDespawning)
            {
                despawnTimer += deltaTime;
                Alpha = Math.Max(0f, 1f - (despawnTimer / DESPAWN_DURATION));
                
                if (AnimatedSprite != null)
                    AnimatedSprite._color = Color.White * Alpha;

                if (despawnTimer >= DESPAWN_DURATION)
                {
                    IsActive = false;
                    Console.WriteLine($"[{Type}] Despawned at night");
                    return;
                }
            }

            // 1. Update pause timer
            if (wanderPauseTimer > 0f)
            {
                wanderPauseTimer -= deltaTime;
            }

            // 2. Update graze timer if idling
            if (State == AnimalState.Idle && wanderPauseTimer > 0f)
            {
                grazeTimer += deltaTime;
            }
            else
            {
                grazeTimer = 0f;
            }

            // 3. Detect threats (only during day)
            if (ThreatTarget == null && !isReturningHome && !IsNight())
            {
                DetectThreats();
            }

            // 4. AI tick
            aiTickTimer -= deltaTime;
            if (aiTickTimer <= 0f)
            {
                behaviorContext.GameTime = gameTime;
                behaviorTree?.Tick(behaviorContext);
                aiTickTimer = AI_TICK_INTERVAL;
            }

            base.Update(gameTime);
            UpdateAnimation();
            AnimatedSprite?.Update(gameTime);
        }

        /// <summary>
        /// ✅ Check if it's night
        /// </summary>
        private bool IsNight()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return false;

            float timeOfDay = gameManager.TimeOfDay;
            return timeOfDay < 6f || timeOfDay > 19.5f; // Night starts at 19:30 (dusk)
        }

        /// <summary>
        /// ✅ NEW: Return home behavior at night
        /// </summary>
        private NodeState ReturnHomeAction(BehaviorContext ctx)
        {
            isReturningHome = true;
            ThreatTarget = null; // Clear threats
            currentWanderTarget = null;

            // Check if already in spawn zone
            if (IsInSpawnZone())
            {
                // Start despawning
                if (!isDespawning)
                {
                    Stop();
                    State = AnimalState.Idle;
                    isDespawning = true;
                    despawnTimer = 0f;
                    Console.WriteLine($"[{Type}] Reached spawn zone, starting despawn");
                }
                return NodeState.Running;
            }

            // Move to spawn position
            if (!IsMoving() || PathFollower.GetRemainingDistance(Position) < 20f)
            {
                Vector2 spawnCenter = GetSpawnZoneCenter();
                MoveToWithPathfinding(spawnCenter);
            }

            State = AnimalState.Walk;
            return NodeState.Running;
        }

        /// <summary>
        /// ✅ Check if animal is inside spawn zone
        /// </summary>
        private bool IsInSpawnZone()
        {
            if (spawnZone == Rectangle.Empty)
            {
                // Fallback: use spawn position
                float distToSpawn = Vector2.Distance(Position, SpawnPosition);
                return distToSpawn < DESPAWN_ZONE_RADIUS;
            }

            // Check if inside spawn zone rectangle
            return spawnZone.Contains(new Point((int)Position.X, (int)Position.Y));
        }

        /// <summary>
        /// ✅ Get center of spawn zone
        /// </summary>
        private Vector2 GetSpawnZoneCenter()
        {
            if (spawnZone == Rectangle.Empty)
                return SpawnPosition;

            return new Vector2(
                spawnZone.X + spawnZone.Width / 2f,
                spawnZone.Y + spawnZone.Height / 2f
            );
        }

        /// <summary>
        /// Detect nearby threats (hunters, aggressive animals)
        /// </summary>
        private void DetectThreats()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            // Check for player first
            var player = world.FindNearestPlayer(Position, detectionRange);
            if (player != null && player.IsActive)
            {
                ThreatTarget = player;
                currentWanderTarget = null;
                wanderPauseTimer = 0f;
                return;
            }

            // Check for aggressive animals
            var nearbyEntities = world.KDTree.FindInRadius(Position, detectionRange);

            foreach (var result in nearbyEntities)
            {
                var entity = result.Item;
                
                if (entity == null || !entity.IsActive)
                    continue;

                if (entity is AggressiveAnimal aggAnimal)
                {
                    ThreatTarget = aggAnimal;
                    currentWanderTarget = null;
                    wanderPauseTimer = 0f;
                    return;
                }
            }
        }

        private NodeState FleeAction(BehaviorContext ctx)
        {
            if (ThreatTarget == null || !ThreatTarget.IsActive)
            {
                ThreatTarget = null;
                State = AnimalState.Idle;
                UpdateAnimation();
                return NodeState.Success;
            }

            // Calculate flee direction (away from threat)
            Vector2 fleeDirection = Position - ThreatTarget.Position;
            float distance = fleeDirection.Length();

            // Check if safe now
            if (distance > fleeDistance)
            {
                ThreatTarget = null;
                State = AnimalState.Idle;
                UpdateAnimation();
                return NodeState.Success;
            }

            // Calculate flee target
            fleeDirection.Normalize();
            Vector2 fleeTarget = Position + fleeDirection * 150f;

            // Keep within wander area
            float distFromSpawn = Vector2.Distance(fleeTarget, SpawnPosition);
            if (distFromSpawn > WanderRadius)
            {
                Vector2 toSpawn = Vector2.Normalize(SpawnPosition - Position);
                fleeTarget = Position + toSpawn * 100f;
            }

            // Update path if not moving
            if (!IsMoving() || (currentWanderTarget.HasValue && 
                Vector2.Distance(Position, currentWanderTarget.Value) < 20f))
            {
                MoveToWithPathfinding(fleeTarget);
                currentWanderTarget = fleeTarget;
            }

            State = AnimalState.Walk;
            UpdateAnimation();

            return NodeState.Running;
        }

        private NodeState WanderAction(BehaviorContext ctx)
        {
            // Reset night flag during day
            if (!IsNight())
            {
                isReturningHome = false;
            }

            // If pausing (grazing), stay idle
            if (wanderPauseTimer > 0f)
            {
                Stop();
                State = AnimalState.Idle;
                UpdateAnimation();
                return NodeState.Running;
            }

            // If has target and still moving towards it
            if (currentWanderTarget.HasValue)
            {
                float distanceToTarget = Vector2.Distance(Position, currentWanderTarget.Value);
                
                if (!IsMoving() || distanceToTarget <= 10f)
                {
                    currentWanderTarget = null;
                    wanderPauseTimer =
                        MIN_PAUSE_TIME +
                        (float)(rng.NextDouble() * (MAX_PAUSE_TIME - MIN_PAUSE_TIME));

                    Stop();
                    State = AnimalState.Idle;
                    return NodeState.Success;
                }
                
                State = AnimalState.Walk;
                UpdateAnimation();
                return NodeState.Running;
            }

            // Pick new wander point
            Vector2 newTarget = PickRandomWanderPoint();
            
            float distFromSpawn = Vector2.Distance(newTarget, SpawnPosition);
            if (distFromSpawn > WanderRadius)
            {
                Vector2 direction = Vector2.Normalize(SpawnPosition - newTarget);
                newTarget = SpawnPosition + direction * (WanderRadius * 0.8f);
            }

            currentWanderTarget = newTarget;
            MoveToWithPathfinding(newTarget);
            
            State = AnimalState.Walk;
            UpdateAnimation();
            return NodeState.Running;
        }

        private Vector2 PickRandomWanderPoint()
        {
            float angle = (float)(rng.NextDouble() * Math.PI * 2);
            float distance = MIN_WANDER_DISTANCE + (float)(rng.NextDouble() * (MAX_WANDER_DISTANCE - MIN_WANDER_DISTANCE));

            Vector2 offset = new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );

            return Position + offset;
        }

        private void UpdateAnimation()
        {
            if (State == lastState && Direction == lastDirection)
                return;

            if (Atlas != null)
            {
                AnimatedSprite = GetAnimatedSprite(GetAnimationName());
                AnimatedSprite._scale = Scale;
                AnimatedSprite._color = Color.White * Alpha;
            }

            lastState = State;
            lastDirection = Direction;
        }

        protected override string GetAnimationName()
        {
            string state = State == AnimalState.Walk ? "walk" : "idle";
            string direction = Direction.ToString().ToLower();
            return $"{state}-{direction}";
        }
    }
}