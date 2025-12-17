using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;

namespace TribeBuild.Entity.Resource
{

    public enum TreeType
    {
        Oak,
        other,
    };

    public enum TreeState
    {
        baseState,
        stump,
    };

    public class Tree : ResourceEntity,IPosition
    {   
        public TreeType treeType {get; private set;}

        public Sprite spirteRoot {get; private set;}
        
        public Tree (int id, Vector2 pos, TreeType Type = TreeType.Oak, Sprite sprite = null) : base(id, pos, ResourceType.Tree)
        {
            treeType = Type;
            Sprite = sprite;

            switch (treeType) 
            {
                case TreeType.Oak:      
                    MaxHealth = 100f;
                    YieldAmount = 5;
                    YieldItem = "wood";
                    RespawnTime = 120f; // 2 minutes
                    break;
                // case TreeType.other:
                //      MaxHealth = 100f;
                //     YieldAmount = 5;
                //     YieldItem = "wood";
                //     RespawnTime = 120f; // 2 minutes
                //     break;
            }
            if (sprite != null)
            {
                Collider = new Rectangle(
                    (int)sprite.Width / 3,
                    (int)sprite.Height * 2 / 3,
                    (int)sprite.Width / 3,
                    (int)sprite.Height / 3
                );
            }
            else
            {
                Collider  = new Rectangle(0, 0, 32, 48);
            }            
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (Health < 10f)
            {
                // Draw stump if depleted
                DrawStump(spriteBatch);
                return;
            }
            if(Health <= 0)
            {
                Destroy();
                return;
            }
            base.Draw(spriteBatch);
            if (IsBeingHarvested)
            {
                
                DrawHealthBar(spriteBatch);
            }
        }

        public void DrawStump(SpriteBatch spriteBatch)
        {
           spirteRoot.Draw(spriteBatch, Position);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            float healthPercent = Health / MaxHealth; 
            int barWidth = 40;
            int barHeight = 4;
            int barX = (int) (Position.X + (Collider.Width + barWidth) / 2);
            int barY = (int) (Position.Y - 10);

            var whitePixel = new Texture2D(Core.GraphicsDevice, barWidth, barHeight);
            var bgRect = new Rectangle(barX, barY, barWidth, barHeight);

            spriteBatch.Draw(whitePixel, bgRect, Color.Red);

            var fgRect = new Rectangle(barX, barY, (int) (barWidth * healthPercent), barHeight);
            spriteBatch.Draw(whitePixel, fgRect, Color.Green);
        }
    }
}