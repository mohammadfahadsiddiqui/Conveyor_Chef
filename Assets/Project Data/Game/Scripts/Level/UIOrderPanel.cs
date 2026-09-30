using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BusStop
{
    public class UIOrderPanel : MonoBehaviour
    {
        [SerializeField] Transform orderItemsContainer;
        [SerializeField] UIOrderItem orderItemPrefab;
        
        [Header("Bus Sprites")]
        [SerializeField] BusTypeSprite[] busTypeSprites;
        
        private Dictionary<LevelElement.Type, UIOrderItem> orderItems;
        private OrderTracker orderTracker;
        
        public void Initialize(LevelElement.Type[] busSpawnQueue)
        {
            // Clear existing items
            ClearOrderItems();
            
            // Create order tracker
            orderTracker = new OrderTracker(busSpawnQueue);
            orderItems = new Dictionary<LevelElement.Type, UIOrderItem>();
            
            // Create UI items for each order
            Dictionary<LevelElement.Type, OrderData> orders = orderTracker.GetOrders();
            foreach (var orderPair in orders)
            {
                LevelElement.Type busType = orderPair.Key;
                OrderData orderData = orderPair.Value;
                
                // Create UI item
                UIOrderItem item = Instantiate(orderItemPrefab, orderItemsContainer);
                Sprite busSprite = GetBusSprite(busType);
                item.Initialize(busType, orderData.requiredAmount, busSprite);
                
                orderItems[busType] = item;
            }

            // Lay the cards out for this many orders straight away (no one-frame jump).
            OrderPanelAdaptiveGrid grid = orderItemsContainer != null ? orderItemsContainer.GetComponent<OrderPanelAdaptiveGrid>() : null;
            if (grid != null)
                grid.Refresh();
        }
        
        public void OnBusCompleted(LevelElement.Type busType)
        {
            if (orderTracker != null)
            {
                orderTracker.MarkBusCompleted(busType);
                
                // Update UI
                if (orderItems.ContainsKey(busType))
                {
                    OrderData orderData = orderTracker.GetOrders()[busType];
                    orderItems[busType].UpdateProgress(orderData.completedAmount);
                }
                
                // Check if all orders complete
                if (orderTracker.AreAllOrdersComplete())
                {
                    Debug.Log("All orders completed!");
                }
            }
        }
        
        private Sprite GetBusSprite(LevelElement.Type busType)
        {
            // Orders show the same country dish as the board.
            Sprite dish = CountryFood.GetDish(busType);
            if (dish != null)
                return dish;

            foreach (var busSprite in busTypeSprites)
            {
                if (busSprite.busType == busType)
                    return busSprite.sprite;
            }
            return null;
        }
        
        private void ClearOrderItems()
        {
            if (orderItems != null)
            {
                foreach (var item in orderItems.Values)
                {
                    if (item == null)
                        continue;
                    item.transform.SetParent(null, false);
                    Destroy(item.gameObject);
                }
                orderItems.Clear();
            }

            // Clear container. Detach first: Destroy is deferred, and old cards must not
            // count towards the new layout.
            for (int i = orderItemsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = orderItemsContainer.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
        
        [System.Serializable]
        public class BusTypeSprite
        {
            public LevelElement.Type busType;
            public Sprite sprite;
        }
    }
}