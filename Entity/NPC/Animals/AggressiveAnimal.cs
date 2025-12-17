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
        private float attackTimer = 0f;

        // Territory
        private float territoryRadius = 250f;

        public AggressiveAnimal(int id, Vector2 pos, AnimalType type, TextureAtlas atlas) 
            : base(id, pos, type, atlas)
        {
            IsAggressive = true;

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

                    // 2. PATROL territory
                    .Sequence("Patrol")
                        .Condition(ctx => !IsMoving(), "Not Moving?")
                        .Action(PatrolAction, "Patrol")
                    .End()

                    // 3. IDLE
                    .Action(IdleAction, "Idle")
                .End()
                .Build($"{Type} Behavior Tree");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            AnimatedSprite.Update(gameTime);

            // Update attack timer
            if (attackTimer > 0f)
            {
                attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            // Detect nearby NPCs if no target
            if (ThreatTarget == null)
            {
                DetectNearbyNPCs();
            }
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
                //GameLogger.Instance?.Debug("Animal", $"{Type} detected NPC #{nearestNPC.ID} at distance {nearestDistance:F1}");
            }
        }

        private NodeState AttackAction(BehaviorContext ctx)
        {
            if (ThreatTarget == null || !ThreatTarget.IsActive)
            {
                ThreatTarget = null;
                State = AnimalState.Idle;
                UpdateAnimation();
                return NodeState.Success;
            }

            float distance = Vector2.Distance(Position, ThreatTarget.Position);

            // Check if target left territory
            if (Vector2.Distance(Position, SpawnPosition) > territoryRadius)
            {
                // Return to territory
                ThreatTarget = null;
                MoveToWithPathfinding(SpawnPosition);
                State = AnimalState.Wandering;
                UpdateAnimation();
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

                State = AnimalState.Wandering;
                UpdateAnimation();
                return NodeState.Running;
            }

            // Attack
            Stop();
            State = AnimalState.Attacking;
            UpdateAnimation();

            if (attackTimer <= 0f)
            {
                PerformAttack(ThreatTarget);
                attackTimer = AttackCooldown;
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

        private NodeState PatrolAction(BehaviorContext ctx)
        {
            var patrolTarget = ctx.GetData<Vector2?>("patrolTarget");
            var patrolTimer = ctx.GetData<float>("patrolTimer");

            patrolTimer -= (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;

            if (patrolTimer <= 0f || !patrolTarget.HasValue)
            {
                // Pick new patrol point within territory
                var random = new Random();
                float angle = (float)(random.NextDouble() * Math.PI * 2);
                float distance = (float)(random.NextDouble() * territoryRadius);

                patrolTarget = SpawnPosition + new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );

                patrolTimer = 5f + (float)(random.NextDouble() * 5f);

                ctx.SetData("patrolTarget", patrolTarget);
                ctx.SetData("patrolTimer", patrolTimer);

                MoveToWithPathfinding(patrolTarget.Value);
            }

            State = AnimalState.Wandering;
            UpdateAnimation();
            return NodeState.Running;
        }

        private NodeState IdleAction(BehaviorContext ctx)
        {
            Stop();
            State = AnimalState.Idle;
            UpdateAnimation();
            return NodeState.Running;
        }

        private void UpdateAnimation()
        {
            if (Atlas != null)
            {
                AnimatedSprite = GetAnimatedSprite(GetAnimationName());
            }
        }
    }
}