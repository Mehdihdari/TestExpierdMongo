using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Globalization;

namespace TestExpierdMongo
{
    public interface IUserRepository
    {
        Task AddUser(User user, CancellationToken cancellationToken);
    }

    public class User
    {
        public string Id { get; set; }
        public DateTime CreationTime { get; set; }
        public string Name { get; set; }
    }
    public class UserMongoDbRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly ILogger<UserMongoDbRepository> _logger;

        public UserMongoDbRepository(ILogger<UserMongoDbRepository> logger)
        {
            _logger = logger;
            var mongoClient = new MongoClient("mongodb://172.16.20.99:27017/");
            var database = mongoClient.GetDatabase("ExpireTest");
            _userCollection = database.GetCollection<User>("ExpireTest");
            CreateExpireIndex();
        }

        public async Task AddUser(User user, CancellationToken cancellationToken)
        {
            await _userCollection.InsertOneAsync(user, null, cancellationToken);
        }
        public async Task<User> GetUserById(string id, CancellationToken cancellationToken)
        {
            var filter = Builders<User>.Filter.Eq(a => a.Id, id);

            var result = await _userCollection.FindAsync(filter, cancellationToken: cancellationToken);

            return await result.FirstOrDefaultAsync(cancellationToken);
        }


        private void CreateExpireIndex()
        {
            var isValidExpireFormat = TimeSpan.TryParseExact("00:00:00:20",
                "dd':'hh':'mm':'ss",
                CultureInfo.InvariantCulture,
                out TimeSpan expireAfter);

            if (isValidExpireFormat)
            {
                const string _expireAt = "ExpireAt";
                var expireIndex = new IndexKeysDefinitionBuilder<User>().Ascending(c => c.CreationTime);

                var index = _userCollection.Indexes.List().ToList()
                    .Where(a => a["name"] == _expireAt)
                    .Select(a => new
                    {
                        ExpireAfterSeconds = a.GetElement("expireAfterSeconds").Value.ToString()
                    }).FirstOrDefault();

                if (index != null)
                {
                    var registeredExpireAfter = TimeSpan.FromSeconds(Convert.ToInt64(index.ExpireAfterSeconds));
                    if (registeredExpireAfter == expireAfter)
                    {
                        return;
                    }
                    _userCollection.Indexes.DropOne(_expireAt);
                    _logger.LogInformation($"Index deleted. {registeredExpireAfter.ToString("dd':'hh':'mm':'ss")}");
                }

                _userCollection.Indexes.CreateOne(new CreateIndexModel<User>(expireIndex, new CreateIndexOptions
                {
                    Name = _expireAt,
                    ExpireAfter = expireAfter
                }));
                _logger.LogInformation($"Index created. {expireAfter.ToString("dd':'hh':'mm':'ss")}");
            }
        }
    }
}
