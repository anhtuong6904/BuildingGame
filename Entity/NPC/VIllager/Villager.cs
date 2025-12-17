using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Behavior;
using TribeBuild.Tasks;
using TribeBuild.Entity.Resource;
//using TribeBuild.Logging;

namespace TribeBuild.Entity.NPC
{
    public class VillagerAI : NPC
    {
        // Job & Task
        public string JobType { get; set; }
        public Task CurrentTask { get; set; }
        
        // Home
        public Vector2 HomePosition { get; set; }
        
        // Combat
        public bool IsUnderAttack { get; set; }
        public Entity Attacker { get; set; }
        
        // Inventory
        public Inventory Inventory { get; set; }
        
        // Work schedule
        private float workTime = 0f;
        private float shiftEndTime = 480f; // 8 minutes (8 game hours)
        
        // Reference to body
        private NPCBody body;
        
        public VillagerAI(string jobType = "Farmer")
        {
            JobType = jobType;
            Inventory = new Inventory(10);
            Speed = 60f;
            MaxEnergy = 80f;
            Energy = 80f;
            
            InitializeBehaviorTree();
            
            //GameLogger.Instance?.Info("NPC", $"Created Villager AI: {jobType}");
        }
        
        protected override void InitializeBehaviorTree()
        {
            var builder = new BehaviorTreeBuilder();
            
            behaviorTree = builder
                .Selector("Villager Root")
                    // 1. DANGER - Run away when attacked
                    .Sequence("Danger Response")
                        .Condition(ctx => IsUnderAttack, "Been Attacked?")
                        .Action(RunAwayAction, "Run to Safety")
                    .End()
                    
                    // 2. CRITICAL ENERGY
                    .Sequence("Critical Energy")
                        .Condition(ctx => Energy < 15f, "Critical Energy?")
                        .Action(GoHomeAndRestAction, "Go Home & Rest")
                    .End()
                    
                    // 3. ENERGY MANAGEMENT
                    .Sequence("Energy Management")
                        .Condition(ctx => Energy < 40f, "Low Energy?")
                        .Selector("Restore Energy")
                            .Sequence("Eat if Have Food")
                                .Condition(ctx => Inventory.HasFood(), "Have Food?")
                                .Action(EatAction, "Eat")
                            .End()
                            .Action(FindFoodAction, "Find Food")
                        .End()
                    .End()
                    
                    // 4. WORK
                    .Selector("Work Activities")
                        // Do current task
                        .Sequence("Current Task")
                            .Condition(ctx => CurrentTask != null && CurrentTask.Status != TaskStatus.Completed, "Has Task?")
                            .Action(ExecuteTaskAction, "Execute Task")
                        .End()
                        
                        // Return home if should
                        .Sequence("Return Home")
                            .Condition(ShouldGoHome, "Should Go Home?")
                            .Action(GoHomeAndDepositAction, "Go Home & Deposit")
                        .End()
                        
                        // Request new task
                        .Action(RequestTaskAction, "Request Task")
                    .End()
                    
                    // 5. IDLE
                    .Action(WanderAroundHomeAction, "Wander")
                .End()
                .Build("Villager Behavior Tree");
        }
        
        public override void Update(GameTime gameTime, NPCBody npcBody)
        {
            body = npcBody;
            base.Update(gameTime, npcBody);
            
            if (CurrentState == NPCState.Working)
            {
                workTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
        
        // ==================== ACTIONS ====================
        
        private NodeState RunAwayAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            if (Attacker == null || !Attacker.IsActive)
            {
                IsUnderAttack = false;
                return NodeState.Success;
            }
            
            // Run towards home
            Vector2 safeDirection = HomePosition - npcBody.Position;
            
            if (safeDirection.LengthSquared() < 100f)
            {
                safeDirection = npcBody.Position - Attacker.Position;
            }
            
            safeDirection.Normalize();
            Vector2 escapePoint = npcBody.Position + safeDirection * 250f;
            
            // Use pathfinding from GameWorld
            var path = npcBody.World.FindPath(npcBody.Position, escapePoint);
            
            if (path != null)
            {
                npcBody.PathFollower.SetPath(path);
            }
            
            ChangeState(NPCState.Fleeing);
            
            float distance = Vector2.Distance(npcBody.Position, Attacker.Position);
            if (distance > 200f)
            {
                IsUnderAttack = false;
                Attacker = null;
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
        
        private NodeState GoHomeAndRestAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            float distance = Vector2.Distance(npcBody.Position, HomePosition);
            
            if (distance < 15f)
            {
                // Rest at home
                npcBody.Stop();
                Energy += 25f * (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;
                ChangeState(NPCState.Sleeping);
                
                if (Energy >= MaxEnergy * 0.7f)
                {
                    return NodeState.Success;
                }
                
                return NodeState.Running;
            }
            
            // Move to home with pathfinding
            if (!npcBody.IsMoving())
            {
                var path = npcBody.World.FindPath(npcBody.Position, HomePosition);
                
                if (path != null)
                {
                    npcBody.PathFollower.SetPath(path);
                }
            }
            
            ChangeState(NPCState.Moving);
            return NodeState.Running;
        }
        
        private NodeState EatAction(BehaviorContext ctx)
        {
            if (Inventory.ConsumeFood())
            {
                Energy += 30f;
                Hunger -= 40f;
                ChangeState(NPCState.Eating);
                
                //GameLogger.Instance?.Debug("NPC", $"Villager ate food, energy: {Energy:F0}");
                return NodeState.Success;
            }
            
            return NodeState.Failure;
        }
        
        private NodeState FindFoodAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            // Find nearest berry bush
            var bush = ResourceManager.Instance.FindNearestBush(npcBody.Position, 500f);
            
            if (bush != null)
            {
                // Create harvest task for the bush
                var task = TaskManager.Instance.CreateHarvestTask(bush, "Any");
                CurrentTask = task;
                TaskManager.Instance.AssignTaskToWorker(task, npcBody);
                
                //GameLogger.Instance?.Debug("NPC", $"Villager found food at bush #{bush.ID}");
                return NodeState.Success;
            }
            
            return NodeState.Failure;
        }
        
        private NodeState ExecuteTaskAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            if (CurrentTask == null || !CurrentTask.IsValid())
            {
                CompleteTask(npcBody);
                return NodeState.Success;
            }
            
            // Check if at work location
            if (CurrentTask.WorkLocation.HasValue)
            {
                float distance = Vector2.Distance(npcBody.Position, CurrentTask.WorkLocation.Value);
                
                if (distance > 25f)
                {
                    // Move to work location
                    if (!npcBody.IsMoving())
                    {
                        var path = npcBody.World.FindPath(npcBody.Position, CurrentTask.WorkLocation.Value);
                        
                        if (path != null)
                        {
                            npcBody.PathFollower.SetPath(path);
                            //GameLogger.Instance?.Debug("Task", $"Villager #{npcBody.ID} moving to task location");
                        }
                        else
                        {
                            //GameLogger.Instance?.Warning("Task", $"No path found for villager #{npcBody.ID}");
                            CurrentTask.Status = TaskStatus.Failed;
                            CompleteTask(npcBody);
                            return NodeState.Success;
                        }
                    }
                    
                    ChangeState(NPCState.Moving);
                    return NodeState.Running;
                }
            }
            
            // Execute task using TaskManager
            ChangeState(NPCState.Working);
            
            bool taskCompleted = TaskManager.Instance.ExecuteTask(ctx.GameTime, npcBody, CurrentTask);
            
            if (taskCompleted)
            {
                CompleteTask(npcBody);
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
        
        private bool ShouldGoHome(BehaviorContext ctx)
        {
            bool inventoryFull = Inventory.IsFull();
            bool tooTired = Energy < 25f;
            bool shiftEnded = workTime >= shiftEndTime;
            
            return inventoryFull || tooTired || shiftEnded;
        }
        
        private NodeState GoHomeAndDepositAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            float distance = Vector2.Distance(npcBody.Position, HomePosition);
            
            if (distance < 15f)
            {
                // Deposit items
                DepositItems();
                workTime = 0f;
                ChangeState(NPCState.Idle);
                return NodeState.Success;
            }
            
            // Move to home
            if (!npcBody.IsMoving())
            {
                var path = npcBody.World.FindPath(npcBody.Position, HomePosition);
                
                if (path != null)
                {
                    npcBody.PathFollower.SetPath(path);
                }
            }
            
            ChangeState(NPCState.Moving);
            return NodeState.Running;
        }
        
        private NodeState RequestTaskAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            // Request task from TaskManager
            var task = TaskManager.Instance.GetNextTask(npcBody, JobType);
            
            if (task != null)
            {
                CurrentTask = task;
                
                //GameLogger.Instance?.LogJobAssigned(
                //     task.Type.ToString(),
                //     npcBody.ID,
                //     task.WorkLocation ?? Vector2.Zero
                // );
                
                return NodeState.Success;
            }
            
            // If no task available, try to find resources to harvest
            if (JobType == "Farmer" || JobType == "Any")
            {
                var tree = ResourceManager.Instance.FindNearestTree(npcBody.Position, 300f);
                if (tree != null)
                {
                    var harvestTask = TaskManager.Instance.CreateHarvestTask(tree, JobType);
                    CurrentTask = harvestTask;
                    TaskManager.Instance.AssignTaskToWorker(harvestTask, npcBody);
                    return NodeState.Success;
                }
            }
            
            return NodeState.Failure;
        }
        
        private NodeState WanderAroundHomeAction(BehaviorContext ctx)
        {
            var npcBody = ctx.GetTarget<NPCBody>();
            
            var wanderTarget = ctx.GetData<Vector2?>("wanderTarget");
            var wanderTimer = ctx.GetData<float>("wanderTimer");
            
            wanderTimer -= (float)ctx.GameTime.ElapsedGameTime.TotalSeconds;
            
            if (wanderTimer <= 0f || !wanderTarget.HasValue)
            {
                var random = new Random();
                float angle = (float)(random.NextDouble() * Math.PI * 2);
                float distance = 20f + (float)(random.NextDouble() * 50f);
                
                wanderTarget = HomePosition + new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );
                
                wanderTimer = 3f + (float)(random.NextDouble() * 5f);
                
                ctx.SetData("wanderTarget", wanderTarget);
                ctx.SetData("wanderTimer", wanderTimer);
            }
            
            if (wanderTarget.HasValue)
            {
                float dist = Vector2.Distance(npcBody.Position, wanderTarget.Value);
                
                if (dist < 10f)
                {
                    ctx.SetData("wanderTarget", (Vector2?)null);
                    ChangeState(NPCState.Idle);
                }
                else if (!npcBody.IsMoving())
                {
                    var path = npcBody.World.FindPath(npcBody.Position, wanderTarget.Value);
                    
                    if (path != null)
                    {
                        npcBody.PathFollower.SetPath(path);
                    }
                    
                    ChangeState(NPCState.Moving);
                }
            }
            
            return NodeState.Running;
        }
        
        // ==================== HELPER METHODS ====================
        
        public void AssignTask(Task task)
        {
            CurrentTask = task;
            workTime = 0f;
        }
        
        public void CompleteTask(NPCBody npcBody)
        {
            if (CurrentTask != null && npcBody != null)
            {
                // Task completion is handled by TaskManager
                TaskManager.Instance.ExecuteTask(new GameTime(), npcBody, CurrentTask);
            }
            CurrentTask = null;
        }
        
        public void DepositItems()
        {
            // TODO: Implement resource storage system
            Inventory.Clear();
            //GameLogger.Instance?.Debug("NPC", "Villager deposited items at home");
        }
        
        public void OnAttacked(Entity attacker)
        {
            IsUnderAttack = true;
            Attacker = attacker;
            
            //GameLogger.Instance?.LogCombat(attacker?.ID ?? -1, 0, 0);
        }
    }
}