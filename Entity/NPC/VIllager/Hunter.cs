// using System;
// using System.Collections.Generic;
// using Microsoft.Xna.Framework;
// using MonoGameLibrary.Behavior;
// using TribeBuild.Tasks;
// using TribeBuild.World;
// //using TribeBuild.Logging;

// namespace TribeBuild.Entity.NPC
// {
//     public class HunterAI : NPC
//     {
//         // Combat stats
//         public float AttackDamage { get; set; }
//         public float AttackRange { get; set; }
//         public float AttackCooldown { get; set; }
//         private float attackTimer = 0f;
        
//         // Hunting
//         public Task CurrentTask { get; set; }
//         public List<string> HuntableAnimals { get; set; }
        
//         // Home & Storage
//         public Vector2 HomePosition { get; set; }
//         public Inventory Inventory { get; set; }
        
//         // Combat state
//         public bool IsUnderAttack { get; set; }
//         public Entity Attacker { get; set; }
        
//         public HunterAI()
//         {
//             Speed = 100f;
//             MaxEnergy = 120f;
//             Energy = 120f;
//             AttackDamage = 20f;
//             AttackRange = 50f;
//             AttackCooldown = 1.5f;
            
//             Inventory = new Inventory(8);
//             HuntableAnimals = new List<string> { "Deer", "Rabbit", "Boar" };
            
//             InitializeBehaviorTree();
            
//            // GameLogger.Instance?.Info("NPC", "Created Hunter AI");
//         }
        
//         protected override void InitializeBehaviorTree()
//         {
//             var builder = new BehaviorTreeBuilder();
            
//             behaviorTree = builder
//                 .Selector("Hunter Root")
//                     // 1. COMBAT
//                     .Selector("Combat Response")
//                         .Sequence("Counter Attack")
//                             .Condition(ctx => IsUnderAttack, "Been Attacked?")
//                             .Selector("Fight or Flight")
//                                 .Sequence("Fight Back")
//                                     .Condition(ctx => Energy > 40f && Attacker != null, "Strong Enough?")
//                                     .Action(FightAttackerAction, "Fight")
//                                 .End()
//                                 .Action(TacticalRetreatAction, "Retreat")
//                             .End()
//                         .End()
//                     .End()
                    
//                     // 2. CRITICAL NEEDS
//                     .Sequence("Critical Needs")
//                         .Condition(ctx => Energy < 20f, "Critical Energy?")
//                         .Action(GoHomeAndRestAction, "Go Home & Rest")
//                     .End()
                    
//                     // 3. ENERGY MANAGEMENT
//                     .Sequence("Energy Management")
//                         .Condition(ctx => Energy < 50f, "Low Energy?")
//                         .Selector("Restore Energy")
//                             .Sequence("Eat if Have Food")
//                                 .Condition(ctx => Inventory.HasFood(), "Have Food?")
//                                 .Action(EatAction, "Eat")
//                             .End()
//                             .Action(HuntForFoodAction, "Hunt for Food")
//                         .End()
//                     .End()
                    
//                     // 4. HUNTING
//                     .Selector("Hunting Activities")
//                         // Execute current hunting task
//                         .Sequence("Hunt Current Target")
//                             .Condition(ctx => CurrentTask != null && CurrentTask.Type == TaskType.Hunting && CurrentTask.Status != TaskStatus.Completed, "Has Hunting Task?")
//                             .Action(ExecuteHuntingTaskAction, "Hunt Target")
//                         .End()
                        
//                         // Return home if inventory full
//                         .Sequence("Return with Loot")
//                             .Condition(ctx => Inventory.IsFull(), "Inventory Full?")
//                             .Action(GoHomeAndDepositAction, "Return Home")
//                         .End()
                        
//                         // Find new prey
//                         .Action(FindPreyAction, "Find Prey")
//                     .End()
                    
//                     // 5. IDLE
//                     .Action(WanderAction, "Wander")
//                 .End()
//                 .Build("Hunter Behavior Tree");
//         }
        
//         public override void Update(GameTime gameTime, NPCBody body)
//         {
//             base.Update(gameTime, body);
            
//             if (attackTimer > 0f)
//             {
//                 attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
//             }
//         }
        
//         // ==================== ACTIONS ====================
        
//         private NodeState FightAttackerAction(BehaviorContext ctx)
//         {
//             var body = ctx.GetTarget<NPCBody>();
            
//             if (Attacker == null || !Attacker.IsActive)
//             {
//                 IsUnderAttack = false;
//                 Attacker = null;
//                 return NodeState.Success;
//             }
            
//             float distance = Vector2.Distance(body.Position, Attacker.Position);
            
//             if (distance > AttackRange)
//             {
//                 // Move closer
//                 if (!body.IsMoving())
//                 {
//                     var pathfinder = GameManager.Instance.World.Pathfinder;
//                     var path = pathfinder.FindPath(body.Position, Attacker.Position);
                    
//                     if (path != null)
//                     {
//                         body.PathFollower.SetPath(path);
//                     }
//                 }
                
//                 ChangeState(NPCState.Moving);
//                 return NodeState.Running;
//             }
            
//             // Attack
//             ChangeState(NPCState.Fighting);
            
//             if (attackTimer <= 0f)
//             {
//                 PerformAttack(Attacker);
//                 attackTimer = AttackCooldown;
//             }
            
//             return NodeState.Running;
//         }
        
//         private NodeState TacticalRetreatAction(BehaviorContext ctx)
//         {
//             var body = ctx.GetTarget<NPCBody>();
            
//             if (Attacker == null)
//             {IsUnderAttack = false;
//             return NodeState.Success;
//         }
        
//         Vector2 retreatDirection = HomePosition - Attacker.Position;
//         retreatDirection.Normalize();
        
//         Vector2 retreatPoint = body.Position + retreatDirection * 150f;
        
//         if (!body.IsMoving())
//         {
//             var pathfinder = GameManager.Instance.World.Pathfinder;
//             var path = pathfinder.FindPath(body.Position, retreatPoint);
            
//             if (path != null)
//             {
//                 body.PathFollower.SetPath(path);
//             }
//         }
        
//         ChangeState(NPCState.Fleeing);
        
//         float distance = Vector2.Distance(body.Position, Attacker.Position);
//         if (distance > 180f)
//         {
//             IsUnderAttack = false;
//             Attacker = null;
//             return NodeState.Success;
//         }
        
//         return NodeState.Running;
//     }
    
//     private NodeState GoHomeAndRestAction(BehaviorContext ctx)
//     {
//         var body = ctx.GetTarget<NPCBody>();
        
//         float distance = Vector2.Distance(body.Position, HomePosition);
        
//         if (distance < 15f)
//         {
//             body.Stop();
//             Energy += 25f * (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;
//             ChangeState(NPCState.Sleeping);
            
//             if (Energy >= MaxEnergy * 0.7f)
//             {
//                 return NodeState.Success;
//             }
            
//             return NodeState.Running;
//         }
        
//         if (!body.IsMoving())
//         {
//             var pathfinder = GameManager.Instance.World.Pathfinder;
//             var path = pathfinder.FindPath(body.Position, HomePosition);
            
//             if (path != null)
//             {
//                 body.PathFollower.SetPath(path);
//             }
//         }
        
//         ChangeState(NPCState.Moving);
//         return NodeState.Running;
//     }
    
//     private NodeState EatAction(BehaviorContext ctx)
//     {
//         if (Inventory.ConsumeFood())
//         {
//             Energy += 40f;
//             Hunger -= 50f;
//             ChangeState(NPCState.Eating);
//             return NodeState.Success;
//         }
        
//         return NodeState.Failure;
//     }
    
//     private NodeState HuntForFoodAction(BehaviorContext ctx)
//     {
//         // Hunt for food (small animals)
//         return FindPreyAction(ctx);
//     }
    
//     private NodeState ExecuteHuntingTaskAction(BehaviorContext ctx)
//     {
//         var body = ctx.GetTarget<NPCBody>();
        
//         if (CurrentTask == null || !CurrentTask.IsValid())
//         {
//             CurrentTask = null;
//             return NodeState.Success;
//         }
        
//         //var huntingTask = CurrentTask as HuntingTask;
//         if (huntingTask == null)
//         {
//             CurrentTask = null;
//             return NodeState.Success;
//         }
        
//         // Update work location
//         if (huntingTask.Target != null && huntingTask.Target.IsActive)
//         {
//             huntingTask.WorkLocation = huntingTask.Target.Position;
            
//             float distance = Vector2.Distance(body.Position, huntingTask.Target.Position);
            
//             if (distance > AttackRange + 10f)
//             {
//                 // Chase target
//                 if (!body.IsMoving() || body.PathFollower.GetRemainingDistance(body.Position) < 20f)
//                 {
//                     var pathfinder = GameManager.Instance.World.Pathfinder;
//                     var path = pathfinder.FindPath(body.Position, huntingTask.Target.Position);
                    
//                     if (path != null)
//                     {
//                         body.PathFollower.SetPath(path);
//                     }
//                 }
                
//                 ChangeState(NPCState.Moving);
//                 return NodeState.Running;
//             }
//         }
        
//         // Execute hunting task
//         ChangeState(NPCState.Fighting);
        
//         bool taskCompleted = CurrentTask.Execute(ctx.GameTime, body);
        
//         if (taskCompleted)
//         {
//             CurrentTask = null;
//             return NodeState.Success;
//         }
        
//         return NodeState.Running;
//     }
    
//     private NodeState GoHomeAndDepositAction(BehaviorContext ctx)
//     {
//         var body = ctx.GetTarget<NPCBody>();
        
//         float distance = Vector2.Distance(body.Position, HomePosition);
        
//         if (distance < 15f)
//         {
//             Inventory.Clear();
//             return NodeState.Success;
//         }
        
//         if (!body.IsMoving())
//         {
//             var pathfinder = GameManager.Instance.World.Pathfinder;
//             var path = pathfinder.FindPath(body.Position, HomePosition);
            
//             if (path != null)
//             {
//                 body.PathFollower.SetPath(path);
//             }
//         }
        
//         ChangeState(NPCState.Moving);
//         return NodeState.Running;
//     }
    
//     private NodeState FindPreyAction(BehaviorContext ctx)
//     {
//         var body = ctx.GetTarget<NPCBody>();
        
//         // TODO: Use spatial index to find nearest animal
//         // For now, return failure
//         return NodeState.Failure;
//     }
    
//     private NodeState WanderAction(BehaviorContext ctx)
//     {
//         var body = ctx.GetTarget<NPCBody>();
        
//         var wanderTarget = ctx.GetData<Vector2?>("wanderTarget");
//         var wanderTimer = ctx.GetData<float>("wanderTimer");
        
//         wanderTimer -= (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;
        
//         if (wanderTimer <= 0f || !wanderTarget.HasValue)
//         {
//             var random = new Random();
//             float angle = (float)(random.NextDouble() * Math.PI * 2);
//             float distance = 50f + (float)(random.NextDouble() * 100f);
            
//             wanderTarget = body.Position + new Vector2(
//                 (float)Math.Cos(angle) * distance,
//                 (float)Math.Sin(angle) * distance
//             );
            
//             wanderTimer = 4f + (float)(random.NextDouble() * 6f);
            
//             ctx.SetData("wanderTarget", wanderTarget);
//             ctx.SetData("wanderTimer", wanderTimer);
//         }
        
//         if (wanderTarget.HasValue)
//         {
//             float dist = Vector2.Distance(body.Position, wanderTarget.Value);
            
//             if (dist < 15f)
//             {
//                 ctx.SetData("wanderTarget", (Vector2?)null);
//                 ChangeState(NPCState.Idle);
//             }
//             else if (!body.IsMoving())
//             {
//                 var pathfinder = GameManager.Instance.World.Pathfinder;
//                 var path = pathfinder.FindPath(body.Position, wanderTarget.Value);
                
//                 if (path != null)
//                 {
//                     body.PathFollower.SetPath(path);
//                 }
                
//                 ChangeState(NPCState.Moving);
//             }
//         }
        
//         return NodeState.Running;
//     }
    
//     // ==================== HELPER METHODS ====================
    
//     private void PerformAttack(Entity target)
//     {
//         // TODO: Implement damage system
//         Energy -= 5f;
        
//        // GameLogger.Instance?.LogCombat(0, target.ID, AttackDamage);
//     }
    
//     public void OnAttacked(Entity attacker)
//     {
//         IsUnderAttack = true;
//         Attacker = attacker;
//     }
//     }
// }