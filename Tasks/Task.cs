using System;
using Microsoft.Xna.Framework;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC;
// using TribeBuild.Logging;

namespace TribeBuild.Tasks
{
    public enum TaskType
    {
        Harvest, 
        Mining,
        Hunting,
        Idle
    }
    public enum TaskStatus
    {
        Pending,        // Đang chờ
        InProgress,     // Đang thực hiện
        Completed,      // Hoàn thành
        Failed,         // Thất bại
        Cancelled       // Bị hủy
    }
    public abstract class Task
    {
        public int TaskID { get; private set; }
        public TaskType Type { get; protected set; }
        public TaskStatus Status { get; set; }
        
        public Vector2? WorkLocation { get; set; }
        public float Duration { get; protected set; }
        public float Progress { get; protected set; }
        
        public string RequiredJobType { get; protected set; }
        public int Priority { get; set; }
        
        private static int nextID = 0;
        
        protected Task(TaskType type, string requiredJobType = "Any")
        {
            TaskID = nextID++;
            Type = type;
            Status = TaskStatus.Pending;
            RequiredJobType = requiredJobType;
            Priority = 1;
            Progress = 0f;
        }
        
        /// <summary>
        /// Check if task is still valid
        /// </summary>
        public abstract bool IsValid();
        
        /// <summary>
        /// Execute task for one frame
        /// </summary>
        public abstract bool Execute(GameTime gameTime, NPCBody worker);
        
        /// <summary>
        /// Called when task is completed
        /// </summary>
        public abstract void OnComplete(NPCBody worker);
        
        /// <summary>
        /// Called when task is cancelled
        /// </summary>
        public virtual void OnCancel()
        {
            Status = TaskStatus.Cancelled;
        }
        
        /// <summary>
        /// Get progress percentage (0-1)
        /// </summary>
        public float GetProgressPercent()
        {
            if (Duration <= 0) return 0f;
            return MathHelper.Clamp(Progress / Duration, 0f, 1f);
        }
    }
}