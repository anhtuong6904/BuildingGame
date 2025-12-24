using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TribeBuild.UI
{
    /// <summary>
    /// ✅ Màn hình tổng kết ngày (giống Stardew Valley)
    /// Hiển thị lúc 2 AM, cho phép player xem thống kê và bắt đầu ngày mới
    /// </summary>
    public class DaySummaryScreen
    {
        private bool isVisible = false;
        private World.DaySummary summary;
        // UI Settings
        private Rectangle panelRect;
        private Color backgroundColor = new Color(20, 20, 30, 240);
        private Color textColor = Color.White;
        private Color accentColor = new Color(255, 215, 0); // Gold
        
        // Animation
        private float fadeAlpha = 0f;
        private float fadeSpeed = 2f;
        private bool fullyVisible = false;
        
        // Input
        private KeyboardState previousKeyState;
        private bool canProceed = false;
        private float proceedTimer = 0f;
        private const float PROCEED_DELAY = 1f; // Wait 1 second before allowing input
        
        // Fonts (you'll need to load these)
        private SpriteFont titleFont;
        private SpriteFont normalFont;
        private SpriteFont smallFont;
        
        // Textures
        private Texture2D pixel; // 1x1 white pixel for drawing rectangles

        public DaySummaryScreen(SpriteFont title, SpriteFont normal, SpriteFont small)
        {
            titleFont = title;
            normalFont = normal;
            smallFont = small;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            // Create 1x1 white pixel texture
            pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            
            // Calculate panel position (center of screen)
            int panelWidth = 600;
            int panelHeight = 500;
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            
            panelRect = new Rectangle(
                (screenWidth - panelWidth) / 2,
                (screenHeight - panelHeight) / 2,
                panelWidth,
                panelHeight
            );
        }

        public void Show(World.DaySummary daySummary)
        {
            isVisible = true;
            summary = daySummary;
            fadeAlpha = 0f;
            fullyVisible = false;
            canProceed = false;
            proceedTimer = 0f;
            
            Console.WriteLine("[DaySummaryScreen] Showing day summary...");
        }

        public void Hide()
        {
            isVisible = false;
            fullyVisible = false;
            Console.WriteLine("[DaySummaryScreen] Hidden");
        }

        public void Update(GameTime gameTime)
        {
            if (!isVisible) return;
            
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Fade in animation
            if (!fullyVisible)
            {
                fadeAlpha = Math.Min(1f, fadeAlpha + fadeSpeed * dt);
                
                if (fadeAlpha >= 1f)
                {
                    fullyVisible = true;
                }
            }
            
            // Wait before allowing input
            if (!canProceed)
            {
                proceedTimer += dt;
                if (proceedTimer >= PROCEED_DELAY)
                {
                    canProceed = true;
                }
            }
            
            // Handle input
            if (canProceed)
            {
                KeyboardState keyState = Keyboard.GetState();
                
                // Press SPACE or ENTER to continue
                if ((keyState.IsKeyDown(Keys.Space) && !previousKeyState.IsKeyDown(Keys.Space)) ||
                    (keyState.IsKeyDown(Keys.Enter) && !previousKeyState.IsKeyDown(Keys.Enter)))
                {
                    OnContinuePressed();
                }
                
                previousKeyState = keyState;
            }
        }

        private void OnContinuePressed()
        {
            Console.WriteLine("[DaySummaryScreen] Player pressed continue");
            
            // Notify DayNightCycleManager to start new day
            World.DayNightCycleManager.Instance.StartNewDay();
            
            Hide();
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!isVisible || summary == null) return;
            
            // Draw semi-transparent fullscreen overlay
            Rectangle screenRect = new Rectangle(0, 0, 
                spriteBatch.GraphicsDevice.Viewport.Width, 
                spriteBatch.GraphicsDevice.Viewport.Height);
            spriteBatch.Draw(pixel, screenRect, new Color(0, 0, 0, 150) * fadeAlpha);
            
            // Draw panel background
            spriteBatch.Draw(pixel, panelRect, backgroundColor * fadeAlpha);
            
            // Draw border
            DrawBorder(spriteBatch, panelRect, accentColor * fadeAlpha, 3);
            
            // Draw content
            DrawContent(spriteBatch, gameTime);
        }

        private void DrawContent(SpriteBatch spriteBatch, GameTime gameTime)
        {
            int yOffset = panelRect.Y + 30;
            int xCenter = panelRect.X + panelRect.Width / 2;
            
            // Title
            string title = $"Day {summary.DayNumber} Complete!";
            Vector2 titleSize = titleFont.MeasureString(title);
            Vector2 titlePos = new Vector2(xCenter - titleSize.X / 2, yOffset);
            spriteBatch.DrawString(titleFont, title, titlePos, accentColor * fadeAlpha);
            
            yOffset += 80;
            
            // Resources section
            DrawSectionTitle(spriteBatch, "Resources Collected", xCenter, ref yOffset);
            DrawStatLine(spriteBatch, "Wood", summary.WoodCollected.ToString(), xCenter, ref yOffset);
            DrawStatLine(spriteBatch, "Stone", summary.StoneCollected.ToString(), xCenter, ref yOffset);
            DrawStatLine(spriteBatch, "Food", summary.FoodCollected.ToString(), xCenter, ref yOffset);
            
            yOffset += 30;
            
            // Combat section
            DrawSectionTitle(spriteBatch, "Combat", xCenter, ref yOffset);
            DrawStatLine(spriteBatch, "Enemies Defeated", summary.EnemiesKilled.ToString(), xCenter, ref yOffset);
            DrawStatLine(spriteBatch, "Damage Taken", summary.DamageTaken.ToString(), xCenter, ref yOffset);
            
            yOffset += 30;
            
            // Status
            if (summary.SurvivedNight)
            {
                string status = "You survived the night!";
                Vector2 statusSize = normalFont.MeasureString(status);
                Vector2 statusPos = new Vector2(xCenter - statusSize.X / 2, yOffset);
                spriteBatch.DrawString(normalFont, status, statusPos, Color.LightGreen * fadeAlpha);
                yOffset += 40;
            }
            
            // Continue prompt (blink effect)
            if (canProceed)
            {
                float blinkAlpha = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3f) * 0.3f + 0.7f;
                string prompt = "Press SPACE or ENTER to continue";
                Vector2 promptSize = smallFont.MeasureString(prompt);
                Vector2 promptPos = new Vector2(xCenter - promptSize.X / 2, panelRect.Bottom - 50);
                spriteBatch.DrawString(smallFont, prompt, promptPos, textColor * fadeAlpha * blinkAlpha);
            }
        }

        private void DrawSectionTitle(SpriteBatch spriteBatch, string text, int xCenter, ref int yOffset)
        {
            Vector2 size = normalFont.MeasureString(text);
            Vector2 pos = new Vector2(xCenter - size.X / 2, yOffset);
            
            // Underline
            Rectangle underline = new Rectangle(
                (int)(pos.X - 10),
                (int)(pos.Y + size.Y + 2),
                (int)(size.X + 20),
                2
            );
            spriteBatch.Draw(pixel, underline, accentColor * fadeAlpha);
            
            spriteBatch.DrawString(normalFont, text, pos, accentColor * fadeAlpha);
            yOffset += (int)size.Y + 15;
        }

        private void DrawStatLine(SpriteBatch spriteBatch, string label, string value, int xCenter, ref int yOffset)
        {
            int spacing = 200;
            
            // Label (left)
            Vector2 labelSize = normalFont.MeasureString(label);
            Vector2 labelPos = new Vector2(xCenter - spacing / 2 - labelSize.X, yOffset);
            spriteBatch.DrawString(normalFont, label, labelPos, textColor * fadeAlpha);
            
            // Value (right)
            Vector2 valuePos = new Vector2(xCenter + spacing / 2, yOffset);
            spriteBatch.DrawString(normalFont, value, valuePos, Color.LightGreen * fadeAlpha);
            
            yOffset += (int)labelSize.Y + 10;
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
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

        public bool IsVisible => isVisible;
    }
}