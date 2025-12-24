using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.Enemies;

namespace TribeBuild.World
{
    /// <summary>
    /// ✅ Spawn manager with day/night cycle support
    /// - Batch spawning to prevent lag
    /// - Poisson Disk Sampling for better distribution
    /// - Auto respawn animals at dawn
    /// </summary>
    public class SpawnZoneManager
    {
        private static readonly object lockObject = new object();
        private static SpawnZoneManager instance;
        
        public static SpawnZoneManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new SpawnZoneManager();
                            Console.WriteLine("[SpawnZoneManager] ✅ Singleton instance created");
                        }
                    }
                }
                return instance;
            }
        }

        private GameWorld world;
        private Tilemap tilemap;
        private Dictionary<string, TextureAtlas> atlases;
        private Dictionary<string, List<SpawnZone>> zones;
        
        // Batch spawning
        private Queue<SpawnJob> spawnQueue;
        private const int MAX_SPAWNS_PER_FRAME = 5;
        
        // ✅ Day/Night cycle tracking
        private Dictionary<SpawnZone, List<DespawnedAnimal>> despawnedAnimals;
        private bool hasRespawnedToday = false;
        
        private Random random;

        private struct SpawnJob
        {
            public Vector2 Position;
            public SpawnType Type;
            public string SubType;
            public SpawnZone Zone; // ✅ Track which zone this spawn belongs to
        }

        // ✅ Track despawned animals for respawn
        private struct DespawnedAnimal
        {
            public Vector2 SpawnPosition;
            public AnimalType Type;
            public string SubType;
        }

        private SpawnZoneManager()
        {
            atlases = new Dictionary<string, TextureAtlas>();
            zones = new Dictionary<string, List<SpawnZone>>();
            spawnQueue = new Queue<SpawnJob>();
            despawnedAnimals = new Dictionary<SpawnZone, List<DespawnedAnimal>>();
            random = new Random();
        }

        public void Initialize(GameWorld gameWorld, Tilemap map)
        {
            world = gameWorld;
            tilemap = map;
            Console.WriteLine("[SpawnZoneManager] Initialized with world and tilemap");
        }

        public void RegisterAtlas(string name, TextureAtlas atlas)
        {
            atlases[name] = atlas;
            Console.WriteLine($"[SpawnZoneManager] Registered atlas: {name}");
        }

        public void ParseObjectLayer(string layerName)
        {
            var objectLayer = tilemap.GetObjectLayer(layerName);
            if (objectLayer == null)
            {
                Console.WriteLine($"[SpawnZoneManager] ⚠️ WARNING: Object layer '{layerName}' not found!");
                return;
            }

            foreach (var obj in objectLayer.Objects)
            {
                string zoneName = obj.Name.ToLower();
                
                SpawnZone zone = new SpawnZone
                {
                    Name = obj.Name,
                    Bounds = new Rectangle(
                        (int)(obj.Position.X * tilemap.Scale.X),
                        (int)(obj.Position.Y * tilemap.Scale.Y),
                        (int)(obj.Width * tilemap.Scale.X),
                        (int)(obj.Height * tilemap.Scale.Y)
                    )
                };

                // Determine zone type
                if (zoneName.Contains("forest"))
                {
                    zone.Type = SpawnType.Tree;
                    zone.SpawnCount = 30;
                }
                else if (zoneName.Contains("grass"))
                {
                    zone.Type = SpawnType.PassiveAnimal;
                    zone.SpawnCount = 5;
                }
                else if (zoneName.Contains("riverside"))
                {
                    zone.Type = SpawnType.Bush;
                    zone.SpawnCount = 10;
                }
                else if (zoneName.Contains("enemies"))
                {
                    zone.Type = SpawnType.NightEnemy;
                    zone.SpawnCount = 4;
                }

                if (!zones.ContainsKey(zoneName))
                    zones[zoneName] = new List<SpawnZone>();
                
                zones[zoneName].Add(zone);
                
                // ✅ Initialize despawn tracking for animal zones
                if (zone.Type == SpawnType.PassiveAnimal || zone.Type == SpawnType.AggressiveAnimal)
                {
                    despawnedAnimals[zone] = new List<DespawnedAnimal>();
                }
                
                Console.WriteLine($"[SpawnZone] ✅ Added '{obj.Name}' at ({zone.Bounds.X}, {zone.Bounds.Y}) " +
                    $"size {zone.Bounds.Width}x{zone.Bounds.Height}, type: {zone.Type}, count: {zone.SpawnCount}");
            }
        }
        /// <summary>
        /// ✅ Queue animal spawns with scaled count for difficulty
        /// </summary>
        public void QueueScaledAnimalSpawns(int totalCount)
        {
            var grassZones = GetZonesByType(SpawnType.PassiveAnimal);
            if (grassZones.Count == 0) return;
            
            // Distribute animals across all grass zones
            int animalsPerZone = totalCount / grassZones.Count;
            int remainder = totalCount % grassZones.Count;
            
            Random rng = new Random();
            string[] animalTypes = { "chicken", "sheep", "boar" };
            
            foreach (var zone in grassZones)
            {
                int count = animalsPerZone + (remainder > 0 ? 1 : 0);
                if (remainder > 0) remainder--;
                
                var spawnPoints = GenerateSpawnPoints(zone.Bounds, count, minSpacing: 80f);
                
                foreach (var point in spawnPoints)
                {
                    string animalType = animalTypes[rng.Next(animalTypes.Length)];
                    
                    spawnQueue.Enqueue(new SpawnJob
                    {
                        Position = point,
                        Type = SpawnType.PassiveAnimal,
                        SubType = animalType,
                        Zone = zone
                    });
                }
                
                Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count} animals in '{zone.Name}'");
            }
        }

        public void SpawnInitialEntities()
        {
            Console.WriteLine("[SpawnZoneManager] 🚀 Starting optimized initial spawn...");
            
            foreach (var zoneGroup in zones.Values)
            {
                foreach (var zone in zoneGroup)
                {
                    switch (zone.Type)
                    {
                        case SpawnType.Tree:
                            QueueTreeSpawns(zone);
                            break;
                        
                        case SpawnType.Bush:
                            QueueBushSpawns(zone);
                            break;
                        
                        case SpawnType.PassiveAnimal:
                            QueueAnimalSpawns(zone);
                            break;
                        
                        case SpawnType.AggressiveAnimal:
                            QueueAggressiveAnimalSpawns(zone);
                            break;
                        
                        case SpawnType.NightEnemy:
                            // Don't spawn night enemies during day
                            break;
                    }
                }
            }
            
            Console.WriteLine($"[SpawnZoneManager] ✅ Queued {spawnQueue.Count} entities for spawning");
        }

        private List<Vector2> GenerateSpawnPoints(Rectangle bounds, int count, float minSpacing = 48f)
        {
            var sampler = new PoissonDiskSampler(minSpacing, random);
            var points = sampler.GeneratePoints(bounds, count * 3);
            
            var validPoints = new List<Vector2>();
            
            foreach (var point in points)
            {
                Point tile = tilemap.WorldToTile(point);
                
                if (!tilemap.IsTileWalkable(tile.X, tile.Y))
                    continue;
                
                if (tilemap.IsWaterTile(tile.X, tile.Y))
                    continue;
                
                validPoints.Add(point);
                
                if (validPoints.Count >= count)
                    break;
            }
            
            return validPoints;
        }

        private void QueueTreeSpawns(SpawnZone zone)
        {
            var spawnPoints = GenerateSpawnPoints(zone.Bounds, zone.SpawnCount, minSpacing: 64f);
            
            foreach (var point in spawnPoints)
            {
                Point tile = tilemap.WorldToTile(point);
                Vector2 snappedPos = tilemap.TileToWorld(tile.X, tile.Y);
                
                spawnQueue.Enqueue(new SpawnJob
                {
                    Position = snappedPos,
                    Type = SpawnType.Tree,
                    SubType = "oak",
                    Zone = zone
                });
            }
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} trees in '{zone.Name}'");
        }

        private void QueueBushSpawns(SpawnZone zone)
        {
            var spawnPoints = GenerateSpawnPoints(zone.Bounds, zone.SpawnCount, minSpacing: 48f);
            
            foreach (var point in spawnPoints)
            {
                Point tile = tilemap.WorldToTile(point);
                Vector2 snappedPos = tilemap.TileToWorld(tile.X, tile.Y);
                
                spawnQueue.Enqueue(new SpawnJob
                {
                    Position = snappedPos,
                    Type = SpawnType.Bush,
                    SubType = "berry",
                    Zone = zone
                });
            }
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} bushes in '{zone.Name}'");
        }

        private void QueueAnimalSpawns(SpawnZone zone)
        {
            var spawnPoints = GenerateSpawnPoints(zone.Bounds, zone.SpawnCount, minSpacing: 80f);
            
            string[] animalTypes = { "chicken", "sheep" };
            
            foreach (var point in spawnPoints)
            {
                string animalType = animalTypes[random.Next(animalTypes.Length)];
                
                spawnQueue.Enqueue(new SpawnJob
                {
                    Position = point,
                    Type = SpawnType.PassiveAnimal,
                    SubType = animalType,
                    Zone = zone
                });
            }
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} animals in '{zone.Name}'");
        }

        private void QueueAggressiveAnimalSpawns(SpawnZone zone)
        {
            var spawnPoints = GenerateSpawnPoints(zone.Bounds, zone.SpawnCount, minSpacing: 100f);
            
            foreach (var point in spawnPoints)
            {
                spawnQueue.Enqueue(new SpawnJob
                {
                    Position = point,
                    Type = SpawnType.AggressiveAnimal,
                    SubType = "boar",
                    Zone = zone
                });
            }
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} boars in '{zone.Name}'");
        }

        /// <summary>
        /// ✅ Update with day/night cycle management
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Process spawn queue
            ProcessSpawnQueue();
            
            // ✅ Handle dawn respawn
            CheckDawnRespawn();
        }

        private void ProcessSpawnQueue()
        {
            if (spawnQueue.Count == 0)
                return;
            
            int spawned = 0;
            
            while (spawnQueue.Count > 0 && spawned < MAX_SPAWNS_PER_FRAME)
            {
                var job = spawnQueue.Dequeue();
                SpawnEntity(job);
                spawned++;
            }
            
            if (spawnQueue.Count > 0 && spawnQueue.Count % 50 == 0)
            {
                Console.WriteLine($"[SpawnZoneManager] ⏳ {spawnQueue.Count} entities remaining...");
            }
            else if (spawnQueue.Count == 0 && spawned > 0)
            {
                Console.WriteLine($"[SpawnZoneManager] ✅ All entities spawned!");
            }
        }

        /// <summary>
        /// ✅ Check for dawn and respawn animals
        /// </summary>
        private void CheckDawnRespawn()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            float timeOfDay = gameManager.TimeOfDay;

            // Dawn is between 6:00 and 7:00
            if (timeOfDay >= 6f && timeOfDay < 7f)
            {
                if (!hasRespawnedToday)
                {
                    RespawnAnimalsAtDawn();
                    hasRespawnedToday = true;
                }
            }
            else if (timeOfDay >= 7f)
            {
                // Reset flag after dawn period
                hasRespawnedToday = false;
            }
        }

        /// <summary>
        /// ✅ Respawn all despawned animals at dawn
        /// </summary>
        private void RespawnAnimalsAtDawn()
        {
            Console.WriteLine("[SpawnZoneManager] ☀️ DAWN - Respawning animals...");

            int totalRespawned = 0;

            foreach (var kvp in despawnedAnimals)
            {
                SpawnZone zone = kvp.Key;
                List<DespawnedAnimal> animals = kvp.Value;

                foreach (var animal in animals)
                {
                    SpawnAnimalWithZone(animal.SpawnPosition, animal.SubType, zone);
                    totalRespawned++;
                }

                // Clear the despawned list
                animals.Clear();
            }

            Console.WriteLine($"[SpawnZoneManager] ✅ Respawned {totalRespawned} animals at dawn");
        }

        /// <summary>
        /// ✅ Called when an animal despawns at night
        /// </summary>
        public void OnAnimalDespawned(PassiveAnimal animal, SpawnZone zone)
        {
            if (!despawnedAnimals.ContainsKey(zone))
                despawnedAnimals[zone] = new List<DespawnedAnimal>();

            despawnedAnimals[zone].Add(new DespawnedAnimal
            {
                SpawnPosition = animal.Position,
                Type = animal.Type,
                SubType = animal.Type.ToString().ToLower()
            });

            Console.WriteLine($"[SpawnZoneManager] 🌙 Tracked despawned {animal.Type} for respawn");
        }

        /// <summary>
        /// ✅ Called when an aggressive animal despawns
        /// </summary>
        public void OnAggressiveAnimalDespawned(AggressiveAnimal animal, SpawnZone zone)
        {
            if (!despawnedAnimals.ContainsKey(zone))
                despawnedAnimals[zone] = new List<DespawnedAnimal>();

            despawnedAnimals[zone].Add(new DespawnedAnimal
            {
                SpawnPosition = animal.Position,
                Type = animal.Type,
                SubType = animal.Type.ToString().ToLower()
            });
        }

        private void SpawnEntity(SpawnJob job)
        {
            switch (job.Type)
            {
                case SpawnType.Tree:
                    SpawnTree(job.Position);
                    break;
                
                case SpawnType.Bush:
                    SpawnBush(job.Position);
                    break;
                
                case SpawnType.PassiveAnimal:
                    SpawnAnimalWithZone(job.Position, job.SubType, job.Zone);
                    break;
                
                case SpawnType.AggressiveAnimal:
                    SpawnAggressiveAnimalWithZone(job.Position, job.SubType, job.Zone);
                    break;
                
                case SpawnType.NightEnemy:
                    SpawnNightEnemy(job.Position, job.SubType);
                    break;
            }
        }

        private void SpawnTree(Vector2 position)
        {
            var atlas = atlases.ContainsKey("resource") ? atlases["resource"] : null;
            if (atlas == null) return;
            
            world.SpawnTree(position, TreeType.Oak, atlas);
        }

        private void SpawnBush(Vector2 position)
        {
            var atlas = atlases.ContainsKey("resource") ? atlases["resource"] : null;
            if (atlas == null) return;
            
            world.SpawnBush(position, BushType.Berry, atlas);
        }

        /// <summary>
        /// ✅ FIXED: Spawn passive animal with zone reference
        /// </summary>
        private void SpawnAnimalWithZone(Vector2 position, string subType, SpawnZone zone)
        {
            AnimalType type = subType switch
            {
                "chicken" => AnimalType.Chicken,
                "sheep" => AnimalType.Sheep,
                _ => AnimalType.Chicken
            };
            
            var atlas = atlases.ContainsKey(subType) ? atlases[subType] : null;
            if (atlas == null)
            {
                Console.WriteLine($"[SpawnZoneManager] ⚠️ WARNING: '{subType}' atlas not registered!");
                return;
            }

            // ✅ Spawn and get reference
            var animal = world.SpawnPassiveAnimal(position, type, atlas);
            
            // ✅ Set spawn zone on the animal
            if (animal != null)
            {
                animal.SetSpawnZone(zone.Bounds);
            }
        }

        /// <summary>
        /// ✅ Spawn aggressive animal with zone reference
        /// </summary>
        private void SpawnAggressiveAnimalWithZone(Vector2 position, string subType, SpawnZone zone)
        {
            var atlas = atlases.ContainsKey(subType) ? atlases[subType] : null;
            if (atlas == null) return;
            
            var animal = world.SpawnAggressiveAnimal(position, AnimalType.Boar, atlas);
            
            if (animal != null)
            {
                animal.SetSpawnZone(zone.Bounds);
            }
        }

        private void SpawnNightEnemy(Vector2 position, string subType)
        {
            NightEnemyType type = subType switch
            {
                "assassin" => NightEnemyType.Assassin,
                _ => NightEnemyType.Assassin
            };
            
            var atlas = atlases.ContainsKey(subType) ? atlases[subType] : null;
            if (atlas == null) return;
            
            world.SpawnNightEnemy(position, type, atlas);
        }

        public List<SpawnZone> GetZonesByType(SpawnType type)
        {
            return zones.Values
                .SelectMany(list => list)
                .Where(z => z.Type == type)
                .ToList();
        }
        
        public int GetQueuedEntityCount()
        {
            return spawnQueue.Count;
        }
    }

    // ==================== HELPER CLASSES ====================

    public class SpawnZone
    {
        public string Name { get; set; }
        public Rectangle Bounds { get; set; }
        public SpawnType Type { get; set; }
        public int SpawnCount { get; set; }

        
    }

    public enum SpawnType
    {
        Tree,
        Bush,
        PassiveAnimal,
        AggressiveAnimal,
        NightEnemy
    }

    public class PoissonDiskSampler
    {
        private Random random;
        private float minDistance;
        private const int REJECTION_LIMIT = 30;
        
        public PoissonDiskSampler(float minDistance, Random rng = null)
        {
            this.minDistance = minDistance;
            this.random = rng ?? new Random();
        }
        
        public List<Vector2> GeneratePoints(Rectangle bounds, int maxPoints)
        {
            List<Vector2> points = new List<Vector2>();
            List<Vector2> activeList = new List<Vector2>();
            
            Vector2 first = new Vector2(
                bounds.X + (float)random.NextDouble() * bounds.Width,
                bounds.Y + (float)random.NextDouble() * bounds.Height
            );
            points.Add(first);
            activeList.Add(first);
            
            while (activeList.Count > 0 && points.Count < maxPoints)
            {
                int index = random.Next(activeList.Count);
                Vector2 point = activeList[index];
                bool found = false;
                
                for (int i = 0; i < REJECTION_LIMIT; i++)
                {
                    float angle = (float)(random.NextDouble() * Math.PI * 2);
                    float radius = minDistance + (float)(random.NextDouble() * minDistance);
                    
                    Vector2 candidate = point + new Vector2(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius
                    );
                    
                    if (!bounds.Contains(candidate))
                        continue;
                    
                    if (!IsValidPoint(candidate, points, minDistance))
                        continue;
                    
                    points.Add(candidate);
                    activeList.Add(candidate);
                    found = true;
                    break;
                }
                
                if (!found)
                    activeList.RemoveAt(index);
            }
            
            return points;
        }
        
        private bool IsValidPoint(Vector2 candidate, List<Vector2> existingPoints, float minDist)
        {
            float minDistSq = minDist * minDist;
            
            foreach (var point in existingPoints)
            {
                if (Vector2.DistanceSquared(candidate, point) < minDistSq)
                    return false;
            }
            
            return true;
        }
    }
}