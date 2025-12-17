using System;
using Microsoft.Xna.Framework;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.NPC;
//using TribeBuild.Logging;

namespace TribeBuild.Tasks
{
    /// <summary>
    /// Task to hunt animals
    /// </summary>
    public class HuntingTask : Task
    {
        public AnimalEntity Target { get; private set; }
        private float attackCooldown = 1.5f;
        private float attackTimer = 0f;
        
        public HuntingTask(AnimalEntity target) 
            : base(TaskType.Hunting, "Hunter")
        {
            Target = target;
            WorkLocation = target.Position;
            Duration = 10f; // Estimated duration
            
            //GameLogger.Instance?.Debug("Task", $"Created hunting task for {target.Type}");
        }
        
        public override bool IsValid()
        {
            return Target != null && Target.IsActive;
        }
        
        public override bool Execute(GameTime gameTime, NPCBody worker)
        {
            if (!IsValid())
            {
                Status = TaskStatus.Failed;
                return true;
            }
            
            Status = TaskStatus.InProgress;
            
            var hunter = worker.AI as HunterAI;
            if (hunter == null)
            {
                Status = TaskStatus.Failed;
                return true;
            }
            
            // Update work location (target moves)
            WorkLocation = Target.Position;
            
            float distance = Vector2.Distance(worker.Position, Target.Position);
            
            // Move closer if too far
            if (distance > hunter.AttackRange)
            {
                // Check if target is too far - abandon hunt
                if (distance > 300f)
                {
                    //GameLogger.Instance?.Warning("Task", $"Hunter #{worker.ID} abandoned hunt - target too far");
                    Status = TaskStatus.Failed;
                    return true;
                }
                
                // Keep chasing
                return false;
            }
            
            // Attack
            attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (attackTimer <= 0f)
            {
                // Perform attack
                Target.TakeDamage(hunter.AttackDamage, worker);
                attackTimer = attackCooldown;
                
                //GameLogger.Instance?.Debug("Task", $"Hunter #{worker.ID} attacked {Target.Type}");
                
                // Check if target is dead
                if (!Target.IsActive || Target.Health <= 0)
                {
                    OnComplete(worker);
                    return true;
                }
            }
            
            return false; // Still hunting
        }
        
        public override void OnComplete(NPCBody worker)
        {
            Status = TaskStatus.Completed;
            
            // Collect loot
            var hunter = worker.AI as HunterAI;
            if (hunter != null && Target.LootItems != null)
            {
                foreach (var lootItem in Target.LootItems)
                {
                    for (int i = 0; i < Target.LootAmount; i++)
                    {
                        hunter.Inventory.AddItem(lootItem);
                    }
                }
                
                //GameLogger.Instance?.GameEvent("Hunting", $"Hunter #{worker.ID} killed {Target.Type}, looted {Target.LootAmount} items");
            }
            
            //GameLogger.Instance?.LogJobCompleted("Hunting", worker.ID, Progress);
        }
    }
}