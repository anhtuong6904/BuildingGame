using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.Enemies;

namespace TribeBuild.World
{
    /// <summary>
    /// ✅ Quản lý chu kỳ ngày/đêm theo phong cách Stardew Valley
    /// - 6:00 AM: Bắt đầu ngày mới, động vật spawn
    /// - 19:30 PM (dusk): Động vật bắt đầu về nhà
    /// - 20:00 PM: Bắt đầu đêm, night enemies spawn
    /// - 2:00 AM: Dừng game, hiện màn hình tổng kết
    /// - 6:00 AM: Ngày mới bắt đầu
    /// </summary>
    public class DayNightCycleManager
    {
        private static DayNightCycleManager instance;
        public static DayNightCycleManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new DayNightCycleManager();
                return instance;
            }
        }

        // Time settings (in game hours)
        public const float DAY_START = 6f;      // 6:00 AM
        public const float DUSK_START = 19.5f;  // 7:30 PM
        public const float NIGHT_START = 20f;   // 8:00 PM
        public const float DAY_END = 2f;        // 2:00 AM (next day)
        
        // Day length in real seconds (from 6 AM to 2 AM = 20 hours game time)
        public float DayLengthInSeconds = 1f * 60f; // 15 minutes real time = 1 day (20 game hours)
        
        // Difficulty scaling
        private int baseAnimalCount = 5;
        private int baseEnemyCount = 3;
        private float difficultyMultiplier = 1.0f;
        
        // Current state
        public enum TimeOfDayPhase
        {
            Morning,    // 6:00 - 12:00
            Afternoon,  // 12:00 - 17:00
            Evening,    // 17:00 - 20:00
            Night,      // 20:00 - 2:00
            Summary     // 2:00 - waiting for player input
        }
        
        public TimeOfDayPhase CurrentPhase { get; private set; }
        private TimeOfDayPhase lastPhase;
        
        // Events
        public event Action OnDayStart;
        public event Action OnDuskStart;
        public event Action OnNightStart;
        public event Action OnDayEnd;
        public event Action<DaySummary> OnShowDaySummary;
        
        // Flags
        private bool hasSpawnedAnimals = false;
        private bool hasSpawnedEnemies = false;
        private bool hasDespawnedAnimals = false;
        private bool hasDespawnedEnemies = false;
        private bool isDaySummaryShowing = false;
        
        // References
        private GameManager gameManager;
        private SpawnZoneManager spawnManager;
        
        // Day statistics
        private DaySummary currentDaySummary;

        private DayNightCycleManager()
        {
            CurrentPhase = TimeOfDayPhase.Morning;
            lastPhase = TimeOfDayPhase.Morning;
        }

        public void Initialize(GameManager gm)
        {
            gameManager = gm;
            spawnManager = SpawnZoneManager.Instance;
            
            currentDaySummary = new DaySummary();
            
            Console.WriteLine("[DayNightCycle] ✅ Initialized");
        }

        public void Update(GameTime gameTime)
        {
            if (gameManager == null) return;
            
            float timeOfDay = gameManager.TimeOfDay;
            
            // Update phase
            UpdatePhase(timeOfDay);
            
            // Handle phase transitions
            if (CurrentPhase != lastPhase)
            {
                OnPhaseChanged(lastPhase, CurrentPhase);
                lastPhase = CurrentPhase;
            }
            
            // Handle specific times
            HandleTimeEvents(timeOfDay);
        }

        private void UpdatePhase(float timeOfDay)
        {
            if (isDaySummaryShowing)
            {
                CurrentPhase = TimeOfDayPhase.Summary;
                return;
            }

            // Handle time wrapping (0-2 AM is night, then 6-24 is day/evening/night)
            if (timeOfDay >= 0f && timeOfDay < DAY_START)
            {
                // 12 AM - 6 AM = Night
                CurrentPhase = TimeOfDayPhase.Night;
            }
            else if (timeOfDay >= DAY_START && timeOfDay < 12f)
            {
                CurrentPhase = TimeOfDayPhase.Morning;
            }
            else if (timeOfDay >= 12f && timeOfDay < 17f)
            {
                CurrentPhase = TimeOfDayPhase.Afternoon;
            }
            else if (timeOfDay >= 17f && timeOfDay < NIGHT_START)
            {
                CurrentPhase = TimeOfDayPhase.Evening;
            }
            else // 20:00 - 23:59
            {
                CurrentPhase = TimeOfDayPhase.Night;
            }
        }

        private void OnPhaseChanged(TimeOfDayPhase from, TimeOfDayPhase to)
        {
            Console.WriteLine($"[DayNightCycle] Phase changed: {from} → {to}");
            
            switch (to)
            {
                case TimeOfDayPhase.Morning:
                    Console.WriteLine($"[DayNightCycle] ☀️ MORNING - Day {gameManager.CurrentDay}");
                    break;
                    
                case TimeOfDayPhase.Evening:
                    Console.WriteLine("[DayNightCycle] 🌆 EVENING - Animals returning home...");
                    OnDuskStart?.Invoke();
                    break;
                    
                case TimeOfDayPhase.Night:
                    Console.WriteLine("[DayNightCycle] 🌙 NIGHT - Enemies spawning...");
                    OnNightStart?.Invoke();
                    break;
            }
        }

        private void HandleTimeEvents(float timeOfDay)
        {
            // 6:00 AM - Dawn - Spawn animals
            if (timeOfDay >= DAY_START && timeOfDay < DAY_START + 0.5f)
            {
                if (!hasSpawnedAnimals)
                {
                    SpawnAnimalsAtDawn();
                    hasSpawnedAnimals = true;
                    hasDespawnedAnimals = false; // Reset for next cycle
                }
            }
            else if (timeOfDay >= DAY_START + 0.5f && timeOfDay < DUSK_START)
            {
                // Reset flag during day
                hasSpawnedAnimals = false;
            }
            
            // 19:30 PM - Dusk - Animals start returning home
            if (timeOfDay >= DUSK_START && timeOfDay < DUSK_START + 0.5f)
            {
                if (!hasDespawnedAnimals)
                {
                    // Animals will automatically return home due to their AI
                    hasDespawnedAnimals = true;
                }
            }
            
            // 20:00 PM - Night - Spawn enemies
            if (timeOfDay >= NIGHT_START && timeOfDay < NIGHT_START + 0.5f)
            {
                if (!hasSpawnedEnemies)
                {
                    SpawnNightEnemies();
                    hasSpawnedEnemies = true;
                    hasDespawnedEnemies = false;
                }
            }
            else if (timeOfDay >= NIGHT_START + 0.5f && timeOfDay < 23.5f)
            {
                // Reset flag during night
                hasSpawnedEnemies = false;
            }
            
            // 6:00 AM next day - Despawn enemies (happens at dawn)
            if (timeOfDay >= DAY_START && timeOfDay < DAY_START + 0.5f)
            {
                if (!hasDespawnedEnemies)
                {
                    DespawnNightEnemies();
                    hasDespawnedEnemies = true;
                }
            }
            
            // ✅ 2:00 AM - End day, show summary
            if (timeOfDay >= DAY_END && timeOfDay < DAY_END + 0.1f)
            {
                if (!isDaySummaryShowing)
                {
                    EndDay();
                }
            }
        }

        /// <summary>
        /// ✅ Spawn animals at dawn with difficulty scaling
        /// </summary>
        private void SpawnAnimalsAtDawn()
        {
            Console.WriteLine("[DayNightCycle] 🐔 Spawning animals at dawn...");
            
            // Trigger respawn event
            OnDayStart?.Invoke();
            
            // ✅ Calculate scaled animal count
            int currentDay = gameManager.CurrentDay;
            int scaledAnimalCount = CalculateScaledAnimalCount(currentDay);
            
            Console.WriteLine($"[DayNightCycle] Day {currentDay}: Spawning {scaledAnimalCount} animals (base: {baseAnimalCount})");
            
            // ✅ Update spawn zones with new counts
            var grassZones = spawnManager.GetZonesByType(SpawnType.PassiveAnimal);
            foreach (var zone in grassZones)
            {
                zone.SpawnCount = scaledAnimalCount;
            }
            
            // Queue animal spawns with updated counts
            spawnManager.QueueScaledAnimalSpawns(scaledAnimalCount);
        }

        /// <summary>
        /// ✅ Spawn night enemies at 8 PM with scaling difficulty
        /// </summary>
        private void SpawnNightEnemies()
        {
            Console.WriteLine("[DayNightCycle] 👹 Spawning night enemies...");
            
            var world = gameManager?.World;
            if (world == null) return;
            
            var enemyZones = spawnManager.GetZonesByType(SpawnType.NightEnemy);
            
            Random rng = new Random();
            int totalSpawned = 0;
            
            // ✅ Calculate scaled enemy count based on day
            int currentDay = gameManager.CurrentDay;
            int scaledEnemyCount = CalculateScaledEnemyCount(currentDay);
            
            Console.WriteLine($"[DayNightCycle] Day {currentDay}: Spawning {scaledEnemyCount} enemies (base: {baseEnemyCount})");
            
            foreach (var zone in enemyZones)
            {
                // Override spawn count with scaled value
                int enemiesToSpawn = scaledEnemyCount;
                
                for (int i = 0; i < enemiesToSpawn; i++)
                {
                    // Random position within zone
                    Vector2 spawnPos = new Vector2(
                        zone.Bounds.X + rng.Next(zone.Bounds.Width),
                        zone.Bounds.Y + rng.Next(zone.Bounds.Height)
                    );
                    
                    // Validate position
                    var atlas = world.GetAtlas("assassin");
                    if (atlas != null)
                    {
                        var enemy = world.SpawnNightEnemy(
                            spawnPos, 
                            NightEnemyType.Assassin, 
                            atlas
                        );
                        
                        if (enemy != null)
                            totalSpawned++;
                    }
                }
            }
            
            Console.WriteLine($"[DayNightCycle] ✅ Spawned {totalSpawned} night enemies (difficulty: x{difficultyMultiplier:F2})");
        }

        /// <summary>
        /// ✅ Despawn night enemies at dawn
        /// </summary>
        private void DespawnNightEnemies()
        {
            Console.WriteLine("[DayNightCycle] 🌅 Despawning night enemies...");
            
            var world = gameManager?.World;
            if (world == null) return;
            
            var enemies = world.GetEntitiesOfType<NightEnemyEntity>();
            foreach (var enemy in enemies)
            {
                // Enemies will fade out automatically in their Update
                // due to IsNightTime() check
            }
        }

        /// <summary>
        /// ✅ End day at 2 AM, show summary
        /// </summary>
        private void EndDay()
        {
            isDaySummaryShowing = true;
            gameManager.PauseGame();
            
            // Finalize day summary
            currentDaySummary.DayNumber = gameManager.CurrentDay;
            currentDaySummary.TimeSpent = DayLengthInSeconds;
            currentDaySummary.WoodCollected = gameManager.WoodCollected;
            currentDaySummary.StoneCollected = gameManager.StoneCollected;
            currentDaySummary.FoodCollected = gameManager.FoodCollected;
            
            Console.WriteLine("\n========== DAY SUMMARY ==========");
            Console.WriteLine($"Day {currentDaySummary.DayNumber} Complete!");
            Console.WriteLine($"Wood Collected: {currentDaySummary.WoodCollected}");
            Console.WriteLine($"Stone Collected: {currentDaySummary.StoneCollected}");
            Console.WriteLine($"Food Collected: {currentDaySummary.FoodCollected}");
            Console.WriteLine($"Enemies Killed: {currentDaySummary.EnemiesKilled}");
            Console.WriteLine("=================================\n");
            
            OnDayEnd?.Invoke();
            OnShowDaySummary?.Invoke(currentDaySummary);
        }

        /// <summary>
        /// ✅ Called when player confirms day summary (presses button)
        /// </summary>
        public void StartNewDay()
        {
            if (!isDaySummaryShowing) return;
            
            Console.WriteLine("[DayNightCycle] ▶️ Starting new day...");
            
            // Reset flags
            isDaySummaryShowing = false;
            hasSpawnedAnimals = false;
            hasSpawnedEnemies = false;
            hasDespawnedAnimals = false;
            hasDespawnedEnemies = false;
            
            // ✅ Increase difficulty for next day
            gameManager.CurrentDay++;
            UpdateDifficultyScaling(gameManager.CurrentDay);
            
            // ✅ Reset time to 6 AM (start of new day cycle)
            gameManager.TimeOfDay = DAY_START;
            
            // Reset day summary
            currentDaySummary = new DaySummary();
            
            // ✅ Trigger respawn with increased counts
            SpawnAnimalsAtDawn();
            
            // Resume game
            gameManager.ResumeGame();
            
            Console.WriteLine($"[DayNightCycle] ☀️ Day {gameManager.CurrentDay} started! (Difficulty: x{difficultyMultiplier:F2})");
        }

        /// <summary>
        /// ✅ Calculate difficulty scaling based on day
        /// </summary>
        private void UpdateDifficultyScaling(int day)
        {
            // Difficulty increases every 3 days
            // Day 1-3: 1.0x
            // Day 4-6: 1.2x
            // Day 7-9: 1.5x
            // Day 10+: 2.0x
            
            if (day <= 3)
                difficultyMultiplier = 1.0f;
            else if (day <= 6)
                difficultyMultiplier = 1.2f;
            else if (day <= 9)
                difficultyMultiplier = 1.5f;
            else if (day <= 15)
                difficultyMultiplier = 2.0f;
            else
                difficultyMultiplier = 2.5f;
            
            Console.WriteLine($"[DayNightCycle] Difficulty updated for Day {day}: x{difficultyMultiplier:F2}");
        }

        /// <summary>
        /// ✅ Calculate scaled animal count
        /// </summary>
        private int CalculateScaledAnimalCount(int day)
        {
            return (int)(baseAnimalCount * difficultyMultiplier);
        }

        /// <summary>
        /// ✅ Calculate scaled enemy count
        /// </summary>
        private int CalculateScaledEnemyCount(int day)
        {
            return (int)(baseEnemyCount * difficultyMultiplier);
        }

        /// <summary>
        /// Track resource collected
        /// </summary>
        public void OnResourceCollected(string resourceType, int amount)
        {
            currentDaySummary.TotalResourcesCollected += amount;
        }

        /// <summary>
        /// Track enemy killed
        /// </summary>
        public void OnEnemyKilled()
        {
            currentDaySummary.EnemiesKilled++;
        }

        /// <summary>
        /// Check if it's safe to be outside (not night)
        /// </summary>
        public bool IsSafeTime()
        {
            return CurrentPhase != TimeOfDayPhase.Night;
        }

        /// <summary>
        /// Get time remaining until day end
        /// </summary>
        public float GetTimeUntilDayEnd()
        {
            float timeOfDay = gameManager.TimeOfDay;
            if (timeOfDay >= DAY_END)
                return 0f;
            return DAY_END - timeOfDay;
        }

        /// <summary>
        /// Get formatted time string
        /// </summary>
        public string GetTimeString()
        {
            float time = gameManager.TimeOfDay;
            
            int hours = (int)time;
            int minutes = (int)((time - hours) * 60);
            
            // Handle AM/PM display
            string period = hours >= 12 && hours < 24 ? "PM" : "AM";
            int displayHour = hours % 12;
            if (displayHour == 0) displayHour = 12;
            
            return $"{displayHour:D2}:{minutes:D2} {period}";
        }
    }

    /// <summary>
    /// Thống kê ngày
    /// </summary>
    public class DaySummary
    {
        public int DayNumber { get; set; }
        public float TimeSpent { get; set; }
        
        // Resources
        public int WoodCollected { get; set; }
        public int StoneCollected { get; set; }
        public int FoodCollected { get; set; }
        public int TotalResourcesCollected { get; set; }
        
        // Combat
        public int EnemiesKilled { get; set; }
        public int DamageTaken { get; set; }
        
        // Bonus info
        public bool SurvivedNight { get; set; } = true;
        
        public DaySummary()
        {
            DayNumber = 1;
            TimeSpent = 0f;
            WoodCollected = 0;
            StoneCollected = 0;
            FoodCollected = 0;
            TotalResourcesCollected = 0;
            EnemiesKilled = 0;
            DamageTaken = 0;
        }
    }
}