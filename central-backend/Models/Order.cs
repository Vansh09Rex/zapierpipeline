using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralBackend.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("order_id")]
        [MaxLength(100)]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        [Column("customer_email")]
        [MaxLength(255)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("sync_status")]
        [MaxLength(50)]
        public string SyncStatus { get; set; } = "Pending"; // Pending, Synced, Failed

        [Column("zoho_record_id")]
        [MaxLength(100)]
        public string? ZohoRecordId { get; set; }

        [Required]
        [Column("source")]
        [MaxLength(50)]
        public string Source { get; set; } = string.Empty; // Shopify, Zapier
    }
}
