
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace TribeBuild.Player
{
    public enum ToolType
    {
        None,
        Axe,        // Chặt cây
        Pickaxe,    // Đào đá
        Sword,      // Đánh quái
        Hoe         // Cuốc đất
    }
    
    /// <summary>
    /// Tool data class
    /// </summary>
    public class Tool
    {
        public ToolType Type { get; set; }
        public string Name { get; set; }
        public float Damage { get; set; }
        public float Range { get; set; }
        public float Cooldown { get; set; }
        public string AnimationName { get; set; }
        
        public Tool(ToolType type, string name, float damage, float range, float cooldown)
        {
            Type = type;
            Name = name;
            Damage = damage;
            Range = range;
            Cooldown = cooldown;
            AnimationName = "work"; // Default work animation
        }
        
        // Predefined tools
        public static Tool Axe => new Tool(ToolType.Axe, "Axe", 10f, 48f, 1f);
        public static Tool Pickaxe => new Tool(ToolType.Pickaxe, "Pickaxe", 8f, 48f, 1f);
        public static Tool Sword => new Tool(ToolType.Sword, "Sword", 20f, 64f, 1f);
        public static Tool Hoe => new Tool(ToolType.Hoe, "Hoe", 5f, 48f, 1f);
        public static Tool Hands => new Tool(ToolType.None, "Hands", 5f, 32f, 1f);
    }
    
    /// <summary>
    /// Equipment manager for player
    /// </summary>
    public class EquipmentManager
    {
        private PlayerCharacter player;
        private Dictionary<int, Tool> hotbar; // Key slots 1-9
        private int currentSlot = 1;
        
        public Tool CurrentTool => hotbar.ContainsKey(currentSlot) ? hotbar[currentSlot] : Tool.Hands;
        public int CurrentSlot => currentSlot;
        
        public EquipmentManager(PlayerCharacter player)
        {
            this.player = player;
            hotbar = new Dictionary<int, Tool>();
            
            // Default equipment
            hotbar[1] = Tool.Sword;   // Slot 1: Sword
            hotbar[2] = Tool.Axe;     // Slot 2: Axe
            hotbar[3] = Tool.Pickaxe; // Slot 3: Pickaxe
        }
        
        public void HandleInput(KeyboardState keyState, KeyboardState prevKeyState)
        {
            // Number keys 1-9 to switch tools
            for (int i = 1; i <= 9; i++)
            {
                Keys key = (Keys)((int)Keys.D1 + i - 1);
                if (keyState.IsKeyDown(key) && !prevKeyState.IsKeyDown(key))
                {
                    SelectSlot(i);
                }
            }
            
            // Mouse wheel to cycle tools
            // (handled in PlayerCharacter.Update)
        }
        
        public void SelectSlot(int slot)
        {
            if (slot < 1 || slot > 9) return;
            
            currentSlot = slot;
            Console.WriteLine($"[Equipment] Selected slot {slot}: {CurrentTool.Name}");
        }
        
        public void CycleNext()
        {
            currentSlot = currentSlot >= 9 ? 1 : currentSlot + 1;
            Console.WriteLine($"[Equipment] Selected slot {currentSlot}: {CurrentTool.Name}");
        }
        
        public void CyclePrevious()
        {
            currentSlot = currentSlot <= 1 ? 9 : currentSlot - 1;
            Console.WriteLine($"[Equipment] Selected slot {currentSlot}: {CurrentTool.Name}");
        }
        
        public void EquipTool(int slot, Tool tool)
        {
            if (slot < 1 || slot > 9) return;
            hotbar[slot] = tool;
        }
        
        public Dictionary<int, Tool> GetHotbar() => new Dictionary<int, Tool>(hotbar);
    }
}
