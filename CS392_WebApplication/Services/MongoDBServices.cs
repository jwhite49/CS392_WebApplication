using MongoDB.Bson;
using MongoDB.Driver;

namespace CS392_WebApplication.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoDBService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDBSettings:ConnectionString"]
                ?? throw new InvalidOperationException("MongoDBSettings:ConnectionString is not configured.");
            var databaseName = configuration["MongoDBSettings:DatabaseName"]
                ?? throw new InvalidOperationException("MongoDBSettings:DatabaseName is not configured.");
            var collectionName = configuration["MongoDBSettings:CollectionName"]
                ?? throw new InvalidOperationException("MongoDBSettings:CollectionName is not configured.");

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task<Dictionary<string, List<string>>> GetCatalogAsync()
        {
            var doc = await _collection.Find(_ => true).FirstOrDefaultAsync();
            if (doc == null) return new Dictionary<string, List<string>>();

            var result = new Dictionary<string, List<string>>();
            foreach (var element in doc)
            {
                if (element.Name == "_id") continue;
                if (element.Value.IsBsonArray)
                {
                    result[element.Name] = element.Value.AsBsonArray
                        .Select(v => v.AsString)
                        .ToList();
                }
            }
            return result;
        }
    }
}
