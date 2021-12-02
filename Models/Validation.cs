using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService.Models
{
    public abstract class Validation : PlatformDataModel
    {
        
    }
}

// other validations build off this