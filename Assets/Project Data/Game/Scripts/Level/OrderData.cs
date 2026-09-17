using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Tracks order requirements and completion for a level
    /// </summary>
    [System.Serializable]
    public class OrderTracker
    {
        private Dictionary<LevelElement.Type, OrderData> orders;
        
        public OrderTracker(LevelElement.Type[] busSpawnQueue)
        {
            orders = new Dictionary<LevelElement.Type, OrderData>();
            
            // Count how many of each bus type are required
            foreach (LevelElement.Type busType in busSpawnQueue)
            {
                if (orders.ContainsKey(busType))
                {
                    orders[busType].requiredAmount++;
                }
                else
                {
                    orders[busType] = new OrderData
                    {
                        busType = busType,
                        requiredAmount = 1,
                        completedAmount = 0
                    };
                }
            }
        }
        
        public Dictionary<LevelElement.Type, OrderData> GetOrders()
        {
            return orders;
        }
        
        public void MarkBusCompleted(LevelElement.Type busType)
        {
            if (orders.ContainsKey(busType))
            {
                orders[busType].completedAmount++;
                Debug.Log($"Bus {busType} completed: {orders[busType].completedAmount}/{orders[busType].requiredAmount}");
            }
        }
        
        public bool IsOrderComplete(LevelElement.Type busType)
        {
            if (orders.ContainsKey(busType))
            {
                return orders[busType].completedAmount >= orders[busType].requiredAmount;
            }
            return false;
        }
        
        public bool AreAllOrdersComplete()
        {
            foreach (var order in orders.Values)
            {
                if (order.completedAmount < order.requiredAmount)
                    return false;
            }
            return true;
        }
    }
    
    [System.Serializable]
    public class OrderData
    {
        public LevelElement.Type busType;
        public int requiredAmount;
        public int completedAmount;
        
        public float Progress => (float)completedAmount / requiredAmount;
        public bool IsComplete => completedAmount >= requiredAmount;
    }
}