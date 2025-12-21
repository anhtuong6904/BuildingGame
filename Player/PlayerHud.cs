using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TribeBuild.Player
{
      public class RPGHud
    {
        private PlayerCharacter player;
        private SpriteFont font;
        private Texture2D pixel;
        
        public RPGHud(PlayerCharacter player, SpriteFont font)
        {
            this.player = player;
            this.font = font;
        }
        
        public void Initialize(GraphicsDevice graphics)
        {
            pixel = new Texture2D(graphics, 1, 1);
            pixel.SetData(new[] { Color.White });
        }
        
        public void Update(GameTime gameTime)
        {
            // Update animations, etc
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            if (pixel == null) return;
            
            // Health bar
            DrawBar(spriteBatch, new Vector2(20, 20), 200, 20, 
                player.Health / player.MaxHealth, Color.Red, "HP");
            
            // Stamina bar
            DrawBar(spriteBatch, new Vector2(20, 50), 200, 20, 
                player.Stamina / player.MaxStamina, Color.Green, "Stamina");
            
            // Inventory quick display
            DrawInventory(spriteBatch, new Vector2(20, 90));
        }
        
        private void DrawBar(SpriteBatch spriteBatch, Vector2 pos, int width, int height, 
            float percent, Color color, string label)
        {
            // Background
            spriteBatch.Draw(pixel, 
                new Rectangle((int)pos.X, (int)pos.Y, width, height), 
                Color.Black * 0.5f);
            
            // Fill
            int fillWidth = (int)(width * percent);
            spriteBatch.Draw(pixel, 
                new Rectangle((int)pos.X, (int)pos.Y, fillWidth, height), 
                color);
            
            // Border
            DrawRectOutline(spriteBatch, 
                new Rectangle((int)pos.X, (int)pos.Y, width, height), 
                Color.White, 2);
            
            // Text
            string text = $"{label}: {(int)(percent * 100)}%";
            spriteBatch.DrawString(font, text, pos + new Vector2(5, 2), Color.White);
        }
        
        private void DrawInventory(SpriteBatch spriteBatch, Vector2 pos)
        {
            var items = player.Inventory.GetAllItems();
            int y = 0;
            
            foreach (var item in items)
            {
                string text = $"{item.Key}: {item.Value}";
                spriteBatch.DrawString(font, text, pos + new Vector2(0, y), Color.White);
                y += 25;
            }
        }
        
        private void DrawRectOutline(SpriteBatch sb, Rectangle rect, Color color, int thickness)
        {
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }

}