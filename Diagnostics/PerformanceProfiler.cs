using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary.PathFinding;
using MonoGameLibrary.Spatial;
using MonoGameLibrary.Behavior;

namespace TribeBuild.Diagnostics
{
    /// <summary>
    /// 📊 Comprehensive performance profiler for game systems
    /// </summary>
    public class PerformanceProfiler
    {
        private static PerformanceProfiler instance;
        public static PerformanceProfiler Instance => instance ??= new PerformanceProfiler();

        // Profiling data
        private Dictionary<string, ProfileData> profiles;
        private Queue<FrameSnapshot> frameHistory;
        private const int MAX_FRAME_HISTORY = 300; // 5 seconds at 60 FPS

        // Settings
        public bool IsEnabled { get; set; } = true;
        public bool LogToConsole { get; set; } = false;
        public int SampleSize { get; set; } = 100;

        private PerformanceProfiler()
        {
            profiles = new Dictionary<string, ProfileData>();
            frameHistory = new Queue<FrameSnapshot>();
        }

        // ==================== BEHAVIOR TREE PROFILING ====================

        /// <summary>
        /// Profile a single behavior tree tick
        /// </summary>
        public NodeState ProfileBehaviorTreeTick(BehaviorTree tree, BehaviorContext context, string label = "BehaviorTree")
        {
            if (!IsEnabled) return tree?.Tick(context) ?? NodeState.Failure;

            var stopwatch = Stopwatch.StartNew();
            var result = tree?.Tick(context) ?? NodeState.Failure;
            stopwatch.Stop();

            RecordSample(label, stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }

        /// <summary>
        /// Benchmark behavior tree performance with multiple iterations
        /// </summary>
        public BehaviorTreeBenchmark BenchmarkBehaviorTree(
            BehaviorTree tree, 
            BehaviorContext context, 
            int iterations = 1000)
        {
            var benchmark = new BehaviorTreeBenchmark
            {
                TreeName = tree?.ToString() ?? "Unknown",
                Iterations = iterations
            };

            var times = new List<double>();
            var results = new Dictionary<NodeState, int>();

            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = tree.Tick(context);
                stopwatch.Stop();

                times.Add(stopwatch.Elapsed.TotalMilliseconds);

                if (!results.ContainsKey(result))
                    results[result] = 0;
                results[result]++;
            }

            // Calculate statistics
            times.Sort();
            benchmark.MinTime = times.First();
            benchmark.MaxTime = times.Last();
            benchmark.AvgTime = times.Average();
            benchmark.MedianTime = times[times.Count / 2];
            benchmark.P95Time = times[(int)(times.Count * 0.95)];
            benchmark.P99Time = times[(int)(times.Count * 0.99)];
            benchmark.TotalTime = times.Sum();
            benchmark.ResultDistribution = results;

            if (LogToConsole)
            {
                Console.WriteLine("\n" + benchmark.ToString());
            }

            return benchmark;
        }

        /// <summary>
        /// Profile behavior tree with node-level detail
        /// </summary>
        public BehaviorTreeDetailedProfile ProfileBehaviorTreeDetailed(
            BehaviorTree tree,
            BehaviorContext context,
            int samples = 100)
        {
            var profile = new BehaviorTreeDetailedProfile
            {
                TreeName = tree?.ToString() ?? "Unknown",
                Samples = samples
            };

            // This would require instrumenting the behavior tree nodes
            // For now, we'll profile the overall tree
            var times = new List<double>();

            for (int i = 0; i < samples; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                tree.Tick(context);
                stopwatch.Stop();
                times.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            profile.TotalAvgTime = times.Average();
            profile.TotalMaxTime = times.Max();

            return profile;
        }

        // ==================== A* PATHFINDING PROFILING ====================

        /// <summary>
        /// Profile a single pathfinding request
        /// </summary>
        public List<Vector2> ProfilePathfinding(
            GridPathfinder pathfinder,
            Vector2 start,
            Vector2 end,
            out PathfindingMetrics metrics)
        {
            metrics = new PathfindingMetrics
            {
                StartPos = start,
                EndPos = end,
                StraightLineDistance = Vector2.Distance(start, end)
            };

            var stopwatch = Stopwatch.StartNew();
            var path = pathfinder.FindPath(start, end);
            stopwatch.Stop();

            metrics.SearchTime = stopwatch.Elapsed.TotalMilliseconds;
            metrics.PathFound = path != null && path.Count > 0;

            if (metrics.PathFound)
            {
                metrics.PathLength = CalculatePathLength(path);
                metrics.WaypointCount = path.Count;
                metrics.OptimalityRatio = metrics.PathLength / metrics.StraightLineDistance;
            }

            RecordSample("Pathfinding", metrics.SearchTime);
            return path;
        }

        /// <summary>
        /// Benchmark pathfinding with various scenarios
        /// </summary>
        public PathfindingBenchmark BenchmarkPathfinding(
            GridPathfinder pathfinder,
            List<PathTestCase> testCases)
        {
            var benchmark = new PathfindingBenchmark
            {
                GridSize = $"{pathfinder.GridWidth}x{pathfinder.GridHeight}",
                TestCases = testCases.Count
            };

            var allMetrics = new List<PathfindingMetrics>();

            foreach (var testCase in testCases)
            {
                var metrics = new PathfindingMetrics
                {
                    StartPos = testCase.Start,
                    EndPos = testCase.End,
                    StraightLineDistance = Vector2.Distance(testCase.Start, testCase.End),
                    Scenario = testCase.Scenario
                };

                var stopwatch = Stopwatch.StartNew();
                var path = pathfinder.FindPath(testCase.Start, testCase.End);
                stopwatch.Stop();

                metrics.SearchTime = stopwatch.Elapsed.TotalMilliseconds;
                metrics.PathFound = path != null && path.Count > 0;

                if (metrics.PathFound)
                {
                    metrics.PathLength = CalculatePathLength(path);
                    metrics.WaypointCount = path.Count;
                    metrics.OptimalityRatio = metrics.PathLength / metrics.StraightLineDistance;
                }

                allMetrics.Add(metrics);
            }

            // Aggregate statistics
            var times = allMetrics.Select(m => m.SearchTime).ToList();
            benchmark.AvgSearchTime = times.Average();
            benchmark.MinSearchTime = times.Min();
            benchmark.MaxSearchTime = times.Max();
            benchmark.MedianSearchTime = times.OrderBy(t => t).ElementAt(times.Count / 2);
            benchmark.SuccessRate = allMetrics.Count(m => m.PathFound) / (float)allMetrics.Count;

            var foundPaths = allMetrics.Where(m => m.PathFound).ToList();
            if (foundPaths.Any())
            {
                benchmark.AvgPathLength = foundPaths.Average(m => m.PathLength);
                benchmark.AvgOptimality = foundPaths.Average(m => m.OptimalityRatio);
            }

            benchmark.DetailedMetrics = allMetrics;

            if (LogToConsole)
            {
                Console.WriteLine("\n" + benchmark.ToString());
            }

            return benchmark;
        }

        /// <summary>
        /// Generate standard pathfinding test cases
        /// </summary>
        public List<PathTestCase> GeneratePathfindingTestCases(
            GridPathfinder pathfinder,
            int count = 50)
        {
            var testCases = new List<PathTestCase>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                // Generate random valid positions
                Vector2 start = Vector2.Zero;
                Vector2 end = Vector2.Zero;
                string scenario = "Random";

                int maxAttempts = 100;
                int attempts = 0;

                while (attempts < maxAttempts)
                {
                    start = new Vector2(
                        random.Next(pathfinder.GridWidth) * pathfinder.CellSize,
                        random.Next(pathfinder.GridHeight) * pathfinder.CellSize
                    );

                    end = new Vector2(
                        random.Next(pathfinder.GridWidth) * pathfinder.CellSize,
                        random.Next(pathfinder.GridHeight) * pathfinder.CellSize
                    );

                    if (pathfinder.IsWalkable(start) && pathfinder.IsWalkable(end))
                    {
                        float distance = Vector2.Distance(start, end);

                        // Categorize by distance
                        if (distance < 100)
                            scenario = "Short";
                        else if (distance < 300)
                            scenario = "Medium";
                        else if (distance < 600)
                            scenario = "Long";
                        else
                            scenario = "VeryLong";

                        break;
                    }

                    attempts++;
                }

                if (attempts < maxAttempts)
                {
                    testCases.Add(new PathTestCase
                    {
                        Start = start,
                        End = end,
                        Scenario = scenario
                    });
                }
            }

            return testCases;
        }

        // ==================== KD-TREE PROFILING ====================

        /// <summary>
        /// Profile KD-Tree nearest neighbor search
        /// </summary>
        public T ProfileKDTreeNearest<T>(
            KDTree<T> tree,
            Vector2 position,
            out KDTreeMetrics metrics) where T : IPosition
        {
            metrics = new KDTreeMetrics
            {
                TreeSize = tree.Count,
                QueryType = "Nearest"
            };

            var stopwatch = Stopwatch.StartNew();
            var result = tree.FindNearest(position, out float distance);
            stopwatch.Stop();

            metrics.SearchTime = stopwatch.Elapsed.TotalMilliseconds;
            metrics.ResultCount = result != null ? 1 : 0;
            metrics.ResultDistance = distance;

            RecordSample("KDTree_Nearest", metrics.SearchTime);
            return result;
        }

        /// <summary>
        /// Profile KD-Tree radius search
        /// </summary>
        public List<SpatialResult<T>> ProfileKDTreeRadius<T>(
            KDTree<T> tree,
            Vector2 position,
            float radius,
            out KDTreeMetrics metrics) where T : IPosition
        {
            metrics = new KDTreeMetrics
            {
                TreeSize = tree.Count,
                QueryType = "Radius",
                SearchRadius = radius
            };

            var stopwatch = Stopwatch.StartNew();
            var results = tree.FindInRadius(position, radius);
            stopwatch.Stop();

            metrics.SearchTime = stopwatch.Elapsed.TotalMilliseconds;
            metrics.ResultCount = results.Count;

            RecordSample("KDTree_Radius", metrics.SearchTime);
            return results;
        }

        /// <summary>
        /// Benchmark KD-Tree with various query types
        /// </summary>
        public KDTreeBenchmark BenchmarkKDTree<T>(
            KDTree<T> tree,
            List<Vector2> queryPositions,
            float[] radii = null) where T : IPosition
        {
            radii ??= new float[] { 50f, 100f, 200f, 500f };

            var benchmark = new KDTreeBenchmark
            {
                TreeSize = tree.Count,
                QueryCount = queryPositions.Count
            };

            // Benchmark nearest neighbor
            var nearestTimes = new List<double>();
            foreach (var pos in queryPositions)
            {
                var stopwatch = Stopwatch.StartNew();
                tree.FindNearest(pos);
                stopwatch.Stop();
                nearestTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            benchmark.NearestAvgTime = nearestTimes.Average();
            benchmark.NearestMinTime = nearestTimes.Min();
            benchmark.NearestMaxTime = nearestTimes.Max();

            // Benchmark radius searches
            benchmark.RadiusSearches = new Dictionary<float, RadiusSearchStats>();

            foreach (var radius in radii)
            {
                var radiusTimes = new List<double>();
                var resultCounts = new List<int>();

                foreach (var pos in queryPositions)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var results = tree.FindInRadius(pos, radius);
                    stopwatch.Stop();

                    radiusTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                    resultCounts.Add(results.Count);
                }

                benchmark.RadiusSearches[radius] = new RadiusSearchStats
                {
                    AvgTime = radiusTimes.Average(),
                    MinTime = radiusTimes.Min(),
                    MaxTime = radiusTimes.Max(),
                    AvgResults = resultCounts.Average()
                };
            }

            if (LogToConsole)
            {
                Console.WriteLine("\n" + benchmark.ToString());
            }

            return benchmark;
        }

        /// <summary>
        /// Benchmark KD-Tree rebuild performance
        /// </summary>
        public KDTreeRebuildBenchmark BenchmarkKDTreeRebuild<T>(
            List<T> items,
            int iterations = 10) where T : IPosition
        {
            var benchmark = new KDTreeRebuildBenchmark
            {
                ItemCount = items.Count,
                Iterations = iterations
            };

            var times = new List<double>();

            for (int i = 0; i < iterations; i++)
            {
                var tree = new KDTree<T>();

                var stopwatch = Stopwatch.StartNew();
                tree.Rebuild(items);
                stopwatch.Stop();

                times.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            benchmark.AvgRebuildTime = times.Average();
            benchmark.MinRebuildTime = times.Min();
            benchmark.MaxRebuildTime = times.Max();
            benchmark.ItemsPerSecond = items.Count / (benchmark.AvgRebuildTime / 1000.0);

            if (LogToConsole)
            {
                Console.WriteLine("\n" + benchmark.ToString());
            }

            return benchmark;
        }

        // ==================== GENERAL PROFILING ====================

        private void RecordSample(string label, double milliseconds)
        {
            if (!profiles.ContainsKey(label))
            {
                profiles[label] = new ProfileData(label, SampleSize);
            }

            profiles[label].AddSample(milliseconds);
        }

        public void RecordFrameSnapshot(GameTime gameTime)
        {
            if (!IsEnabled) return;

            var snapshot = new FrameSnapshot
            {
                Timestamp = gameTime.TotalGameTime.TotalSeconds,
                FrameTime = gameTime.ElapsedGameTime.TotalMilliseconds
            };

            // Copy current profile data
            foreach (var kvp in profiles)
            {
                snapshot.ProfileSamples[kvp.Key] = kvp.Value.GetAverage();
            }

            frameHistory.Enqueue(snapshot);

            while (frameHistory.Count > MAX_FRAME_HISTORY)
            {
                frameHistory.Dequeue();
            }
        }

        public ProfileReport GenerateReport()
        {
            var report = new ProfileReport
            {
                GeneratedAt = DateTime.Now,
                Profiles = new Dictionary<string, ProfileData>()
            };

            foreach (var kvp in profiles)
            {
                report.Profiles[kvp.Key] = kvp.Value;
            }

            // Calculate frame statistics
            if (frameHistory.Count > 0)
            {
                var frameTimes = frameHistory.Select(f => f.FrameTime).ToList();
                report.AvgFPS = 1000.0 / frameTimes.Average();
                report.MinFPS = 1000.0 / frameTimes.Max();
                report.MaxFPS = 1000.0 / frameTimes.Min();
            }

            return report;
        }

        public void PrintReport()
        {
            var report = GenerateReport();
            Console.WriteLine("\n" + report.ToString());
        }

        public void Reset()
        {
            profiles.Clear();
            frameHistory.Clear();
        }

        // ==================== HELPER METHODS ====================

        private float CalculatePathLength(List<Vector2> path)
        {
            if (path == null || path.Count < 2) return 0f;

            float length = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                length += Vector2.Distance(path[i - 1], path[i]);
            }
            return length;
        }
    }

    // ==================== DATA STRUCTURES ====================

    public class ProfileData
    {
        public string Label { get; }
        private Queue<double> samples;
        private int maxSamples;

        public double Min { get; private set; } = double.MaxValue;
        public double Max { get; private set; } = double.MinValue;
        public int SampleCount => samples.Count;

        public ProfileData(string label, int maxSamples)
        {
            Label = label;
            this.maxSamples = maxSamples;
            samples = new Queue<double>(maxSamples);
        }

        public void AddSample(double value)
        {
            samples.Enqueue(value);
            if (samples.Count > maxSamples)
                samples.Dequeue();

            Min = Math.Min(Min, value);
            Max = Math.Max(Max, value);
        }

        public double GetAverage()
        {
            return samples.Count > 0 ? samples.Average() : 0;
        }

        public double GetMedian()
        {
            if (samples.Count == 0) return 0;
            var sorted = samples.OrderBy(x => x).ToList();
            return sorted[sorted.Count / 2];
        }

        public override string ToString()
        {
            return $"{Label}: Avg={GetAverage():F3}ms, Min={Min:F3}ms, Max={Max:F3}ms";
        }
    }

    public class FrameSnapshot
    {
        public double Timestamp { get; set; }
        public double FrameTime { get; set; }
        public Dictionary<string, double> ProfileSamples { get; set; } = new Dictionary<string, double>();
    }

    // ==================== BEHAVIOR TREE BENCHMARKS ====================

    public class BehaviorTreeBenchmark
    {
        public string TreeName { get; set; }
        public int Iterations { get; set; }
        public double MinTime { get; set; }
        public double MaxTime { get; set; }
        public double AvgTime { get; set; }
        public double MedianTime { get; set; }
        public double P95Time { get; set; }
        public double P99Time { get; set; }
        public double TotalTime { get; set; }
        public Dictionary<NodeState, int> ResultDistribution { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Behavior Tree Benchmark: {TreeName} ===");
            sb.AppendLine($"Iterations: {Iterations}");
            sb.AppendLine($"Avg Time: {AvgTime:F3}ms");
            sb.AppendLine($"Median: {MedianTime:F3}ms");
            sb.AppendLine($"Min: {MinTime:F3}ms | Max: {MaxTime:F3}ms");
            sb.AppendLine($"P95: {P95Time:F3}ms | P99: {P99Time:F3}ms");
            sb.AppendLine($"Total: {TotalTime:F2}ms");
            sb.AppendLine("Result Distribution:");
            foreach (var kvp in ResultDistribution)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} ({kvp.Value * 100.0 / Iterations:F1}%)");
            }
            return sb.ToString();
        }
    }

    public class BehaviorTreeDetailedProfile
    {
        public string TreeName { get; set; }
        public int Samples { get; set; }
        public double TotalAvgTime { get; set; }
        public double TotalMaxTime { get; set; }
        public Dictionary<string, double> NodeTimes { get; set; } = new Dictionary<string, double>();
    }

    // ==================== PATHFINDING BENCHMARKS ====================

    public class PathTestCase
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public string Scenario { get; set; }
    }

    public class PathfindingMetrics
    {
        public Vector2 StartPos { get; set; }
        public Vector2 EndPos { get; set; }
        public string Scenario { get; set; }
        public double SearchTime { get; set; }
        public bool PathFound { get; set; }
        public float PathLength { get; set; }
        public int WaypointCount { get; set; }
        public float StraightLineDistance { get; set; }
        public float OptimalityRatio { get; set; } // PathLength / StraightLineDistance
    }

    public class PathfindingBenchmark
    {
        public string GridSize { get; set; }
        public int TestCases { get; set; }
        public double AvgSearchTime { get; set; }
        public double MinSearchTime { get; set; }
        public double MaxSearchTime { get; set; }
        public double MedianSearchTime { get; set; }
        public float SuccessRate { get; set; }
        public float AvgPathLength { get; set; }
        public float AvgOptimality { get; set; }
        public List<PathfindingMetrics> DetailedMetrics { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Pathfinding Benchmark ===");
            sb.AppendLine($"Grid: {GridSize} | Test Cases: {TestCases}");
            sb.AppendLine($"Success Rate: {SuccessRate:P1}");
            sb.AppendLine($"Search Times:");
            sb.AppendLine($"  Avg: {AvgSearchTime:F3}ms");
            sb.AppendLine($"  Median: {MedianSearchTime:F3}ms");
            sb.AppendLine($"  Min: {MinSearchTime:F3}ms | Max: {MaxSearchTime:F3}ms");
            sb.AppendLine($"Path Quality:");
            sb.AppendLine($"  Avg Length: {AvgPathLength:F1}");
            sb.AppendLine($"  Avg Optimality: {AvgOptimality:F2}x");

            if (DetailedMetrics != null)
            {
                var byScenario = DetailedMetrics.GroupBy(m => m.Scenario);
                sb.AppendLine("By Scenario:");
                foreach (var group in byScenario)
                {
                    var metrics = group.ToList();
                    sb.AppendLine($"  {group.Key}: {metrics.Average(m => m.SearchTime):F3}ms avg");
                }
            }

            return sb.ToString();
        }
    }

    // ==================== KD-TREE BENCHMARKS ====================

    public class KDTreeMetrics
    {
        public int TreeSize { get; set; }
        public string QueryType { get; set; }
        public double SearchTime { get; set; }
        public int ResultCount { get; set; }
        public float SearchRadius { get; set; }
        public float ResultDistance { get; set; }
    }

    public class RadiusSearchStats
    {
        public double AvgTime { get; set; }
        public double MinTime { get; set; }
        public double MaxTime { get; set; }
        public double AvgResults { get; set; }
    }

    public class KDTreeBenchmark
    {
        public int TreeSize { get; set; }
        public int QueryCount { get; set; }
        public double NearestAvgTime { get; set; }
        public double NearestMinTime { get; set; }
        public double NearestMaxTime { get; set; }
        public Dictionary<float, RadiusSearchStats> RadiusSearches { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== KD-Tree Benchmark ===");
            sb.AppendLine($"Tree Size: {TreeSize} | Queries: {QueryCount}");
            sb.AppendLine($"Nearest Neighbor:");
            sb.AppendLine($"  Avg: {NearestAvgTime:F3}ms");
            sb.AppendLine($"  Min: {NearestMinTime:F3}ms | Max: {NearestMaxTime:F3}ms");
            
            if (RadiusSearches != null && RadiusSearches.Count > 0)
            {
                sb.AppendLine("Radius Searches:");
                foreach (var kvp in RadiusSearches.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"  Radius {kvp.Key:F0}:");
                    sb.AppendLine($"    Avg: {kvp.Value.AvgTime:F3}ms | Results: {kvp.Value.AvgResults:F1}");
                }
            }

            return sb.ToString();
        }
    }

    public class KDTreeRebuildBenchmark
    {
        public int ItemCount { get; set; }
        public int Iterations { get; set; }
        public double AvgRebuildTime { get; set; }
        public double MinRebuildTime { get; set; }
        public double MaxRebuildTime { get; set; }
        public double ItemsPerSecond { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== KD-Tree Rebuild Benchmark ===");
            sb.AppendLine($"Items: {ItemCount} | Iterations: {Iterations}");
            sb.AppendLine($"Avg Rebuild: {AvgRebuildTime:F3}ms");
            sb.AppendLine($"Min: {MinRebuildTime:F3}ms | Max: {MaxRebuildTime:F3}ms");
            sb.AppendLine($"Throughput: {ItemsPerSecond:F0} items/sec");
            return sb.ToString();
        }
    }

    // ==================== GENERAL REPORTS ====================

    public class ProfileReport
    {
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, ProfileData> Profiles { get; set; }
        public double AvgFPS { get; set; }
        public double MinFPS { get; set; }
        public double MaxFPS { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"\n========== Performance Report ==========");
            sb.AppendLine($"Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"FPS: Avg={AvgFPS:F1} | Min={MinFPS:F1} | Max={MaxFPS:F1}");
            sb.AppendLine("\nProfile Data:");

            foreach (var kvp in Profiles.OrderBy(p => p.Key))
            {
                sb.AppendLine($"  {kvp.Value}");
            }

            sb.AppendLine("========================================\n");
            return sb.ToString();
        }
    }
}