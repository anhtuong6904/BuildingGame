using System;
using System.Collections.Generic;

namespace TribeBuild.Player
{
    public class PlayerInventory
    {
         private Dictionary<string, int> items;
        public int MaxSlots { get; private set; }

        public PlayerInventory(int maxSlots)
        {
            items = new Dictionary<string, int>();
            MaxSlots = maxSlots;
        }

        public bool AddItem(string itemName, int amount)
        {
            if (items.ContainsKey(itemName))
            {
                items[itemName] += amount;
            }
            else if (items.Count < MaxSlots)
            {
                items[itemName] = amount;
            }
            else
            {
                Console.WriteLine("[Inventory] Full!");
                return false;
            }
            
            Console.WriteLine($"[Inventory] +{amount} {itemName} (Total: {items[itemName]})");
            return true;
        }

        public bool RemoveItem(string itemName, int amount)
        {
            if (!items.ContainsKey(itemName) || items[itemName] < amount)
                return false;
            
            items[itemName] -= amount;
            if (items[itemName] <= 0)
                items.Remove(itemName);
            
            return true;
        }

        public Dictionary<string, int> GetAllItems() => new Dictionary<string, int>(items);
    }
}