using Microsoft.Xna.Framework;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC;
//using TribeBuild.Logging;

namespace TribeBuild.Tasks
{
    /// <summary>
    /// Task to mine in a mine building
    /// </summary>
    public class MiningTask : Task
    {
        public Mine Target { get; private set; }
        private bool hasStarted;
        private MineWorker mineWorker;
        
        public MiningTask(Mine mine) 
            : base(TaskType.Mining, "Miner")
        {
            Target = mine;
            WorkLocation = mine.EntrancePosition;
            Duration = mine.MiningDuration;
            hasStarted = false;
            
            //GameLogger.Instance?.Debug("Task", $"Created mining task at mine #{mine.ID}");
        }
        
        public override bool IsValid()
        {
            return Target != null && Target.IsActive && !Target.IsFull();
        }
        
        public override bool Execute(GameTime gameTime, NPCBody worker)
        {
            if (!IsValid())
            {
                Status = TaskStatus.Failed;
                return true;
            }
            
            // Start mining
            if (!hasStarted)
            {
                if (Target.StartMining(worker))
                {
                    hasStarted = true;
                    Status = TaskStatus.InProgress;
                    
                    // Get reference to mine worker
                    mineWorker = Target.CurrentWorkers.Find(w => w.NPC == worker);
                    
                    //GameLogger.Instance?.LogNPCAction(
                    //     worker.ID,
                    //     "Miner",
                    //     "Entered mine",
                    //     $"Duration: {Duration}s"
                    // );
                }
                else
                {
                    // Mine is full, fail task
                    Status = TaskStatus.Failed;
                    return true;
                }
            }
            
            // Check if mining is complete
            if (mineWorker != null && !mineWorker.IsWorking)
            {
                OnComplete(worker);
                return true;
            }
            
            // Update progress
            Progress = mineWorker?.WorkProgress ?? 0f;
            
            return false; // Still mining
        }
        
        public override void OnComplete(NPCBody worker)
        {
            Status = TaskStatus.Completed;
            
            // Rewards already added by Mine system
            //GameLogger.Instance?.LogJobCompleted("Mining", worker.ID, Progress);
        }
    }
}