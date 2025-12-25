using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TribeBuild.Diagnostics;

namespace TribeBuild.UI
{
    /// <summary>
    /// 📊 Realtime performance overlay for in-game profiling
    /// Press F11 to toggle display
    /// </summary>
    public class RealtimeProfilerUI
    {
        private PerformanceProfiler profiler;
        private SpriteFont font;
        
        // Display settings
        public bool IsVisible { get; set; } = false;
        public DisplayMode CurrentMode { get; set; } = DisplayMode.Summary;
        
        // Graph data
        private Queue<float> fpsHistory;
        private Queue<float> kdTreeHistory;
        private Queue<float> pathfindingHistory;
        private const int GRAPH_SAMPLES = 120; // 2 seconds at 60 FPS
        
        // Colors
        private Color backgroundColor = new Color(0, 0, 0, 200);
        private Color textColor = Color.White;
        private Color excellentColor = Color.LightGreen;
        private Color goodColor = Color.Yellow;
        private Color warningColor = Color.Orange;
        private Color poorColor = Color.Red;
        
        // Position and size
        private Rectangle bounds;
        private const int PADDING = 10;
        private const int LINE_HEIGHT = 20;
        
        // Input
        private KeyboardState prevKeyState;

        public enum DisplayMode
        {
            Summary,        // Overall stats
            Detailed,       // All profile data
            Graphs,         // Performance graphs
            KDTree,         // KD-Tree specific
            Pathfinding,    // Pathfinding specific
            BehaviorTree    // AI specific
        }

        public RealtimeProfilerUI(SpriteFont debugFont, GraphicsDevice graphics)
        {
            profiler = PerformanceProfiler.Instance;
            font = debugFont;
            
            // Position in top-right corner
            bounds = new Rectangle(
                graphics.Viewport.Width - 400,
                10,
                390,
                300
            );
            
            fpsHistory = new Queue<float>(GRAPH_SAMPLES);
            kdTreeHistory = new Queue<float>(GRAPH_SAMPLES);
            pathfindingHistory = new Queue<float>(GRAPH_SAMPLES);
        }

        public void Update(GameTime gameTime)
        {
            // Handle input
            var keyState = Keyboard.GetState();
            
            // Toggle visibility (F11)
            if (keyState.IsKeyDown(Keys.F11) && prevKeyState.IsKeyUp(Keys.F11))
            {
                IsVisible = !IsVisible;
            }
            
            // Cycle display modes (F10)
            if (keyState.IsKeyDown(Keys.F10) && prevKeyState.IsKeyUp(Keys.F10))
            {
                CycleDisplayMode();
            }
            
            prevKeyState = keyState;
            
            if (!IsVisible) return;
            
            // Record frame snapshot
            profiler.RecordFrameSnapshot(gameTime);
            
            // Update graph data
            UpdateGraphData();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            
            spriteBatch.Begin();
            
            // Draw background
            DrawBackground(spriteBatch);
            
            // Draw content based on mode
            switch (CurrentMode)
            {
                case DisplayMode.Summary:
                    DrawSummary(spriteBatch);
                    break;
                case DisplayMode.Detailed:
                    DrawDetailed(spriteBatch);
                    break;
                case DisplayMode.Graphs:
                    DrawGraphs(spriteBatch);
                    break;
                case DisplayMode.KDTree:
                    DrawKDTreeStats(spriteBatch);
                    break;
                case DisplayMode.Pathfinding:
                    DrawPathfindingStats(spriteBatch);
                    break;
                case DisplayMode.BehaviorTree:
                    DrawBehaviorTreeStats(spriteBatch);
                    break;
            }
            
            // Draw controls
            DrawControls(spriteBatch);
            
            spriteBatch.End();
        }

        // ==================== DRAWING METHODS ====================

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            // Create 1x1 white texture for drawing rectangles
            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            
            // Background
            spriteBatch.Draw(pixel, bounds, backgroundColor);
            
            // Border
            DrawRectangleOutline(spriteBatch, pixel, bounds, Color.White, 2);
            
            pixel.Dispose();
        }

        private void DrawSummary(SpriteBatch spriteBatch)
        {
            int y = bounds.Y + PADDING;
            
            // Title
            DrawText(spriteBatch, "📊 PERFORMANCE MONITOR", bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            var report = profiler.GenerateReport();
            
            // FPS
            DrawMetric(spriteBatch, "FPS", $"{report.AvgFPS:F1}", 
                bounds.X + PADDING, y, GetFPSColor(report.AvgFPS));
            y += LINE_HEIGHT;
            
            DrawMetric(spriteBatch, "  Min", $"{report.MinFPS:F1}", 
                bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT;
            
            DrawMetric(spriteBatch, "  Max", $"{report.MaxFPS:F1}", 
                bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            // KD-Tree
            if (report.Profiles.ContainsKey("KDTree_Nearest"))
            {
                var kdData = report.Profiles["KDTree_Nearest"];
                DrawMetric(spriteBatch, "KD-Tree (Nearest)", $"{kdData.GetAverage():F4}ms",
                    bounds.X + PADDING, y, GetTimeColor(kdData.GetAverage(), 0.01, 0.05, 0.1));
                y += LINE_HEIGHT;
            }
            
            if (report.Profiles.ContainsKey("KDTree_Radius"))
            {
                var kdData = report.Profiles["KDTree_Radius"];
                DrawMetric(spriteBatch, "KD-Tree (Radius)", $"{kdData.GetAverage():F3}ms",
                    bounds.X + PADDING, y, GetTimeColor(kdData.GetAverage(), 0.05, 0.1, 0.5));
                y += LINE_HEIGHT;
            }
            
            y += 5;
            
            // Pathfinding
            if (report.Profiles.ContainsKey("Pathfinding"))
            {
                var pathData = report.Profiles["Pathfinding"];
                DrawMetric(spriteBatch, "Pathfinding", $"{pathData.GetAverage():F2}ms",
                    bounds.X + PADDING, y, GetTimeColor(pathData.GetAverage(), 1.0, 5.0, 10.0));
                y += LINE_HEIGHT;
            }
            
            y += 5;
            
            // Behavior Trees
            if (report.Profiles.ContainsKey("BehaviorTree"))
            {
                var btData = report.Profiles["BehaviorTree"];
                DrawMetric(spriteBatch, "AI (BehaviorTree)", $"{btData.GetAverage():F3}ms",
                    bounds.X + PADDING, y, GetTimeColor(btData.GetAverage(), 0.05, 0.1, 0.5));
                y += LINE_HEIGHT;
            }
        }

        private void DrawDetailed(SpriteBatch spriteBatch)
        {
            int y = bounds.Y + PADDING;
            
            DrawText(spriteBatch, "📊 DETAILED STATS", bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            var report = profiler.GenerateReport();
            
            foreach (var kvp in report.Profiles.Take(10)) // Show top 10
            {
                string name = kvp.Key.Length > 20 ? kvp.Key.Substring(0, 20) : kvp.Key;
                DrawMetric(spriteBatch, name, $"{kvp.Value.GetAverage():F3}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                if (y > bounds.Bottom - 40) break; // Don't overflow
            }
        }

        private void DrawGraphs(SpriteBatch spriteBatch)
        {
            int y = bounds.Y + PADDING;
            
            DrawText(spriteBatch, "📈 PERFORMANCE GRAPHS", bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            var graphBounds = new Rectangle(
                bounds.X + PADDING,
                y,
                bounds.Width - PADDING * 2,
                60
            );
            
            // FPS Graph
            DrawText(spriteBatch, "FPS", graphBounds.X, y, textColor);
            DrawGraph(spriteBatch, fpsHistory.ToList(), graphBounds, 30f, 120f, excellentColor);
            y += 70;
            
            // KD-Tree Graph
            graphBounds.Y = y;
            DrawText(spriteBatch, "KD-Tree (ms)", graphBounds.X, y, textColor);
            DrawGraph(spriteBatch, kdTreeHistory.ToList(), graphBounds, 0f, 0.5f, goodColor);
            y += 70;
            
            // Pathfinding Graph
            graphBounds.Y = y;
            DrawText(spriteBatch, "Pathfinding (ms)", graphBounds.X, y, textColor);
            DrawGraph(spriteBatch, pathfindingHistory.ToList(), graphBounds, 0f, 10f, warningColor);
        }

        private void DrawKDTreeStats(SpriteBatch spriteBatch)
        {
            int y = bounds.Y + PADDING;
            
            DrawText(spriteBatch, "🔍 KD-TREE PERFORMANCE", bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            var report = profiler.GenerateReport();
            
            // Show all KD-Tree related metrics
            foreach (var kvp in report.Profiles.Where(p => p.Key.StartsWith("KDTree")))
            {
                DrawText(spriteBatch, kvp.Key, bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "  Avg", $"{kvp.Value.GetAverage():F4}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "  Min", $"{kvp.Value.Min:F4}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "  Max", $"{kvp.Value.Max:F4}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT + 5;
            }
        }

        private void DrawPathfindingStats(SpriteBatch spriteBatch)
        {
            int y = bounds.Y + PADDING;
            
            DrawText(spriteBatch, "🗺️ PATHFINDING STATS", bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            var report = profiler.GenerateReport();
            
            if (report.Profiles.ContainsKey("Pathfinding"))
            {
                var data = report.Profiles["Pathfinding"];
                
                DrawMetric(spriteBatch, "Average Time", $"{data.GetAverage():F2}ms",
                    bounds.X + PADDING, y, GetTimeColor(data.GetAverage(), 1.0, 5.0, 10.0));
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "Median Time", $"{data.GetMedian():F2}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "Min Time", $"{data.Min:F2}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "Max Time", $"{data.Max:F2}ms",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT;
                
                DrawMetric(spriteBatch, "Samples", $"{data.SampleCount}",
                    bounds.X + PADDING, y, textColor);
                y += LINE_HEIGHT + 10;
                
                // Performance rating
                string rating = data.GetAverage() < 1.0 ? "⚡ EXCELLENT" :
                               data.GetAverage() < 5.0 ? "✅ GOOD" :
                               data.GetAverage() < 10.0 ? "⚠️ ACCEPTABLE" : "❌ POOR";
                
                DrawText(spriteBatch, $"Rating: {rating}", bounds.X + PADDING, y, 
                    GetTimeColor(data.GetAverage(), 1.0, 5.0, 10.0));
            }
            else
            {
                DrawText(spriteBatch, "No pathfinding data", bounds.X + PADDING, y, textColor);
            }
        }

        private void DrawBehaviorTreeStats(SpriteBatch spriteBatch)
        {
            int y = bounds.Y + PADDING;
            
            DrawText(spriteBatch, "🌳 BEHAVIOR TREE STATS", bounds.X + PADDING, y, textColor);
            y += LINE_HEIGHT + 5;
            
            var report = profiler.GenerateReport();
            
            // Show all BehaviorTree related metrics
            var btProfiles = report.Profiles.Where(p => 
                p.Key.Contains("BehaviorTree") || p.Key.Contains("AI")).ToList();
            
            if (btProfiles.Any())
            {
                foreach (var kvp in btProfiles)
                {
                    DrawText(spriteBatch, kvp.Key, bounds.X + PADDING, y, textColor);
                    y += LINE_HEIGHT;
                    
                    DrawMetric(spriteBatch, "  Avg", $"{kvp.Value.GetAverage():F3}ms",
                        bounds.X + PADDING, y, GetTimeColor(kvp.Value.GetAverage(), 0.05, 0.1, 0.5));
                    y += LINE_HEIGHT;
                    
                    DrawMetric(spriteBatch, "  Max", $"{kvp.Value.Max:F3}ms",
                        bounds.X + PADDING, y, textColor);
                    y += LINE_HEIGHT + 5;
                }
            }
            else
            {
                DrawText(spriteBatch, "No AI data", bounds.X + PADDING, y, textColor);
            }
        }

        private void DrawControls(SpriteBatch spriteBatch)
        {
            int y = bounds.Bottom - PADDING - LINE_HEIGHT * 2;
            
            DrawText(spriteBatch, "F10: Mode | F11: Toggle", 
                bounds.X + PADDING, y, new Color(180, 180, 180));
            y += LINE_HEIGHT;
            
            DrawText(spriteBatch, $"Mode: {CurrentMode}", 
                bounds.X + PADDING, y, new Color(180, 180, 180));
        }

        // ==================== HELPER METHODS ====================

        private void UpdateGraphData()
        {
            var report = profiler.GenerateReport();
            
            // FPS
            AddToQueue(fpsHistory, (float)report.AvgFPS, GRAPH_SAMPLES);
            
            // KD-Tree
            if (report.Profiles.ContainsKey("KDTree_Nearest"))
            {
                AddToQueue(kdTreeHistory, 
                    (float)report.Profiles["KDTree_Nearest"].GetAverage(), GRAPH_SAMPLES);
            }
            
            // Pathfinding
            if (report.Profiles.ContainsKey("Pathfinding"))
            {
                AddToQueue(pathfindingHistory, 
                    (float)report.Profiles["Pathfinding"].GetAverage(), GRAPH_SAMPLES);
            }
        }

        private void AddToQueue(Queue<float> queue, float value, int maxSize)
        {
            queue.Enqueue(value);
            while (queue.Count > maxSize)
                queue.Dequeue();
        }

        private void CycleDisplayMode()
        {
            CurrentMode = (DisplayMode)(((int)CurrentMode + 1) % 6);
        }

        private void DrawText(SpriteBatch spriteBatch, string text, int x, int y, Color color)
        {
            if (font != null && !string.IsNullOrEmpty(text))
            {
                spriteBatch.DrawString(font, text, new Vector2(x, y), color);
            }
        }

        private void DrawMetric(SpriteBatch spriteBatch, string label, string value, 
            int x, int y, Color color)
        {
            DrawText(spriteBatch, label, x, y, textColor);
            DrawText(spriteBatch, value, x + 180, y, color);
        }

        private void DrawGraph(SpriteBatch spriteBatch, List<float> data, 
            Rectangle graphBounds, float minValue, float maxValue, Color color)
        {
            if (data.Count < 2) return;
            
            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            
            // Draw graph background
            spriteBatch.Draw(pixel, graphBounds, new Color(20, 20, 20, 150));
            
            // Draw graph border
            DrawRectangleOutline(spriteBatch, pixel, graphBounds, Color.Gray, 1);
            
            // Draw data points
            float xStep = graphBounds.Width / (float)(data.Count - 1);
            
            for (int i = 1; i < data.Count; i++)
            {
                float prevValue = MathHelper.Clamp((data[i - 1] - minValue) / (maxValue - minValue), 0f, 1f);
                float currValue = MathHelper.Clamp((data[i] - minValue) / (maxValue - minValue), 0f, 1f);
                
                Vector2 p1 = new Vector2(
                    graphBounds.X + (i - 1) * xStep,
                    graphBounds.Bottom - prevValue * graphBounds.Height
                );
                
                Vector2 p2 = new Vector2(
                    graphBounds.X + i * xStep,
                    graphBounds.Bottom - currValue * graphBounds.Height
                );
                
                DrawLine(spriteBatch, pixel, p1, p2, color, 2);
            }
            
            pixel.Dispose();
        }

        private void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D pixel, 
            Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, 
            Vector2 start, Vector2 end, Color color, int thickness)
        {
            float distance = Vector2.Distance(start, end);
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            
            spriteBatch.Draw(pixel, start, null, color, angle, Vector2.Zero, 
                new Vector2(distance, thickness), SpriteEffects.None, 0);
        }

        // ==================== COLOR HELPERS ====================

        private Color GetFPSColor(double fps)
        {
            if (fps >= 55) return excellentColor;
            if (fps >= 45) return goodColor;
            if (fps >= 30) return warningColor;
            return poorColor;
        }

        private Color GetTimeColor(double ms, double excellent, double good, double acceptable)
        {
            if (ms < excellent) return excellentColor;
            if (ms < good) return goodColor;
            if (ms < acceptable) return warningColor;
            return poorColor;
        }
    }
}