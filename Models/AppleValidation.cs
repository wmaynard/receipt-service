namespace Rumble.Platform.ReceiptService.Models
{
    public class AppleValidation : Validation
    {
        public AppleValidation(string status, Receipt response, string transactionId, string offerId, string receiptKey, string receiptData, long timestamp) 
            : base(status: status, response: response, transactionId: transactionId, offerId: offerId, receiptKey: receiptKey, receiptData: receiptData, timestamp: timestamp)
        { }
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