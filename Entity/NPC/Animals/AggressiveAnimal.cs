using System;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Behavior;

namespace TribeBuild.Entity.NPC.Animals
{
    /// <summary>
    /// Động vật hung dữ - tấn công dân làng trong lãnh thổ
    /// Sử dụng KD-Tree để tìm NPC gần nhất
    /// </summary>
    public class AggressiveAnimal : AnimalEntity
    {
        // Combat
        public float AttackDamage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackCooldown { get; private set; }
        private Vector2 Scale;
        private float attackTimer = 0f;

        private float aiTickTimer = 0f;
        private const float AI_TICK_INTERVAL = 0.4f; // 10 lần / giây
        private static readonly Random rng = new Random();


        // Territory
        private float territoryRadius = 250f;
        private BehaviorTree behaviorTree;
        private BehaviorContext behaviorContext;
        private AnimalState lastState;
        private Direction lastDirection;


        // Walk
        private Vector2? currentWanderTarget = null;
        private float wanderPauseTimer = 0f;
        private const float MIN_WANDER_DISTANCE = 30f;
        private const float MAX_WANDER_DISTANCE = 100f;
        private const float MIN_PAUSE_TIME = 1f;
        private const float MAX_PAUSE_TIME = 3f;


        public AggressiveAnimal(int id, Vector2 pos, AnimalType type, TextureAtlas atlas, Vector2 scale) 
            : base(id, pos, type, atlas)
        {
            IsAggressive = true;
            Scale = scale;

            // Setup stats based on type
            switch (type)
            {
                case AnimalType.Boar:
                    MaxHealth = 80f;
                    Speed = 70f;
                    AttackDamage = 15f;
                    AttackRange = 40f;
                    AttackCooldown = 2f;
                    LootItems = new[] { "meat", "tusk" };
                    LootAmount = 2;
                    detectionRange = 150f;
                    territoryRadius = 250f;
                    break;
            }

            Health = MaxHealth;

            // Load initial animation
            AnimatedSprite = GetAnimatedSprite(GetAnimationName());
            AnimatedSprite._scale = Scale;

            // Set collider
            if (AnimatedSprite != null)
            {
                Collider = new Rectangle(
                    AnimatedSprite._region.Width / 4,
                    AnimatedSprite._region.Height / 2,
                    AnimatedSprite._region.Width / 2,
                    AnimatedSprite._region.Height / 2
                );
            }
            else
            {
                Collider = new Rectangle(0, 0, 48, 48);
            }

            InitializeBehaviorTree();
            behaviorContext = new BehaviorContext(
                target: this,
                gameTime: null // sẽ set lại mỗi frame
            );

            //GameLogger.Instance?.Debug("Animal", $"Created aggressive {type} at ({pos.X:F0}, {pos.Y:F0})");
        }

        protected override void InitializeBehaviorTree()
        {
            var builder = new BehaviorTreeBuilder();

            behaviorTree = builder
                .Selector($"{Type} Behavior")
                    // 1. ATTACK target
                    .Sequence("Attack")
                        .Condition(ctx => ThreatTarget != null && ThreatTarget.IsActive, "Has Target?")
                        .Action(AttackAction, "Attack Target")
                    .End()

                    // 2. WANDER around territory
                    .Action(WanderAction, "Wander")
                .End()
                .Build($"{Type} Behavior Tree");
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 1. SENSOR
            if (ThreatTarget == null)
                DetectNearbyNPCs();

            // 2. TIMERS
            if (wanderPauseTimer > 0f)
                wanderPauseTimer -= dt;

            if (attackTimer > 0f)
                attackTimer -= dt;

            // 3. AI THINK
            aiTickTimer -= dt;
            if (aiTickTimer <= 0f)
            {
                behaviorContext.GameTime = gameTime;
                behaviorTree.Tick(behaviorContext);
                aiTickTimer = AI_TICK_INTERVAL;
            }

            // 4. MOVEMENT
            UpdatePathfindingMovement(gameTime);

            // ✅ 5. UPDATE ANIMATION STATE
            UpdateAnimation();

            // 6. PLAY ANIMATION
            AnimatedSprite?.Update(gameTime);
        }




        /// <summary>
        /// Use KD-Tree to find nearest NPC in territory
        /// </summary>
        private void DetectNearbyNPCs()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            // TODO: Use KD-Tree to find NPCs
            // For now, use simple distance check
            var npcs = world.GetEntitiesOfType<NPCBody>();

            NPCBody nearestNPC = null;

            float nearestDistance = float.MaxValue;

            foreach (var npc in npcs)
            {
                if (!npc.IsActive) continue;

                float distance = Vector2.Distance(Position, npc.Position);

                // Check if in detection range
                if (distance <= detectionRange && distance < nearestDistance)
                {
                    nearestNPC = npc;
                    nearestDistance = distance;
                }
            }

            if (nearestNPC != null)
            {
                ThreatTarget = nearestNPC;
                currentWanderTarget = null; // Reset wander when detecting threat
                wanderPauseTimer = 0f;
                //GameLogger.Instance?.Debug("Animal", $"{Type} detected NPC #{nearestNPC.ID} at distance {nearestDistance:F1}");
            }
        }

        private NodeState AttackAction(BehaviorContext ctx)
        {
            if (ThreatTarget == null || !ThreatTarget.IsActive)
            {
                ThreatTarget = null;
                State = AnimalState.Idle;
                ;
                return NodeState.Success;
            }

            float distance = Vector2.Distance(Position, ThreatTarget.Position);

            // Check if target left territory
            if (Vector2.Distance(Position, SpawnPosition) > territoryRadius)
            {
                // Return to territory
                ThreatTarget = null;
                MoveToWithPathfinding(SpawnPosition);
                State = AnimalState.Walk;
                ;
                return NodeState.Success;
            }

            // Chase if too far
            if (distance > AttackRange)
            {
                // Use A* pathfinding to chase
                if (!IsMoving() || PathFollower.GetRemainingDistance(Position) < 20f)
                {
                    MoveToWithPathfinding(ThreatTarget.Position);
                }

                State = AnimalState.Walk;
                ;
                return NodeState.Running;
            }

            if (distance > detectionRange * 1.2f)
            {
                ThreatTarget = null;
                State = AnimalState.Walk;
                ;
                return NodeState.Success;
            }


            // Attack
            Stop();
            State = AnimalState.Attacking;
            ;

            if (attackTimer <= 0f)
            {
                PerformAttack(ThreatTarget);
                attackTimer = AttackCooldown;
                return NodeState.Success;
            }

            return NodeState.Running;

        }

        private void PerformAttack(Entity target)
        {
            // Apply damage
            if (target is NPCBody npc)
            {
                var villager = npc.AI as VillagerAI;
                if (villager != null)
                {
                    villager.OnAttacked(this);
                }

                var hunter = npc.AI as HunterAI;
                if (hunter != null)
                {
                    hunter.OnAttacked(this);
                }

                //GameLogger.Instance?.LogCombat(ID, target.ID, AttackDamage);
            }
        }

        private NodeState WanderAction(BehaviorContext ctx)
        {
            // If pausing, stay idle
            if (wanderPauseTimer > 0f)
            {
                Stop();
                State = AnimalState.Idle;
                ;
                return NodeState.Running;
            }

            // If has target and still moving towards it
            if (currentWanderTarget.HasValue)
            {
                float distanceToTarget = Vector2.Distance(Position, currentWanderTarget.Value);
                
                // Check if reached destination or path completed
                if (!IsMoving() || distanceToTarget <= 10f)
                {
                    // Reached target - start pause
                    currentWanderTarget = null;
                    wanderPauseTimer = MIN_PAUSE_TIME + (float)(rng.NextDouble() * (MAX_PAUSE_TIME - MIN_PAUSE_TIME));
                    Stop();
                    State = AnimalState.Idle;
                    ;
                    return NodeState.Running;
                }
                
                // Still moving towards target
                State = AnimalState.Walk;
                ;
                return NodeState.Running;
            }

            // Pick new wander point within territory
            Vector2 newTarget = PickRandomWanderPoint();
            
            // Make sure the point is within territory
            float distFromSpawn = Vector2.Distance(newTarget, SpawnPosition);
            if (distFromSpawn > territoryRadius)
            {
                // Pull it back towards spawn
                Vector2 direction = Vector2.Normalize(SpawnPosition - newTarget);
                newTarget = SpawnPosition + direction * (territoryRadius * 0.8f);
            }

            currentWanderTarget = newTarget;
            MoveToWithPathfinding(newTarget);
            
            State = AnimalState.Walk;
            return NodeState.Running;
        }

        /// <summary>
        /// Pick a random point near current position for wandering
        /// </summary>
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

            AnimatedSprite = GetAnimatedSprite(GetAnimationName());

            // ✅ SET SCALE NGAY SAU KHI TẠO
            AnimatedSprite._scale = Scale;

            lastState = State;
            lastDirection = Direction;
        }


    }
}