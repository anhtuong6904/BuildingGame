using System;
using System.Collections.Generic;

namespace TribeBuild.Player
{
    /// <summary>
    /// ✅ Simple inventory system (compatible with original code)
    /// </summary>
    public class PlayerInventory
    {
        private Dictionary<string, int> items;
        public int MaxSlots { get; private set; }
        
        // ✅ Stack limits for different items
        private static readonly Dictionary<string, int> StackLimits = new Dictionary<string, int>
        {
            { "wood", 99 },
            { "softwood", 99 },
            { "stone", 99 },
            { "berry", 50 },
            { "meat", 50 }
        };

        public PlayerInventory(int maxSlots)
        {
            items = new Dictionary<string, int>();
            MaxSlots = maxSlots;
        }

        /// <summary>
        /// ✅ Add item with smart stacking
        /// </summary>
        public bool AddItem(string itemName, int amount)
        {
            if (amount <= 0) return false;
            
            // If item exists, add to stack
            if (items.ContainsKey(itemName))
            {
                items[itemName] += amount;
                Console.WriteLine($"[Inventory] +{amount} {itemName} (Total: {items[itemName]})");
                return true;
            }
            
            // Add new item if there's space
            if (items.Count < MaxSlots)
            {
                items[itemName] = amount;
                Console.WriteLine($"[Inventory] +{amount} {itemName} (New item)");
                return true;
            }
            
            Console.WriteLine("[Inventory] Full!");
            return false;
        }

        /// <summary>
        /// ✅ Remove item
        /// </summary>
        public bool RemoveItem(string itemName, int amount)
        {
            if (!items.ContainsKey(itemName) || items[itemName] < amount)
            {
                Console.WriteLine($"[Inventory] Not enough {itemName}!");
                return false;
            }
            
            items[itemName] -= amount;
            
            if (items[itemName] <= 0)
            {
                items.Remove(itemName);
            }
            
            return true;
        }

        /// <summary>
        /// ✅ Check if item exists
        /// </summary>
        public bool HasItem(string itemName, int amount = 1)
        {
            return items.ContainsKey(itemName) && items[itemName] >= amount;
        }

        /// <summary>
        /// ✅ Get item count
        /// </summary>
        public int GetItemCount(string itemName)
        {
            return items.ContainsKey(itemName) ? items[itemName] : 0;
        }

        /// <summary>
        /// ✅ Get all items (for UI display)
        /// </summary>
        public Dictionary<string, int> GetAllItems() => new Dictionary<string, int>(items);

        /// <summary>
        /// ✅ Get inventory stats
        /// </summary>
        public (int used, int total) GetSlotInfo()
        {
            return (items.Count, MaxSlots);
        }

        /// <summary>
        /// ✅ Clear inventory
        /// </summary>
        public void Clear()
        {
            items.Clear();
            Console.WriteLine("[Inventory] Cleared");
        }
    }
}