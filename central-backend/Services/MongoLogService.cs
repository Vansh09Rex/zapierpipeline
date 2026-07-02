using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CentralBackend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CentralBackend.Services
{
    public class MongoLogService
    {
        private readonly IMongoCollection<RawPayloadLog>? _collection;
        private readonly ILogger<MongoLogService> _logger;

        public MongoLogService(IConfiguration configuration, ILogger<MongoLogService> logger)
        {
            _logger = logger;
            try
            {
                var connectionString = configuration.GetConnectionString("MongoConnection") 
                    ?? "mongodb://localhost:27017";
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("zapier_pipeline_logs");
                _collection = database.GetCollection<RawPayloadLog>("raw_payload_logs");
                _logger.LogInformation("Successfully connected to MongoDB.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MongoDB connection. Logging to MongoDB will be disabled.");
            }
        }

        public async Task LogPayloadAsync(string payloadType, string rawJson, Dictionary<string, string> headers)
        {
            if (_collection == null)
            {
                _logger.LogWarning("MongoDB collection is not initialized. Skipping payload log.");
                return;
            }

            try
            {
                var logEntry = new RawPayloadLog
                {
                    PayloadType = payloadType,
                    RawJson = rawJson,
                    Timestamp = DateTime.UtcNow,
                    Headers = headers
                };

                await _collection.InsertOneAsync(logEntry);
                _logger.LogInformation("Raw payload logged to MongoDB for type: {PayloadType}", payloadType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while writing raw payload to MongoDB.");
            }
        }
    }
}
