using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CentralBackend.Models;

namespace CentralBackend.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        
        Task<Order?> GetOrderByStringIdAsync(string orderId);
        
        Task<Order?> GetOrderByIdAsync(Guid id);
        
        Task<List<Order>> GetAllOrdersAsync();

        Task UpdateOrderAsync(Order order);
    }
}
