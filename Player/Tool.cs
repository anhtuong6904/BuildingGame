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
    /// ✅ Tool data class - Balanced stats
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
            AnimationName = type == ToolType.Sword ? "attack" : "work";
        }
        
        // ✅ Predefined tools with balanced stats
        public static Tool Hands => new Tool(ToolType.None, "Hands", 5f, 32f, 0.5f);
        public static Tool Axe => new Tool(ToolType.Axe, "Axe", 15f, 48f, 1.2f);
        public static Tool Pickaxe => new Tool(ToolType.Pickaxe, "Pickaxe", 12f, 48f, 1.0f);
        public static Tool Sword => new Tool(ToolType.Sword, "Sword", 25f, 64f, 0.8f);
        public static Tool Hoe => new Tool(ToolType.Hoe, "Hoe", 8f, 48f, 1.0f);
    }
    
    /// <summary>
    /// ✅ FINAL: Equipment manager with fixed controls
    /// Controls:
    /// - 1-9: Direct slot selection
    /// - Q: Previous tool
    /// - R: Next tool (changed from E to avoid conflict with Interact)
    /// </summary>
    public class EquipmentManager
    {
        private PlayerCharacter player;
        private Dictionary<int, Tool> hotbar;
        private int currentSlot = 1;
        private KeyboardState previousKeyState;
        
        public Tool CurrentTool => hotbar.ContainsKey(currentSlot) ? hotbar[currentSlot] : Tool.Hands;
        public int CurrentSlot => currentSlot;
        
        public EquipmentManager(PlayerCharacter player)
        {
            this.player = player;
            hotbar = new Dictionary<int, Tool>();
            
            // ✅ Default equipment setup
            hotbar[1] = Tool.Sword;   // Combat first
            hotbar[2] = Tool.Axe;     // Wood gathering
            hotbar[3] = Tool.Pickaxe; // Stone gathering
            hotbar[4] = Tool.Hoe;     // Farming
            
            Console.WriteLine("[Equipment] Default tools equipped");
            Console.WriteLine("[Equipment] Controls: 1-9 (Select) | Q (Previous) | R (Next)");
        }
        
        /// <summary>
        /// ✅ FIXED: Handle input without E key conflict
        /// </summary>
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
            
            // Q to cycle previous
            if (keyState.IsKeyDown(Keys.Q) && !prevKeyState.IsKeyDown(Keys.Q))
            {
                CyclePrevious();
            }
            
            // R to cycle next (changed from E to avoid Interact conflict)
            if (keyState.IsKeyDown(Keys.R) && !prevKeyState.IsKeyDown(Keys.R))
            {
                CycleNext();
            }
            
            previousKeyState = keyState;
        }
        
        /// <summary>
        /// ✅ Select specific slot
        /// </summary>
        public void SelectSlot(int slot)
        {
            if (slot < 1 || slot > 9) return;
            
            currentSlot = slot;
            string toolName = hotbar.ContainsKey(slot) ? hotbar[slot].Name : "Empty";
            Console.WriteLine($"[Equipment] Slot {slot}: {toolName}");
        }
        
        /// <summary>
        /// ✅ Cycle to next equipped tool (skip empty slots)
        /// </summary>
        public void CycleNext()
        {
            int startSlot = currentSlot;
            int attempts = 0;
            
            do
            {
                currentSlot = currentSlot >= 9 ? 1 : currentSlot + 1;
                attempts++;
                
                if (hotbar.ContainsKey(currentSlot))
                {
                    Console.WriteLine($"[Equipment] → {hotbar[currentSlot].Name}");
                    return;
                }
            } 
            while (attempts < 9);
            
            // If no tools found, stay at current
            currentSlot = startSlot;
        }
        
        /// <summary>
        /// ✅ Cycle to previous equipped tool (skip empty slots)
        /// </summary>
        public void CyclePrevious()
        {
            int startSlot = currentSlot;
            int attempts = 0;
            
            do
            {
                currentSlot = currentSlot <= 1 ? 9 : currentSlot - 1;
                attempts++;
                
                if (hotbar.ContainsKey(currentSlot))
                {
                    Console.WriteLine($"[Equipment] ← {hotbar[currentSlot].Name}");
                    return;
                }
            } 
            while (attempts < 9);
            
            currentSlot = startSlot;
        }
        
        /// <summary>
        /// ✅ Equip tool to specific slot
        /// </summary>
        public void EquipTool(int slot, Tool tool)
        {
            if (slot < 1 || slot > 9) return;
            
            hotbar[slot] = tool;
            Console.WriteLine($"[Equipment] Equipped {tool.Name} to slot {slot}");
        }
        
        /// <summary>
        /// ✅ Remove tool from slot
        /// </summary>
        public void UnequipSlot(int slot)
        {
            if (hotbar.ContainsKey(slot))
            {
                Console.WriteLine($"[Equipment] Unequipped {hotbar[slot].Name} from slot {slot}");
                hotbar.Remove(slot);
            }
        }
        
        /// <summary>
        /// ✅ Check if tool can be used
        /// </summary>
        public bool CanUseTool(ToolType type, Entity.Entity target)
        {
            if (target == null) return false;
            
            switch (type)
            {
                case ToolType.Axe:
                    return target is Entity.Resource.Tree;
                    
                case ToolType.Pickaxe:
                    return target.Name?.Contains("Rock") ?? false;
                    
                case ToolType.Sword:
                    return target is Entity.NPC.Animals.AnimalEntity || 
                           target is Entity.Enemies.NightEnemyEntity;
                    
                case ToolType.Hoe:
                    return false; // Farming not implemented yet
                    
                case ToolType.None:
                    return true; // Hands can interact with anything
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// ✅ Get hotbar for UI display
        /// </summary>
        public Dictionary<int, Tool> GetHotbar() => new Dictionary<int, Tool>(hotbar);
        
        /// <summary>
        /// ✅ Get tool effectiveness against target
        /// </summary>
        public float GetEffectiveness(ToolType type, Entity.Entity target)
        {
            if (target is Entity.Resource.Tree && type == ToolType.Axe)
                return 1.5f; // 50% bonus
            
            if (target.Name?.Contains("Rock") == true && type == ToolType.Pickaxe)
                return 1.5f;
            
            if ((target is Entity.NPC.Animals.AnimalEntity || 
                 target is Entity.Enemies.NightEnemyEntity) && 
                type == ToolType.Sword)
                return 1.3f; // 30% bonus
            
            return 1.0f; // Normal effectiveness
        }
    }
}