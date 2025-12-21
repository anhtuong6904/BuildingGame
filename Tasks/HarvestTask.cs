using Microsoft.Xna.Framework;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.Resource;


namespace TribeBuild.Tasks
{
    public class HarvestTask:Task
    {
        public ResourceEntity Target {get; private set;}
        public HarvestTask (ResourceEntity target, string requiredJobType = "Any") : base(TaskType.Harvest, requiredJobType)
        {
            Target = target;
            WorkLocation = target.Position;
            Duration = 5f;
            //GameLogger.Instance?.Debug("Task", $"Created harvest task for {target.GetType().Name} #{target.ID}");
        }

        public override bool IsValid()
        {
            return Target != null && Target.IsActive && !Target.IsBeingHarvested;
        }

        public override bool Execute(GameTime gameTime, NPCBody worker)
        {
            if(!IsValid())
            {
                Status = TaskStatus.Failed;
                return true;
            }
            if (!Target.IsBeingHarvested)
            {
                Target.Interact(worker);
                Status = TaskStatus.InProgress;
                // GameLogger.Instance?.LogNPCAction(
                //     worker.ID,
                //     "Villager",
                //     $"Started harvesting {Target.YieldItem}",
                //     $"at ({Target.Position.X:F0}, {Target.Position.Y:F0})"
                // );
            }
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Progress += deltaTime;

            float damagePerSecond = Target.MaxHealth / Duration;
            Target.Harvest(damagePerSecond * deltaTime);
            if(Progress >= Duration || Target.Health <= 0)
            {
                OnComplete(worker);
                return true;
            }
            return false;
        }

        public override void OnComplete(NPCBody worker)
        {
            Status = TaskStatus.Completed;
            var villager = worker.AI as VillagerAI;
            if(villager != null)
            {
                for(int i = 0; i < Target.YieldAmount; i++)
                {
                    villager.Inventory.AddItem(Target.YieldItem);
                }
                // GameLogger.Instance?.LogResourceCollected(
                //     Target.YieldItem,
                //     Target.YieldAmount,
                //     worker.ID
                // );
            }
             //GameManager.Instance.OnResourceCollected(Target.YieldItem, Target.YieldAmount);
            Target.StopHarvest();
            //GameLogger.Instance?.LogJobCompleted("Harvest", worker.ID, Progress);
        }
    }
}