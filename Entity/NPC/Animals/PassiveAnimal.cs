using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Behavior;
using System.Runtime.CompilerServices;

namespace TribeBuild.Entity.NPC.Animals
{
    /// <summary>
    /// Động vật ôn hòa - chỉ biết chạy trốn
    /// </summary>
    public class PassiveAnimal : AnimalEntity
    {
        private float grazeTimer = 0f;
        private float grazeDuration = 3f;

        public PassiveAnimal(int id, Vector2 pos, AnimalType type, TextureAtlas atlas) 
            : base(id, pos, type, atlas)
        {
            IsAggressive = false;

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

            //GameLogger.Instance?.Debug("Animal", $"Created {type} at ({pos.X:F0}, {pos.Y:F0})");
        }

        protected override void InitializeBehaviorTree()
        {
            var builder = new BehaviorTreeBuilder();

            behaviorTree = builder
                .Selector($"{Type} Behavior")
                    // 1. FLEE from threats
                    .Sequence("Flee")
                        .Condition(ctx => ThreatTarget != null, "Threat Detected?")
                        .Action(FleeAction, "Run Away")
                    .End()

                    // 2. GRAZE (eat grass)
                    .Sequence("Graze")
                        .Condition(ctx => !IsMoving(), "Standing Still?")
                        .Action(GrazeAction, "Graze")
                    .End()

                    // 3. WANDER
                    .Action(WanderAction, "Wander")
                .End()
                .Build($"{Type} Behavior Tree");
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

            if (distance > fleeDistance)
            {
                // Safe now
                ThreatTarget = null;
                State = AnimalState.Idle;
                UpdateAnimation();
                return NodeState.Success;
            }

            fleeDirection.Normalize();
            Vector2 fleeTarget = Position + fleeDirection * 150f;

            // Keep within wander area
            if (Vector2.Distance(fleeTarget, SpawnPosition) > WanderRadius)
            {
                fleeTarget = SpawnPosition;
            }

            // Use pathfinding to flee
            MoveToWithPathfinding(fleeTarget);
            State = AnimalState.Fleeing;
            UpdateAnimation();

            return NodeState.Running;
        }

        private NodeState GrazeAction(BehaviorContext ctx)
        {
            grazeTimer += (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;
            State = AnimalState.Idle;
            UpdateAnimation();

            if (grazeTimer >= grazeDuration)
            {
                grazeTimer = 0f;
                return NodeState.Success;
            }

            return NodeState.Running;
        }

        private NodeState WanderAction(BehaviorContext ctx)
        {
            var wanderTarget = ctx.GetData<Vector2?>("wanderTarget");
            var wanderTimer = ctx.GetData<float>("wanderTimer");

            wanderTimer -= (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;

            if (wanderTimer <= 0f || !wanderTarget.HasValue)
            {
                // Pick new wander point within radius
                var random = new Random();
                float angle = (float)(random.NextDouble() * Math.PI * 2);
                float distance = (float)(random.NextDouble() * WanderRadius);

                wanderTarget = SpawnPosition + new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );

                wanderTimer = 3f + (float)(random.NextDouble() * 4f);

                ctx.SetData("wanderTarget", wanderTarget);
                ctx.SetData("wanderTimer", wanderTimer);
            }

            if (wanderTarget.HasValue)
            {
                float dist = Vector2.Distance(Position, wanderTarget.Value);

                if (dist < 10f)
                {
                    ctx.SetData("wanderTarget", (Vector2?)null);
                    State = AnimalState.Idle;
                    UpdateAnimation();
                }
                else if (!IsMoving())
                {
                    MoveToWithPathfinding(wanderTarget.Value);
                    State = AnimalState.Wandering;
                    UpdateAnimation();
                }
            }

            return NodeState.Running;
        }

        private void UpdateAnimation()
        {
            if (Atlas != null)
            {
                AnimatedSprite = GetAnimatedSprite(GetAnimationName());
            }
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            AnimatedSprite.Update(gameTime);
        }
    }
}