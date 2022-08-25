using System.Net.Http;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services
{
    public abstract class VerificationService : PlatformMongoService<Receipt>
    {
        // other verification services build off this
        // need to pick the collection based on property in receipt? one collection for all?
        public static readonly HttpClient client = new HttpClient();

        protected VerificationService() : base(collection: "receipts")
        { }

        public bool Exists(string orderId)
        {
            if (_collection.Find(filter: receipt => receipt.OrderId == orderId) == null)
            {
                return false;
            }
            return true;
        }
    }
}