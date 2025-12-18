using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Spatial;
using TribeBuild.Entity.NPC;
using MonoGameLibrary.PathFinding;
//using TribeBuild.Entity.NPC.Villager;

namespace TribeBuild.Entity.Resource
{

    public class Mine : Entity, IPosition
    {
        public int MaxWorkers { get; private set; }
        public float MiningDuration { get; private set; }
        public List<MineWorker> CurrentWorkers { get; private set; }
        public Vector2 EntrancePosition { get; private set; }
        public Rectangle WorkArea { get; private set; }

        private LootTable lootTable;
        private Random random = new Random();

        public Mine(int id, Vector2 pos, Sprite sprite = null) : base(id, pos)
        {
            Sprite = sprite;
            MaxWorkers = 4;
            MiningDuration = 10f;
            CurrentWorkers = new List<MineWorker>();
            
            // FIXED: Setup work area and collider properly
            if (sprite != null)
            {
                WorkArea = new Rectangle(
                    (int)pos.X,
                    (int)pos.Y,
                    (int)sprite.Width,
                    (int)sprite.Height
                );
                
                EntrancePosition = new Vector2(
                    pos.X + sprite.Width / 3,
                    pos.Y + sprite.Height
                );
                
                Collider = new Rectangle(
                    (int)pos.X + 8,
                    (int)pos.Y + 8,
                    (int)sprite.Width - 16,
                    (int)sprite.Height - 16
                );
            }
            else
            {
                WorkArea = new Rectangle((int)pos.X, (int)pos.Y, 64, 64);
                EntrancePosition = pos + new Vector2(20, 64);
                Collider = new Rectangle((int)pos.X, (int)pos.Y, 64, 64);
            }

            SetupLootTable();
        }

        private void SetupLootTable()
        {
            lootTable = new LootTable();
            lootTable.AddEntry("Stone", 3, 5, 1f);
            lootTable.AddEntry("Coal", 1, 3, 0.5f);
            lootTable.AddEntry("Bronze Ore", 1, 1, 0.3f);
            lootTable.AddEntry("Iron Ore", 1, 1, 0.2f);
            lootTable.AddEntry("Gold Ore", 1, 1, 0.1f);
            lootTable.AddEntry("Ruby", 1, 1, 0.05f);
            lootTable.AddEntry("Emerald", 1, 1, 0.01f);
            lootTable.AddEntry("Diamond", 1, 1, 0.02f);
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive || CurrentWorkers.Count == 0)
                return;
            
            // FIXED: Loop backwards properly
            for (int i = CurrentWorkers.Count - 1; i >= 0; i--)
            {
                var worker = CurrentWorkers[i];
                worker.Update(gameTime);
                
                if (!worker.IsWorking)
                {
                    CurrentWorkers.RemoveAt(i);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;
            
            // Draw mine sprite
            if (Sprite != null)
            {
                Sprite.Draw(spriteBatch, Position);
            }
            
            DrawWorkerInfo(spriteBatch);
            
            foreach (var worker in CurrentWorkers)
            {
                worker.Draw(spriteBatch);
            }
        }

        public void DrawWorkerInfo(SpriteBatch spriteBatch)
        {
            // TODO: Display worker count indicator
        }

        public bool StartMining(NPCBody npc)
        {
            if (CurrentWorkers.Count >= MaxWorkers)
                return false;
            
            var worker = new MineWorker(npc, this, MiningDuration);
            CurrentWorkers.Add(worker);
            
            npc.MoveTo(EntrancePosition);
            
            return true;
        }

        public List<LootDrop> GetMiningReward()
        {
            return lootTable.Roll();
        }

        public bool IsFull()
        {
            return CurrentWorkers.Count >= MaxWorkers;
        }

        public bool IsInWorkArea(Vector2 position)
        {
            return WorkArea.Contains(position);
        }
    }


    public class MineWorker
    {
        public NPCBody NPC {get; private set;}
        public Mine Mine {get; private set;}
        public bool IsWorking {get; private set;}
        public float WorkProgress{get; private set;}
        public float WorkDuration {get; private set;}
        private MiningState state;
        private float stateTimer;

        public MineWorker(NPCBody npc, Mine mine, float duration)
        {
            NPC = npc;
            Mine = mine;
            WorkDuration = duration;
            WorkProgress = 0f;
            IsWorking = true;
            state = MiningState.EnteringMine;
            stateTimer = 0f;
        }
        
        public void Update(GameTime gameTime)
        {
            if(!IsWorking) return;
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            stateTimer += deltaTime;
            switch (state) 
            {
                case MiningState.EnteringMine:
                    UpdateEntering();  
                    break;
                case MiningState.Mining:
                    UpdateMining(deltaTime);
                    break;
                case MiningState.ExitingMine:
                    UpdateExiting();
                    break;
                case MiningState.Completed:
                    CompleteWork();
                    break;
            }
        }
        
        private void UpdateEntering()
        {
            if(Vector2.Distance(NPC.Position, Mine.EntrancePosition) < 10f)
            {
                NPC.IsActive = false;
                state = MiningState.Mining;
                stateTimer = 0f;
            }
        }
        private void UpdateMining (float deltaTime)
        {
            WorkProgress += deltaTime;
            // hoan thanh mining sau do spawn tai cua mo
            if(WorkProgress >= WorkDuration)
            {
                state = MiningState.ExitingMine;
                stateTimer = 0f;
                NPC.Position = Mine.EntrancePosition;
                NPC.IsActive = true;
            }
        }

        private void UpdateExiting()
        {
            if(stateTimer > 1f)
            {
                state = MiningState.Completed;
            }
        }

        private void CompleteWork()
        {
            var loot = Mine.GetMiningReward();
            Console.WriteLine("Mining Complete");
            Console.WriteLine ($"Worker: {NPC.ID}");
            Console.WriteLine($"Rewards: ");
            foreach(var drop in loot)
            {
                Console.WriteLine($" + {drop.Amount} x {drop.ItemType}");
                var Villager = NPC.AI as VillagerAI;
                for(int i = 0 ; i < drop.Amount ; i++)
                {
                    Villager?.Inventory.AddItem(drop.ItemType);

                }
            }
            IsWorking = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if(state == MiningState.Mining)
            {
                DrawMiningProgress(spriteBatch);
            }
        }

        private void DrawMiningProgress(SpriteBatch spriteBatch)
        {
            float progress = WorkProgress / WorkDuration;
            int barWidth = 60;
            int barHeight = 6;
            int barX = (int) (Mine.Position.X + 18);
            int barY = (int) (Mine.Position.Y - 15);

            //spriteBatch.Draw(Rectangle(barX, barY, barWidth, barHeight))
        }
    }

    enum MiningState
    {
        EnteringMine,   // Đang đi vào mỏ
        Mining,         // Đang khai thác (NPC ẩn)
        ExitingMine,    // Đang đi ra
        Completed       // Hoàn thành
    }

    public class LootTable
    {
        private List<LootEntry> entries = new List<LootEntry>();
        private Random random = new Random();

        public void AddEntry(string itemType, int minAmount, int maxAmount, float chance)
        {
            entries.Add(new LootEntry(itemType, minAmount, maxAmount, chance));
        }

        public List<LootDrop> Roll()
        {
            var drops = new List<LootDrop>();
            foreach(var entry in entries)
            {
                float roll = (float)random.NextDouble();
                if( roll == entry.DropChance)
                {
                    int amount = random.Next(entry.MinAmount, entry.MaxAmount + 1);
                    drops.Add(new LootDrop(entry.ItemType, amount));
                }
            }
            return drops;
        }

    }

    public class LootEntry
    {
        public String ItemType{get; set;}
        public int MinAmount{get; set;}
        public int MaxAmount {get; set;}
        public float DropChance {get; set;}

        public LootEntry(string itemType, int minAmount, int maxAmount, float dropChance)
        {
            ItemType = itemType;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            DropChance = dropChance;
        }
    }

    public class LootDrop
    {
        public string ItemType {get; set;}
        public int Amount {get; set;}
        public LootDrop(string itemType, int amount)
        {
            ItemType = itemType;
            Amount = amount;
        }
    }
}