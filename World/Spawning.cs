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
    /// ✅ IMPROVED: Natural spawn distribution with biome-appropriate density
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
        private const int MAX_SPAWNS_PER_FRAME = 3; // ✅ Reduced for smoother performance
        
        // Day/Night cycle tracking
        private Dictionary<SpawnZone, List<DespawnedAnimal>> despawnedAnimals;
        private bool hasRespawnedToday = false;
        
        private Random random;

        private struct SpawnJob
        {
            public Vector2 Position;
            public SpawnType Type;
            public string SubType;
            public SpawnZone Zone;
        }

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

        /// <summary>
        /// ✅ IMPROVED: Parse zones with intelligent spawn counts
        /// </summary>
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

                // ✅ Calculate zone area for density-based spawning
                float areaInTiles = (zone.Bounds.Width / tilemap.ScaleTileWidth) * 
                                   (zone.Bounds.Height / tilemap.ScaleTileHeight);

                // ✅ Determine zone type with appropriate density
                if (zoneName.Contains("forest"))
                {
                    zone.Type = SpawnType.Tree;
                    // Dense forest: 1 tree per 3 tiles
                    zone.SpawnCount = Math.Max(5, (int)(areaInTiles / 3f));
                }
                else if (zoneName.Contains("grass"))
                {
                    zone.Type = SpawnType.PassiveAnimal;
                    // Sparse animals: 1 per 20 tiles
                    zone.SpawnCount = Math.Max(2, (int)(areaInTiles / 20f));
                }
                else if (zoneName.Contains("riverside"))
                {
                    zone.Type = SpawnType.Bush;
                    // Medium density: 1 bush per 5 tiles
                    zone.SpawnCount = Math.Max(3, (int)(areaInTiles / 5f));
                }
                else if (zoneName.Contains("enemies"))
                {
                    zone.Type = SpawnType.NightEnemy;
                    // Low density: 1 enemy per 30 tiles
                    zone.SpawnCount = Math.Max(2, (int)(areaInTiles / 30f));
                }
                else if (zoneName.Contains("wild"))
                {
                    zone.Type = SpawnType.AggressiveAnimal;
                    // Very sparse: 1 per 40 tiles
                    zone.SpawnCount = Math.Max(1, (int)(areaInTiles / 40f));
                }

                if (!zones.ContainsKey(zoneName))
                    zones[zoneName] = new List<SpawnZone>();
                
                zones[zoneName].Add(zone);
                
                // Initialize despawn tracking for animal zones
                if (zone.Type == SpawnType.PassiveAnimal || zone.Type == SpawnType.AggressiveAnimal)
                {
                    despawnedAnimals[zone] = new List<DespawnedAnimal>();
                }
                
                Console.WriteLine($"[SpawnZone] ✅ '{obj.Name}' | Area: {areaInTiles:F0} tiles | " +
                    $"Type: {zone.Type} | Spawn Count: {zone.SpawnCount}");
            }
        }

        /// <summary>
        /// ✅ IMPROVED: Smart animal spawning with difficulty scaling
        /// </summary>
        public void QueueScaledAnimalSpawns(int totalCount)
        {
            var grassZones = GetZonesByType(SpawnType.PassiveAnimal);
            if (grassZones.Count == 0) return;
            
            // ✅ Distribute proportionally by zone area
            int totalArea = grassZones.Sum(z => z.Bounds.Width * z.Bounds.Height);
            
            string[] animalTypes = { "chicken", "sheep" };
            int spawned = 0;
            
            foreach (var zone in grassZones)
            {
                // ✅ Calculate how many animals this zone should get based on its area
                float zoneRatio = (float)(zone.Bounds.Width * zone.Bounds.Height) / totalArea;
                int zoneCount = (int)(totalCount * zoneRatio);
                zoneCount = Math.Max(1, zoneCount); // At least 1 per zone
                
                // ✅ Use clustered distribution for more natural look
                var spawnPoints = GenerateClusteredSpawnPoints(zone.Bounds, zoneCount, 
                    minSpacing: 60f, clusterSize: 2);
                
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
                    spawned++;
                }
                
                Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count} animals in '{zone.Name}' " +
                    $"(area ratio: {zoneRatio:P0})");
            }
            
            Console.WriteLine($"[SpawnZoneManager] ✅ Total animals queued: {spawned}");
        }

        public void SpawnInitialEntities()
        {
            Console.WriteLine("[SpawnZoneManager] 🚀 Starting natural spawn distribution...");
            
            int totalQueued = 0;
            
            foreach (var zoneGroup in zones.Values)
            {
                foreach (var zone in zoneGroup)
                {
                    int beforeCount = spawnQueue.Count;
                    
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
                    
                    int spawned = spawnQueue.Count - beforeCount;
                    totalQueued += spawned;
                }
            }
            
            Console.WriteLine($"[SpawnZoneManager] ✅ Queued {totalQueued} entities for natural spawning");
        }

        /// <summary>
        /// ✅ NEW: Generate spawn points with clustering for natural look
        /// </summary>
        private List<Vector2> GenerateClusteredSpawnPoints(Rectangle bounds, int count, 
            float minSpacing = 48f, int clusterSize = 3)
        {
            var points = new List<Vector2>();
            int clusters = Math.Max(1, count / clusterSize);
            
            // Generate cluster centers
            var clusterCenters = GenerateSpawnPoints(bounds, clusters, minSpacing * 2);
            
            // Generate points around each cluster
            foreach (var center in clusterCenters)
            {
                int pointsInCluster = Math.Min(clusterSize, count - points.Count);
                
                for (int i = 0; i < pointsInCluster; i++)
                {
                    // Spawn near cluster center with some randomness
                    float angle = (float)(random.NextDouble() * Math.PI * 2);
                    float distance = (float)(random.NextDouble() * minSpacing * 0.8f);
                    
                    Vector2 offset = new Vector2(
                        (float)Math.Cos(angle) * distance,
                        (float)Math.Sin(angle) * distance
                    );
                    
                    Vector2 candidate = center + offset;
                    
                    // Validate position
                    if (!bounds.Contains(candidate))
                        continue;
                    
                    Point tile = tilemap.WorldToTile(candidate);
                    if (!tilemap.IsTileWalkable(tile.X, tile.Y))
                        continue;
                    
                    if (tilemap.IsWaterTile(tile.X, tile.Y))
                        continue;
                    
                    points.Add(candidate);
                    
                    if (points.Count >= count)
                        break;
                }
                
                if (points.Count >= count)
                    break;
            }
            
            return points;
        }

        /// <summary>
        /// ✅ IMPROVED: Better distributed spawn points
        /// </summary>
        private List<Vector2> GenerateSpawnPoints(Rectangle bounds, int count, float minSpacing = 48f)
        {
            // ✅ Use enhanced Poisson Disk Sampling
            var sampler = new PoissonDiskSampler(minSpacing, random);
            var candidates = sampler.GeneratePoints(bounds, count * 4); // More candidates
            
            var validPoints = new List<Vector2>();
            
            foreach (var point in candidates)
            {
                Point tile = tilemap.WorldToTile(point);
                
                // Check if tile is valid
                if (tile.X < 0 || tile.X >= tilemap.Width || 
                    tile.Y < 0 || tile.Y >= tilemap.Height)
                    continue;
                
                if (!tilemap.IsTileWalkable(tile.X, tile.Y))
                    continue;
                
                if (tilemap.IsWaterTile(tile.X, tile.Y))
                    continue;
                
                validPoints.Add(point);
                
                if (validPoints.Count >= count)
                    break;
            }
            
            // ✅ If not enough points, try grid-based fallback
            if (validPoints.Count < count / 2)
            {
                Console.WriteLine($"[SpawnZone] ⚠️ Low spawn points, using grid fallback");
                validPoints.AddRange(GenerateGridSpawnPoints(bounds, count - validPoints.Count, minSpacing));
            }
            
            return validPoints;
        }

        /// <summary>
        /// ✅ NEW: Grid-based fallback for difficult zones
        /// </summary>
        private List<Vector2> GenerateGridSpawnPoints(Rectangle bounds, int count, float spacing)
        {
            var points = new List<Vector2>();
            int cols = (int)(bounds.Width / spacing);
            int rows = (int)(bounds.Height / spacing);
            
            // Create shuffled grid positions
            var positions = new List<(int x, int y)>();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    positions.Add((x, y));
                }
            }
            
            // Shuffle for randomness
            positions = positions.OrderBy(p => random.Next()).ToList();
            
            foreach (var (x, y) in positions)
            {
                Vector2 pos = new Vector2(
                    bounds.X + x * spacing + spacing / 2,
                    bounds.Y + y * spacing + spacing / 2
                );
                
                Point tile = tilemap.WorldToTile(pos);
                if (!tilemap.IsTileWalkable(tile.X, tile.Y))
                    continue;
                
                if (tilemap.IsWaterTile(tile.X, tile.Y))
                    continue;
                
                points.Add(pos);
                
                if (points.Count >= count)
                    break;
            }
            
            return points;
        }

        /// <summary>
        /// ✅ IMPROVED: Natural tree distribution
        /// </summary>
        private void QueueTreeSpawns(SpawnZone zone)
        {
            // ✅ Trees use clustering for forest look
            var spawnPoints = GenerateClusteredSpawnPoints(zone.Bounds, zone.SpawnCount, 
                minSpacing: 48f, clusterSize: 4);
            
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
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} trees (clustered)");
        }

        /// <summary>
        /// ✅ IMPROVED: Bush spawning along riversides
        /// </summary>
        private void QueueBushSpawns(SpawnZone zone)
        {
            // ✅ Bushes use loose clustering
            var spawnPoints = GenerateClusteredSpawnPoints(zone.Bounds, zone.SpawnCount, 
                minSpacing: 40f, clusterSize: 2);
            
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
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} bushes (riverside)");
        }

        private void QueueAnimalSpawns(SpawnZone zone)
        {
            var spawnPoints = GenerateClusteredSpawnPoints(zone.Bounds, zone.SpawnCount, 
                minSpacing: 80f, clusterSize: 2);
            
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
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} animals (grazing)");
        }

        private void QueueAggressiveAnimalSpawns(SpawnZone zone)
        {
            var spawnPoints = GenerateSpawnPoints(zone.Bounds, zone.SpawnCount, minSpacing: 120f);
            
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
            
            Console.WriteLine($"[SpawnZone] Queued {spawnPoints.Count}/{zone.SpawnCount} boars (wild)");
        }

        public void Update(GameTime gameTime)
        {
            ProcessSpawnQueue();
            CheckDawnRespawn();
        }

        /// <summary>
        /// ✅ IMPROVED: Smooth spawn processing with progress
        /// </summary>
        private void ProcessSpawnQueue()
        {
            if (spawnQueue.Count == 0)
                return;
            
            int spawned = 0;
            int totalCount = spawnQueue.Count;
            
            while (spawnQueue.Count > 0 && spawned < MAX_SPAWNS_PER_FRAME)
            {
                var job = spawnQueue.Dequeue();
                SpawnEntity(job);
                spawned++;
            }
            
            // ✅ Progress logging
            if (totalCount > 10)
            {
                int remaining = spawnQueue.Count;
                float progress = ((float)(totalCount - remaining) / totalCount) * 100f;
                
                if (remaining > 0 && remaining % 20 == 0)
                {
                    Console.WriteLine($"[SpawnZoneManager] ⏳ Spawning... {progress:F0}% ({remaining} remaining)");
                }
                else if (remaining == 0)
                {
                    Console.WriteLine($"[SpawnZoneManager] ✅ All entities spawned! (100%)");
                }
            }
        }

        private void CheckDawnRespawn()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            float timeOfDay = gameManager.TimeOfDay;

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
                hasRespawnedToday = false;
            }
        }

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

                animals.Clear();
            }

            Console.WriteLine($"[SpawnZoneManager] ✅ Respawned {totalRespawned} animals at dawn");
        }

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

            var animal = world.SpawnPassiveAnimal(position, type, atlas);
            
            if (animal != null)
            {
                animal.SetSpawnZone(zone.Bounds);
            }
        }

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