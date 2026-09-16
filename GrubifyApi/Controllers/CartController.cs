using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using GrubifyApi.Models;

namespace GrubifyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        // Thread-safe in-memory cart storage with bounded capacity
        // In production, replace with a distributed cache (e.g. Redis) or database
        private static readonly ConcurrentDictionary<string, (Cart Cart, DateTime LastAccessed)> UserCarts = new();

        // Maximum number of carts to keep in memory to prevent unbounded growth
        private const int MaxCartEntries = 1000;

        // Cart expiration time — idle carts are evicted after this period
        private static readonly TimeSpan CartExpiration = TimeSpan.FromHours(2);

        [HttpGet("{userId}")]
        public ActionResult<Cart> GetCart(string userId)
        {
            EvictExpiredCarts();

            var entry = UserCarts.GetOrAdd(userId, _ => (new Cart { UserId = userId }, DateTime.UtcNow));
            // Update last-accessed timestamp
            UserCarts[userId] = (entry.Cart, DateTime.UtcNow);
            return Ok(entry.Cart);
        }

        [HttpPost("{userId}/items")]
        public ActionResult<Cart> AddItemToCart(string userId, [FromBody] AddCartItemRequest request)
        {
            EvictExpiredCarts();

            // Enforce max cart entries to prevent unbounded memory growth
            if (!UserCarts.ContainsKey(userId) && UserCarts.Count >= MaxCartEntries)
            {
                return StatusCode(503, "Cart service is at capacity. Please try again later.");
            }

            var entry = UserCarts.GetOrAdd(userId, _ => (new Cart { UserId = userId }, DateTime.UtcNow));
            var cart = entry.Cart;
            UserCarts[userId] = (cart, DateTime.UtcNow);

            var existingItem = cart.Items.FirstOrDefault(i => i.FoodItemId == request.FoodItemId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.SpecialInstructions = request.SpecialInstructions;
            }
            else
            {
                var newItem = new CartItem
                {
                    Id = cart.Items.Count + 1,
                    FoodItemId = request.FoodItemId,
                    FoodItem = GetFoodItemById(request.FoodItemId),
                    Quantity = request.Quantity,
                    SpecialInstructions = request.SpecialInstructions
                };
                cart.Items.Add(newItem);
            }

            return Ok(cart);
        }

        [HttpPut("{userId}/items/{itemId}")]
        public ActionResult<Cart> UpdateCartItem(string userId, int itemId, [FromBody] UpdateCartItemRequest request)
        {
            if (!UserCarts.TryGetValue(userId, out var entry))
            {
                return NotFound("Cart not found");
            }

            var cart = entry.Cart;
            UserCarts[userId] = (cart, DateTime.UtcNow);
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                return NotFound("Item not found in cart");
            }

            item.Quantity = request.Quantity;
            item.SpecialInstructions = request.SpecialInstructions;

            return Ok(cart);
        }

        [HttpDelete("{userId}/items/{itemId}")]
        public ActionResult<Cart> RemoveItemFromCart(string userId, int itemId)
        {
            if (!UserCarts.TryGetValue(userId, out var entry))
            {
                return NotFound("Cart not found");
            }

            var cart = entry.Cart;
            UserCarts[userId] = (cart, DateTime.UtcNow);
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                return NotFound("Item not found in cart");
            }

            cart.Items.Remove(item);
            return Ok(cart);
        }

        [HttpDelete("{userId}")]
        public ActionResult ClearCart(string userId)
        {
            // Remove the entire cart entry from the dictionary to free memory
            UserCarts.TryRemove(userId, out _);
            return Ok();
        }

        /// <summary>
        /// Evicts carts that have not been accessed within the expiration window.
        /// Called on each request to keep memory bounded without a background timer.
        /// </summary>
        private static void EvictExpiredCarts()
        {
            var cutoff = DateTime.UtcNow - CartExpiration;
            foreach (var key in UserCarts.Keys)
            {
                if (UserCarts.TryGetValue(key, out var entry) && entry.LastAccessed < cutoff)
                {
                    UserCarts.TryRemove(key, out _);
                }
            }
        }

        // Helper method to get food item (in production, this would query the database)
        private FoodItem GetFoodItemById(int foodItemId)
        {
            // This is a simplified version - in production, inject the FoodItems service
            var foodItems = new List<FoodItem>
            {
                new FoodItem { Id = 1, Name = "Margherita Pizza", Price = 16.99m, ImageUrl = "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=400&h=300&fit=crop", RestaurantId = 1 },
                new FoodItem { Id = 2, Name = "Chicken Alfredo", Price = 19.99m, ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3dbc353d2e5?w=400&h=300&fit=crop", RestaurantId = 1 },
                new FoodItem { Id = 3, Name = "Caesar Salad", Price = 12.99m, ImageUrl = "https://images.unsplash.com/photo-1546793665-c74683f339c1?w=400&h=300&fit=crop", RestaurantId = 1 },
                new FoodItem { Id = 4, Name = "California Roll", Price = 14.99m, ImageUrl = "https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=400&h=300&fit=crop", RestaurantId = 2 },
                new FoodItem { Id = 5, Name = "Spicy Tuna Roll", Price = 16.99m, ImageUrl = "https://images.unsplash.com/photo-1617196034796-73dfa7b1fd56?w=400&h=300&fit=crop", RestaurantId = 2 },
                new FoodItem { Id = 6, Name = "Chicken Teriyaki Bowl", Price = 18.99m, ImageUrl = "https://images.unsplash.com/photo-1546069901-eacef0df6022?w=400&h=300&fit=crop", RestaurantId = 2 },
                new FoodItem { Id = 7, Name = "Chicken Tikka Masala", Price = 17.99m, ImageUrl = "https://images.unsplash.com/photo-1565557623262-b51c2513a641?w=400&h=300&fit=crop", RestaurantId = 3 },
                new FoodItem { Id = 8, Name = "Vegetable Biryani", Price = 15.99m, ImageUrl = "https://images.unsplash.com/photo-1563379091339-03246963d17a?w=400&h=300&fit=crop", RestaurantId = 3 },
                new FoodItem { Id = 9, Name = "Garlic Naan", Price = 4.99m, ImageUrl = "https://images.unsplash.com/photo-1601050690597-df0568f70950?w=400&h=300&fit=crop", RestaurantId = 3 },
                new FoodItem { Id = 10, Name = "Classic Cheeseburger", Price = 13.99m, ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=400&h=300&fit=crop", RestaurantId = 4 },
                new FoodItem { Id = 11, Name = "Crispy Chicken Sandwich", Price = 15.99m, ImageUrl = "https://images.unsplash.com/photo-1606755962773-d324e9a13086?w=400&h=300&fit=crop", RestaurantId = 4 },
                new FoodItem { Id = 12, Name = "Sweet Potato Fries", Price = 6.99m, ImageUrl = "https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400&h=300&fit=crop", RestaurantId = 4 },
                new FoodItem { Id = 13, Name = "Quinoa Buddha Bowl", Price = 14.99m, ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=400&h=300&fit=crop", RestaurantId = 5 },
                new FoodItem { Id = 14, Name = "Acai Berry Smoothie", Price = 8.99m, ImageUrl = "https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=400&h=300&fit=crop", RestaurantId = 5 },
                new FoodItem { Id = 15, Name = "Grilled Salmon Salad", Price = 18.99m, ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=400&h=300&fit=crop", RestaurantId = 5 }
            };

            return foodItems.FirstOrDefault(f => f.Id == foodItemId) ?? new FoodItem();
        }
    }

    public class AddCartItemRequest
    {
        public int FoodItemId { get; set; }
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; } = string.Empty;
    }
}
