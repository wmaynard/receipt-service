using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.ReceiptService.Models
{
    public class AppleValidation : Validation
    {
        
    }
}

/*
status: 'redeemed',
message: "Receipt is already redeemed",
offerId: it['product_id'] ?: ""

status       : 'success',
response     : itunesResponse.receipt,
transactionId: transactionId,
offerId      : it['product_id'],
receiptKey   : itunesReceiptKey,
receiptData  : it.toString(),
ts           : it.purchase_date_ms ?: System.currentTimeMillis()
*/