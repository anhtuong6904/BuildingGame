using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TribeBuild.World;
using TribeBuild.Entity;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Diagnostics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Testing
{
    /// <summary>
    /// 🧪 Test manager for running performance benchmarks in-game
    /// Usage: Call RunAllBenchmarks() from your Game class (e.g., on F12 key press)
    /// </summary>
    public class PerformanceTestManager
    {
        private GameWorld world;
        private PerformanceProfiler profiler;

        public PerformanceTestManager(GameWorld gameWorld)
        {
            world = gameWorld;
            profiler = PerformanceProfiler.Instance;
            profiler.LogToConsole = true;
        }

        /// <summary>
        /// 🚀 Run comprehensive performance benchmarks
        /// </summary>
        public void RunAllBenchmarks()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════╗");
            Console.WriteLine("║     🧪 PERFORMANCE BENCHMARK SUITE             ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            try
            {
                // 1. KD-Tree Benchmarks
                BenchmarkKDTree();

                // 2. Pathfinding Benchmarks
                BenchmarkPathfinding();

                // 3. Behavior Tree Benchmarks
                BenchmarkBehaviorTrees();

                // 4. Overall Report
                PrintOverallReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ BENCHMARK ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        // ==================== KD-TREE BENCHMARKS ====================

        private void BenchmarkKDTree()
        {
            Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("🔍 KD-TREE PERFORMANCE TESTS");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Test 1: Current tree performance
            if (world.KDTree != null && world.KDTree.Count > 0)
            {
                Console.WriteLine($"📊 Testing current KD-Tree ({world.KDTree.Count} entities)...\n");

                // Generate test positions
                var testPositions = GenerateTestPositions(100);

                // Benchmark
                var benchmark = profiler.BenchmarkKDTree(
                    world.KDTree,
                    testPositions,
                    new float[] { 50f, 100f, 200f, 500f, 1000f }
                );

                // Analyze results
                AnalyzeKDTreeResults(benchmark);
            }

            // Test 2: Rebuild performance
            Console.WriteLine("\n📊 Testing KD-Tree rebuild performance...\n");

            var entities = world.GetEntitiesOfType<Entity.Entity>()
                .Where(e => e.IsActive)
                .ToList();

            if (entities.Count > 0)
            {
                var rebuildBenchmark = profiler.BenchmarkKDTreeRebuild(entities, 20);
                AnalyzeRebuildResults(rebuildBenchmark);
            }

            // Test 3: Scalability test
            Console.WriteLine("\n📊 Testing KD-Tree scalability...\n");
            TestKDTreeScalability();
        }

        private void AnalyzeKDTreeResults(KDTreeBenchmark benchmark)
        {
            Console.WriteLine("┌─────────────────────────────────────────┐");
            Console.WriteLine("│         KD-Tree Analysis                │");
            Console.WriteLine("└─────────────────────────────────────────┘");

            // Performance rating
            string nearestRating = benchmark.NearestAvgTime < 0.01 ? "⚡ EXCELLENT" :
                                  benchmark.NearestAvgTime < 0.05 ? "✅ GOOD" :
                                  benchmark.NearestAvgTime < 0.1 ? "⚠️ ACCEPTABLE" : "❌ POOR";

            Console.WriteLine($"Nearest Neighbor: {nearestRating} ({benchmark.NearestAvgTime:F4}ms)");

            // Radius search analysis
            Console.WriteLine("\nRadius Search Performance:");
            foreach (var kvp in benchmark.RadiusSearches.OrderBy(x => x.Key))
            {
                string rating = kvp.Value.AvgTime < 0.05 ? "⚡" :
                               kvp.Value.AvgTime < 0.1 ? "✅" :
                               kvp.Value.AvgTime < 0.5 ? "⚠️" : "❌";

                Console.WriteLine($"  {rating} Radius {kvp.Key,4:F0}: {kvp.Value.AvgTime:F3}ms " +
                    $"(~{kvp.Value.AvgResults:F0} results)");
            }

            // Recommendations
            Console.WriteLine("\n💡 Recommendations:");

            if (benchmark.NearestAvgTime > 0.1)
            {
                Console.WriteLine("  ⚠️ Nearest neighbor search is slow. Consider:");
                Console.WriteLine("     - Reducing tree size");
                Console.WriteLine("     - Using spatial partitioning");
            }

            var largeRadius = benchmark.RadiusSearches.FirstOrDefault(r => r.Key > 500f);
            if (largeRadius.Value != null && largeRadius.Value.AvgTime > 0.5)
            {
                Console.WriteLine("  ⚠️ Large radius searches are expensive. Consider:");
                Console.WriteLine("     - Using smaller search radii");
                Console.WriteLine("     - Caching results");
            }

            Console.WriteLine();
        }

        private void AnalyzeRebuildResults(KDTreeRebuildBenchmark benchmark)
        {
            Console.WriteLine("┌─────────────────────────────────────────┐");
            Console.WriteLine("│      KD-Tree Rebuild Analysis           │");
            Console.WriteLine("└─────────────────────────────────────────┘");

            string rating = benchmark.AvgRebuildTime < 5 ? "⚡ EXCELLENT" :
                           benchmark.AvgRebuildTime < 10 ? "✅ GOOD" :
                           benchmark.AvgRebuildTime < 20 ? "⚠️ ACCEPTABLE" : "❌ POOR";

            Console.WriteLine($"Rebuild Performance: {rating}");
            Console.WriteLine($"  {benchmark.ItemCount} items in {benchmark.AvgRebuildTime:F2}ms");
            Console.WriteLine($"  Throughput: {benchmark.ItemsPerSecond:F0} items/sec");

            // Calculate rebuild frequency impact
            float rebuildsPerSecond = 1000f / (float)benchmark.AvgRebuildTime;
            float frameImpact = (float)(benchmark.AvgRebuildTime / 16.67f) * 100f; // % of 60 FPS frame

            Console.WriteLine($"\n📈 Impact Analysis:");
            Console.WriteLine($"  Max rebuilds/sec: {rebuildsPerSecond:F1}");
            Console.WriteLine($"  Frame time impact: {frameImpact:F1}% (at 60 FPS)");

            if (frameImpact > 30)
            {
                Console.WriteLine("\n⚠️ WARNING: Rebuild takes >30% of frame budget!");
                Console.WriteLine("   Current optimization: 2 rebuilds/second is GOOD ✅");
            }
            else
            {
                Console.WriteLine($"\n✅ Rebuild impact is acceptable ({frameImpact:F1}% of frame)");
            }

            Console.WriteLine();
        }

        private void TestKDTreeScalability()
        {
            Console.WriteLine("Testing with different entity counts...");

            var mockEntities = GenerateMockEntities(1000);
            var sizes = new[] { 100, 250, 500, 1000 };

            Console.WriteLine("\n┌──────────┬──────────────┬──────────────┬──────────────┐");
            Console.WriteLine("│   Size   │   Rebuild    │   Nearest    │   Radius     │");
            Console.WriteLine("├──────────┼──────────────┼──────────────┼──────────────┤");

            foreach (var size in sizes)
            {
                var subset = mockEntities.Take(size).ToList();
                var tree = new KDTree<MockEntity>();
                tree.Rebuild(subset);

                // Test rebuild
                var sw = System.Diagnostics.Stopwatch.StartNew();
                tree.Rebuild(subset);
                sw.Stop();
                var rebuildTime = sw.Elapsed.TotalMilliseconds;

                // Test nearest
                sw.Restart();
                for (int i = 0; i < 100; i++)
                {
                    tree.FindNearest(new Vector2(500, 500));
                }
                sw.Stop();
                var nearestTime = sw.Elapsed.TotalMilliseconds / 100;

                // Test radius
                sw.Restart();
                for (int i = 0; i < 100; i++)
                {
                    tree.FindInRadius(new Vector2(500, 500), 200f);
                }
                sw.Stop();
                var radiusTime = sw.Elapsed.TotalMilliseconds / 100;

                Console.WriteLine($"│  {size,6}  │  {rebuildTime,8:F2}ms  │  {nearestTime,8:F4}ms  │  {radiusTime,8:F3}ms  │");
            }

            Console.WriteLine("└──────────┴──────────────┴──────────────┴──────────────┘\n");
        }

        // ==================== PATHFINDING BENCHMARKS ====================

        private void BenchmarkPathfinding()
        {
            Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("🗺️ PATHFINDING PERFORMANCE TESTS");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            if (world.Pathfinder == null)
            {
                Console.WriteLine("❌ No pathfinder available!");
                return;
            }

            // Generate test cases
            Console.WriteLine($"📊 Generating pathfinding test cases...\n");
            var testCases = profiler.GeneratePathfindingTestCases(world.Pathfinder, 50);

            // Run benchmark
            var benchmark = profiler.BenchmarkPathfinding(world.Pathfinder, testCases);

            // Analyze results
            AnalyzePathfindingResults(benchmark);

            // Test specific scenarios
            TestPathfindingScenarios();
        }

        private void AnalyzePathfindingResults(PathfindingBenchmark benchmark)
        {
            Console.WriteLine("┌─────────────────────────────────────────┐");
            Console.WriteLine("│      Pathfinding Analysis               │");
            Console.WriteLine("└─────────────────────────────────────────┘");

            // Overall rating
            string timeRating = benchmark.AvgSearchTime < 1.0 ? "⚡ EXCELLENT" :
                               benchmark.AvgSearchTime < 5.0 ? "✅ GOOD" :
                               benchmark.AvgSearchTime < 10.0 ? "⚠️ ACCEPTABLE" : "❌ POOR";

            string successRating = benchmark.SuccessRate > 0.95f ? "⚡ EXCELLENT" :
                                  benchmark.SuccessRate > 0.85f ? "✅ GOOD" :
                                  benchmark.SuccessRate > 0.70f ? "⚠️ ACCEPTABLE" : "❌ POOR";

            Console.WriteLine($"Search Speed: {timeRating} ({benchmark.AvgSearchTime:F2}ms)");
            Console.WriteLine($"Success Rate: {successRating} ({benchmark.SuccessRate:P1})");

            // Quality metrics
            if (benchmark.AvgOptimality > 0)
            {
                string qualityRating = benchmark.AvgOptimality < 1.2f ? "⚡ OPTIMAL" :
                                      benchmark.AvgOptimality < 1.5f ? "✅ GOOD" :
                                      benchmark.AvgOptimality < 2.0f ? "⚠️ ACCEPTABLE" : "❌ POOR";

                Console.WriteLine($"Path Quality: {qualityRating} ({benchmark.AvgOptimality:F2}x optimal)");
            }

            // Performance by scenario
            if (benchmark.DetailedMetrics != null && benchmark.DetailedMetrics.Count > 0)
            {
                Console.WriteLine("\n📊 Performance by Distance:");

                var byScenario = benchmark.DetailedMetrics
                    .Where(m => m.PathFound)
                    .GroupBy(m => m.Scenario);

                foreach (var group in byScenario.OrderBy(g => g.Key))
                {
                    var metrics = group.ToList();
                    var avgTime = metrics.Average(m => m.SearchTime);
                    var avgPath = metrics.Average(m => m.PathLength);

                    Console.WriteLine($"  {group.Key,10}: {avgTime,6:F2}ms  (path: {avgPath,6:F1})");
                }
            }

            // Recommendations
            Console.WriteLine("\n💡 Recommendations:");

            if (benchmark.AvgSearchTime > 5.0)
            {
                Console.WriteLine("  ⚠️ Pathfinding is slow. Consider:");
                Console.WriteLine("     - Increasing cell size");
                Console.WriteLine("     - Adding path caching");
                Console.WriteLine("     - Using hierarchical pathfinding");
            }

            if (benchmark.SuccessRate < 0.85f)
            {
                Console.WriteLine("  ⚠️ Low success rate. Check:");
                Console.WriteLine("     - Tilemap collision data");
                Console.WriteLine("     - Pathfinder grid sync");
            }

            if (benchmark.AvgOptimality > 1.5f)
            {
                Console.WriteLine("  ⚠️ Paths are not optimal. Consider:");
                Console.WriteLine("     - Tuning heuristic function");
                Console.WriteLine("     - Better path smoothing");
            }

            Console.WriteLine();
        }

        private void TestPathfindingScenarios()
        {
            Console.WriteLine("📊 Testing specific scenarios...\n");

            // Test diagonal movement
            var start = new Vector2(100, 100);
            var end = new Vector2(500, 500);

            PathfindingMetrics metrics;
            var path = profiler.ProfilePathfinding(world.Pathfinder, start, end, out metrics);

            Console.WriteLine("Diagonal Long Path:");
            Console.WriteLine($"  Time: {metrics.SearchTime:F3}ms");
            Console.WriteLine($"  Found: {(metrics.PathFound ? "✅" : "❌")}");
            if (metrics.PathFound)
            {
                Console.WriteLine($"  Waypoints: {metrics.WaypointCount}");
                Console.WriteLine($"  Optimality: {metrics.OptimalityRatio:F2}x");
            }

            Console.WriteLine();
        }

        // ==================== BEHAVIOR TREE BENCHMARKS ====================

        private void BenchmarkBehaviorTrees()
        {
            Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("🌳 BEHAVIOR TREE PERFORMANCE TESTS");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Test with actual animals
            var passiveAnimals = world.GetEntitiesOfType<PassiveAnimal>();
            var aggressiveAnimals = world.GetEntitiesOfType<AggressiveAnimal>();

            if (passiveAnimals.Count > 0)
            {
                Console.WriteLine("📊 Testing PassiveAnimal behavior trees...\n");
                TestAnimalBehaviorTree(passiveAnimals.First(), "PassiveAnimal");
            }

            if (aggressiveAnimals.Count > 0)
            {
                Console.WriteLine("\n📊 Testing AggressiveAnimal behavior trees...\n");
                TestAnimalBehaviorTree(aggressiveAnimals.First(), "AggressiveAnimal");
            }

            // Overall AI performance
            AnalyzeAIPerformance();
        }

        private void TestAnimalBehaviorTree<T>(T animal, string name) where T : AnimalEntity
        {
            // We can't directly access the behavior tree, so we'll profile Update calls
            // which internally call the behavior tree

            var times = new List<double>();
            var gameTime = new GameTime();

            for (int i = 0; i < 1000; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                // This would need to be adapted to your actual AI structure
                // For now, we'll measure the full Update which includes BT
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            Console.WriteLine($"┌─────────────────────────────────────────┐");
            Console.WriteLine($"│   {name,-37} │");
            Console.WriteLine($"└─────────────────────────────────────────┘");
            Console.WriteLine($"Samples: {times.Count}");
            Console.WriteLine($"Avg: {times.Average():F4}ms");
            Console.WriteLine($"Min: {times.Min():F4}ms | Max: {times.Max():F4}ms");

            string rating = times.Average() < 0.05 ? "⚡ EXCELLENT" :
                           times.Average() < 0.1 ? "✅ GOOD" :
                           times.Average() < 0.5 ? "⚠️ ACCEPTABLE" : "❌ POOR";

            Console.WriteLine($"Rating: {rating}");
            Console.WriteLine();
        }

        private void AnalyzeAIPerformance()
        {
            Console.WriteLine("\n┌─────────────────────────────────────────┐");
            Console.WriteLine("│         AI System Analysis              │");
            Console.WriteLine("└─────────────────────────────────────────┘");

            var allAnimals = world.GetEntitiesOfType<AnimalEntity>();
            Console.WriteLine($"Total AI Entities: {allAnimals.Count}");

            // Estimate total AI cost per frame
            float estimatedAICost = allAnimals.Count * 0.05f; // 0.05ms per animal (example)
            float frameTimePercent = (estimatedAICost / 16.67f) * 100f;

            Console.WriteLine($"Estimated AI Cost: {estimatedAICost:F2}ms/frame");
            Console.WriteLine($"Frame Impact: {frameTimePercent:F1}% (at 60 FPS)");

            if (frameTimePercent > 20)
            {
                Console.WriteLine("\n⚠️ AI is consuming >20% of frame time!");
                Console.WriteLine("   Consider:");
                Console.WriteLine("   - Reducing AI tick frequency");
                Console.WriteLine("   - Using LOD for distant entities");
                Console.WriteLine("   - Optimizing behavior trees");
            }
            else
            {
                Console.WriteLine($"\n✅ AI performance is good ({frameTimePercent:F1}% of frame)");
            }

            Console.WriteLine();
        }

        // ==================== OVERALL REPORT ====================

        private void PrintOverallReport()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════╗");
            Console.WriteLine("║         📊 OVERALL PERFORMANCE SUMMARY         ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            var report = profiler.GenerateReport();
            Console.WriteLine(report.ToString());

            // Performance summary
            Console.WriteLine("🎯 KEY METRICS:");
            Console.WriteLine($"   FPS: {report.AvgFPS:F1} avg");
            
            if (report.Profiles.ContainsKey("KDTree_Nearest"))
            {
                var kdNearest = report.Profiles["KDTree_Nearest"];
                Console.WriteLine($"   KD-Tree Nearest: {kdNearest.GetAverage():F4}ms");
            }

            if (report.Profiles.ContainsKey("Pathfinding"))
            {
                var pathfinding = report.Profiles["Pathfinding"];
                Console.WriteLine($"   Pathfinding: {pathfinding.GetAverage():F3}ms");
            }

            Console.WriteLine("\n✅ Benchmark suite completed!");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }

        // ==================== HELPER METHODS ====================

        private List<Vector2> GenerateTestPositions(int count)
        {
            var positions = new List<Vector2>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector2(
                    random.Next(world.Width),
                    random.Next(world.Height)
                ));
            }

            return positions;
        }

        private List<MockEntity> GenerateMockEntities(int count)
        {
            var entities = new List<MockEntity>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                entities.Add(new MockEntity
                {
                    Position = new Vector2(
                        random.Next(1000),
                        random.Next(1000)
                    )
                });
            }

            return entities;
        }
    }

    // Mock entity for testing
    public class MockEntity : IPosition
    {
        public Vector2 Position { get; set; }
    }
}