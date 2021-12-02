namespace Rumble.Platform.ReceiptService.Models
{
    public class GoogleValidation : Validation
    {
        public GoogleValidation(string status, Receipt response, string transactionId, string offerId, string receiptKey, string receiptData, long timestamp)
            : base(status: status, response: response, transactionId: transactionId, offerId: offerId, receiptKey: receiptKey, receiptData: receiptData, timestamp: timestamp)
        {
            
        }
    }
}

// don't actually need an external service
/*
status:'redeemed',
message:"Receipt is already redeemed",
offerId: receipt.productId ?: ""

status       : 'success',
response     : receipt,
transactionId: transactionId,
offerId      : receipt.productId,
receiptKey   : receiptKey,
receiptData  : sreceipt,
ts           : receipt.purchaseTime as String ?: System.currentTimeMillis()
*/