using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using MonoGameLibrary.Graphics;

namespace TribeBuild.Entity.Resource
{
    //cac vat pham co the thu hoach duoc
    public enum BushType
    {
        Berry,//quả mọng
    }
    public class Bush : ResourceEntity
    {
        public BushType BushType{get; private set;}
        public Sprite spriteCanHarvested{get; set;}
        public Sprite sprite1{get; set;}
        private bool hasBeenHarvested;

        private bool CanHarvested;
        private const float TimerCanHarvested = 30f;
        private float Timer = 0;

        public Bush(int id, Vector2 pos, BushType bushType = BushType.Berry, Sprite sprite = null)
        :base(id, pos, ResourceType.Bush)
        {
            BushType = bushType;
            Sprite = sprite;
            sprite1 = sprite;
            hasBeenHarvested = false;

            switch (bushType)
            {
                case BushType.Berry:
                    MaxHealth = 1f;
                    YieldAmount = 3;
                    YieldItem = "berry";
                    RespawnTime = 60f;
                    break;
                // case BushType.Herb:
                //     MaxHealth = 1f;
                //     YieldAmount = 2;
                //     YieldItem = "herd";
                //     RespawnTime = 45f;
                //     break;
            }

            Health = MaxHealth;
            CanRespawn = true;
            int width = Sprite._region.Width;
            int height = Sprite._region.Height;
            Collider = new Rectangle ((int)Position.X, (int) Position.Y, width, height / 2);
        }

        public override void Harvest(float damage)
        {
            if(!hasBeenHarvested && CanHarvested)
            {
                hasBeenHarvested = true;
                
                OnDepleted();
                CanHarvested = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            Timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(Timer >= TimerCanHarvested)
            {
                CanHarvested = true;
                Timer = 0 ;
            }
        }

        protected override void Respawn()
        {
            base.Respawn();
            hasBeenHarvested = false;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive)
            {
                return;
            }
            base.Draw(spriteBatch);
        }
    }
}