using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TribeBuild.Entity.NPC;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC.Animals;

namespace TribeBuild.Tasks
{
    /// <summary>
    /// Manages all tasks in the game - assigning, executing, and tracking them
    /// </summary>
    public class TaskManager
    {
        private static TaskManager instance;
        public static TaskManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new TaskManager();
                return instance;
            }
        }

        // Task queues by type
        private List<Task> allTasks;
        private Dictionary<int, Task> npcAssignedTasks; // NPCBody.ID -> Task
        
        // Statistics
        public int CompletedTasksCount { get; private set; }
        public int FailedTasksCount { get; private set; }
        public int CancelledTasksCount { get; private set; }

        private TaskManager()
        {
            allTasks = new List<Task>();
            npcAssignedTasks = new Dictionary<int, Task>();
            CompletedTasksCount = 0;
            FailedTasksCount = 0;
            CancelledTasksCount = 0;
        }

        /// <summary>
        /// Add a new task to the queue
        /// </summary>
        public void AddTask(Task task)
        {
            if (task == null) return;
            
            allTasks.Add(task);
            //GameLogger.Instance?.Debug("TaskManager", $"Added {task.Type} task #{task.TaskID}");
        }

        /// <summary>
        /// Create and add a harvest task
        /// </summary>
        public HarvestTask CreateHarvestTask(ResourceEntity target, string requiredJobType = "Any")
        {
            var task = new HarvestTask(target, requiredJobType);
            AddTask(task);
            return task;
        }

        /// <summary>
        /// Create and add a mining task
        /// </summary>
        public MiningTask CreateMiningTask(Mine mine)
        {
            var task = new MiningTask(mine);
            AddTask(task);
            return task;
        }

        /// <summary>
        /// Create and add a hunting task
        /// </summary>
        public HuntingTask CreateHuntingTask(AnimalEntity animal)
        {
            var task = new HuntingTask(animal);
            AddTask(task);
            return task;
        }

        /// <summary>
        /// Get next available task for a worker
        /// </summary>
        public Task GetNextTask(NPCBody worker, string jobType)
        {
            // Check if worker already has a task
            if (npcAssignedTasks.ContainsKey(worker.ID))
            {
                var currentTask = npcAssignedTasks[worker.ID];
                if (currentTask.Status == TaskStatus.InProgress || 
                    currentTask.Status == TaskStatus.Pending)
                {
                    return currentTask;
                }
            }

            // Find suitable pending task
            var suitableTask = allTasks
                .Where(t => t.Status == TaskStatus.Pending)
                .Where(t => t.IsValid())
                .Where(t => t.RequiredJobType == "Any" || t.RequiredJobType == jobType)
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();

            if (suitableTask != null)
            {
                AssignTaskToWorker(suitableTask, worker);
            }

            return suitableTask;
        }

        /// <summary>
        /// Assign a specific task to a worker
        /// </summary>
        public void AssignTaskToWorker(Task task, NPCBody worker)
        {
            if (npcAssignedTasks.ContainsKey(worker.ID))
            {
                var oldTask = npcAssignedTasks[worker.ID];
                if (oldTask != task && oldTask.Status == TaskStatus.InProgress)
                {
                    oldTask.OnCancel();
                }
            }

            npcAssignedTasks[worker.ID] = task;
            //GameLogger.Instance?.Debug("TaskManager", $"Assigned {task.Type} task #{task.TaskID} to NPC #{worker.ID}");
        }

        /// <summary>
        /// Execute task for a worker
        /// </summary>
        public bool ExecuteTask(GameTime gameTime, NPCBody worker, Task task)
        {
            if (task == null) return true;

            // Check if task is still valid
            if (!task.IsValid())
            {
                task.Status = TaskStatus.Failed;
                FailedTasksCount++;
                RemoveTaskAssignment(worker.ID);
                return true;
            }

            // Execute the task
            bool isCompleted = task.Execute(gameTime, worker);

            if (isCompleted)
            {
                if (task.Status == TaskStatus.Completed)
                {
                    CompletedTasksCount++;
                    //GameLogger.Instance?.Debug("TaskManager", $"Task #{task.TaskID} completed by NPC #{worker.ID}");
                }
                else if (task.Status == TaskStatus.Failed)
                {
                    FailedTasksCount++;
                    //GameLogger.Instance?.Warning("TaskManager", $"Task #{task.TaskID} failed for NPC #{worker.ID}");
                }

                RemoveTaskAssignment(worker.ID);
            }

            return isCompleted;
        }

        /// <summary>
        /// Cancel a task
        /// </summary>
        public void CancelTask(Task task)
        {
            if (task == null) return;

            task.OnCancel();
            CancelledTasksCount++;

            // Remove assignment
            var workerID = npcAssignedTasks.FirstOrDefault(x => x.Value == task).Key;
            if (workerID != 0)
            {
                npcAssignedTasks.Remove(workerID);
            }

            //GameLogger.Instance?.Debug("TaskManager", $"Cancelled task #{task.TaskID}");
        }

        /// <summary>
        /// Cancel all tasks assigned to a worker
        /// </summary>
        public void CancelWorkerTasks(int workerID)
        {
            if (npcAssignedTasks.ContainsKey(workerID))
            {
                var task = npcAssignedTasks[workerID];
                CancelTask(task);
            }
        }

        /// <summary>
        /// Get current task for a worker
        /// </summary>
        public Task GetWorkerTask(int workerID)
        {
            return npcAssignedTasks.ContainsKey(workerID) ? npcAssignedTasks[workerID] : null;
        }

        /// <summary>
        /// Remove task assignment
        /// </summary>
        private void RemoveTaskAssignment(int workerID)
        {
            npcAssignedTasks.Remove(workerID);
        }

        /// <summary>
        /// Update and cleanup tasks
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Remove completed/failed/cancelled tasks
            allTasks.RemoveAll(t => 
                t.Status == TaskStatus.Completed || 
                t.Status == TaskStatus.Failed || 
                t.Status == TaskStatus.Cancelled);

            // Check for invalid tasks
            var invalidTasks = allTasks.Where(t => !t.IsValid()).ToList();
            foreach (var task in invalidTasks)
            {
                task.Status = TaskStatus.Failed;
                FailedTasksCount++;
                
                // Remove worker assignment
                var workerID = npcAssignedTasks.FirstOrDefault(x => x.Value == task).Key;
                if (workerID != 0)
                {
                    npcAssignedTasks.Remove(workerID);
                }
            }
        }

        /// <summary>
        /// Get all pending tasks of a specific type
        /// </summary>
        public List<Task> GetPendingTasks(TaskType? type = null)
        {
            var query = allTasks.Where(t => t.Status == TaskStatus.Pending);
            
            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);
            
            return query.ToList();
        }

        /// <summary>
        /// Get all tasks in progress
        /// </summary>
        public List<Task> GetInProgressTasks()
        {
            return allTasks.Where(t => t.Status == TaskStatus.InProgress).ToList();
        }

        /// <summary>
        /// Get task count by status
        /// </summary>
        public int GetTaskCount(TaskStatus status)
        {
            return allTasks.Count(t => t.Status == status);
        }

        /// <summary>
        /// Clear all tasks
        /// </summary>
        public void ClearAllTasks()
        {
            foreach (var task in allTasks)
            {
                task.OnCancel();
            }
            
            allTasks.Clear();
            npcAssignedTasks.Clear();
            
            //GameLogger.Instance?.Debug("TaskManager", "Cleared all tasks");
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStatistics()
        {
            CompletedTasksCount = 0;
            FailedTasksCount = 0;
            CancelledTasksCount = 0;
        }
    }
}