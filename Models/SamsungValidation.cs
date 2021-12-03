using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.ReceiptService.Models
{
    public class SamsungValidation : Validation
    {
        
    }
}

/*
status:'redeemed',
message:"Receipt is already redeemed",
response:samsungResponse,
offerId: samsungResponse.itemId ?: ""

status       : 'success',
response     : samsungResponse,
transactionId: samsungResponse.paymentId,
offerId      : samsungResponse.itemId,
receiptKey   : receiptKey,
receiptData  : "${samsungResponse as JSON}".toString(),
ts           : System.currentTimeMillis() as String //Documentation is private
*/