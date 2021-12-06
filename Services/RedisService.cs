using MongoDB.Driver;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.ReceiptService.Models;
using StackExchange.Redis;

namespace Rumble.Platform.ReceiptService.Services
{
    public class RedisService : PlatformMongoService<Receipt>
    {
        // purpose is to pull data from redis and put into mongo
        // to be removed when no longer needed

        public RedisService() : base(collection: "receipts") { }

        public int UpdateDatabase()
        {
            int counter = 0;
                // ex for when working on actual server, use env variables
                // var conn = ConnectionMultiplexer.Connect("contoso5.redis.cache.windows.net:8909,ssl=true,password=...");
                string host = PlatformEnvironment.Variable(name: "REDIS_HOST");
                string password = PlatformEnvironment.Variable(name: "REDIS_PASSWORD");
                int port = int.Parse(PlatformEnvironment.Variable(name: "REDIS_PORT"));
                
                // ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"); // change later
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(configuration: $"{host}:{port},password={password}");
                
                IDatabase db = redis.GetDatabase(); // may need to change to server instead of db to scan server (use .Keys, which lets it pick between keys and scan (???) ) 
                // means might not need a dictionary to store if all values returned are unique
                IServer server = redis.GetServer(host: host, port: port); // takes name value pair, i.e. "localhost", 6379

                // Dictionary<string, string> data = new Dictionary<string, string>(); only need if returned values from redis are not unique

                // have to use scan instead of keys to not block current redis server
                // no normal scan option. sscan for set? should just be scan, depends on structure of redis db
                // plan to use dictionary to store all keys and values -- optimize later?
                // use scan starting with cursor 0 and keep going until returned cursor is 0 again
                // because duplicates may show up, need to check if key exists in storage dictionary
                // if key does not exist, use mget to grab value for that key and add into storage dictionary
                // notes: cursor is a string representing int, only returns elements present throughout the whole iteration

                foreach (string key in server.Keys(pattern: "*")) // optimize? it takes very long but it is likely because of the redis db, have to iterate through all no matter what
                {
                    // perhaps check to see if present already in mongo and add only if not present?
                    string keyFrag = key.Substring(17); // currently assumes only aos receipts, can parse if needed for other types

                    Receipt entry = _collection.Find(filter: receipt => receipt.OrderId == keyFrag).FirstOrDefault();
                    if (entry == null)
                    {
                        string value = db.StringGet(key);
                        Receipt newReceipt = JsonConvert.DeserializeObject<Receipt>(value);
                        _collection.InsertOne(newReceipt);
                        counter++;
                        // data.Add(key, value); only need if returned values from redis are not unique
                    }
                }

                return counter;
        }
    }
}