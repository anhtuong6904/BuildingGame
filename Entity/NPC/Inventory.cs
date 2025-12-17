using System.Collections.Generic;
using System.Linq;

namespace TribeBuild.Entity.NPC
{
    /// <summary>
    /// Simple inventory system for NPCs
    /// </summary>
    public class Inventory
    {
        private List<string> items;
        public int MaxCapacity { get; private set; }
        
        public int Count => items.Count;
        
        public Inventory(int maxCapacity)
        {
            MaxCapacity = maxCapacity;
            items = new List<string>();
        }
        
        public bool AddItem(string item)
        {
            if (IsFull()) return false;
            
            items.Add(item);
            return true;
        }
        
        public bool RemoveItem(string item)
        {
            return items.Remove(item);
        }
        
        public bool HasItem(string item)
        {
            return items.Contains(item);
        }
        
        public bool HasFood()
        {
            return items.Any(i => i == "berry" || i == "meat" || i == "food");
        }
        
        public bool ConsumeFood()
        {
            var food = items.FirstOrDefault(i => i == "berry" || i == "meat" || i == "food");
            if (food != null)
            {
                items.Remove(food);
                return true;
            }
            return false;
        }
        
        public bool IsFull()
        {
            return items.Count >= MaxCapacity;
        }
        
        public void Clear()
        {
            items.Clear();
        }
        
        public List<string> GetAllItems()
        {
            return new List<string>(items);
        }
        
        public int GetItemCount(string itemType)
        {
            return items.Count(i => i == itemType);
        }
    }
}