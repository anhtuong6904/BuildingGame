using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Behavior;
using TribeBuild.Player;
using TribeBuild.World;

namespace TribeBuild.Entity.NPC.Animals
{
    /// <summary>
    /// Động vật ôn hòa - chỉ biết chạy trốn
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

        public PassiveAnimal(int id, Vector2 pos, AnimalType type, TextureAtlas atlas, Vector2 scale)
            : base(id, pos, type, atlas)
        {
            IsAggressive = false;
            Scale = scale;

            // Setup stats based on type
            switch (type)
            {
                case AnimalType.Chicken:
                    MaxHealth = 20f;
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
                    AnimatedSprite._region.Width / 4,
                    AnimatedSprite._region.Height / 2,
                    AnimatedSprite._region.Width / 2,
                    AnimatedSprite._region.Height / 2
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

            //GameLogger.Instance?.Debug("Animal", $"Created {type} at ({pos.X:F0}, {pos.Y:F0})");
        }

        protected override void InitializeBehaviorTree()
        {
            var builder = new BehaviorTreeBuilder();

            behaviorTree = builder
                .Selector($"{Type} Behavior")
                    // 1. FLEE from threats
                    .Sequence("Flee")
                        .Condition(ctx => ThreatTarget != null && ThreatTarget.IsActive, "Threat Detected?")
                        .Action(FleeAction, "Run Away")
                    .End()

                    // 2. WANDER and occasionally graze
                    .Action(WanderAction, "Wander")
                .End()
                .Build($"{Type} Behavior Tree");
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            // 1. Update pause timer
            if (wanderPauseTimer > 0f)
            {
                wanderPauseTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            // 2. Update graze timer if idling
            if (State == AnimalState.Idle && wanderPauseTimer > 0f)
            {
                grazeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else
            {
                grazeTimer = 0f;
            }

            // 3. Detect threats
            if (ThreatTarget == null)
            {
                DetectThreats();
            }

            // 4. AI tick
            aiTickTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (aiTickTimer <= 0f)
            {
                // ✅ FIX: Use correct variable name
                behaviorContext.GameTime = gameTime;
                behaviorTree?.Tick(behaviorContext);
                aiTickTimer = AI_TICK_INTERVAL;
            }

            base.Update(gameTime);
            UpdateAnimation();
            AnimatedSprite?.Update(gameTime);
        }


        /// <summary>
        /// Detect nearby threats (hunters, aggressive animals)
        /// </summary>
        private void DetectThreats()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;

            // ✅ Check for player first (most common threat)
            var player = world.FindNearestPlayer(Position, detectionRange);
            if (player != null && player.IsActive)
            {
                ThreatTarget = player;
                currentWanderTarget = null;
                wanderPauseTimer = 0f;
                return;
            }

            // ✅ Then check for other threats via KD-Tree
            var nearbyEntities = world.KDTree.FindInRadius(Position, detectionRange);

            foreach (var result in nearbyEntities)
            {
                var entity = result.Item;
                
                if (entity == null || !entity.IsActive)
                    continue;

                // Hunter NPCs
                if (entity is NPCBody npc && npc.AI is HunterAI)
                {
                    ThreatTarget = npc;
                    currentWanderTarget = null;
                    wanderPauseTimer = 0f;
                    return;
                }

                // Aggressive animals
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
                // Flee towards spawn instead
                Vector2 toSpawn = Vector2.Normalize(SpawnPosition - Position);
                fleeTarget = Position + toSpawn * 100f;
            }

            // Update path if not moving or close to current target
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
                
                // Check if reached destination or path completed
                if (!IsMoving() || distanceToTarget <= 10f)
                {
                    currentWanderTarget = null;
                    wanderPauseTimer =
                        MIN_PAUSE_TIME +
                        (float)(rng.NextDouble() * (MAX_PAUSE_TIME - MIN_PAUSE_TIME));

                    Stop();
                    State = AnimalState.Idle;

                    return NodeState.Success; // ✅ QUAN TRỌNG
                }
                
                // Still moving towards target
                State = AnimalState.Walk;
                UpdateAnimation();
                return NodeState.Running;
            }

            // Pick new wander point
            Vector2 newTarget = PickRandomWanderPoint();
            
            // Make sure the point is within wander radius
            float distFromSpawn = Vector2.Distance(newTarget, SpawnPosition);
            if (distFromSpawn > WanderRadius)
            {
                // Pull it back towards spawn
                Vector2 direction = Vector2.Normalize(SpawnPosition - newTarget);
                newTarget = SpawnPosition + direction * (WanderRadius * 0.8f);
            }

            currentWanderTarget = newTarget;
            MoveToWithPathfinding(newTarget);
            
            State = AnimalState.Walk;
            UpdateAnimation();
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

            if (Atlas != null)
            {
                AnimatedSprite = GetAnimatedSprite(GetAnimationName());

                // ✅ SET SCALE NGAY SAU KHI TẠO
                AnimatedSprite._scale = Scale;
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