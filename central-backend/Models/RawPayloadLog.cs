using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CentralBackend.Models
{
    public class RawPayloadLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("payload_type")]
        public string PayloadType { get; set; } = string.Empty; // Shopify, Zapier

        [BsonElement("raw_json")]
        public string RawJson { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("headers")]
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}
