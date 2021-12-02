namespace Rumble.Platform.ReceiptService.Models
{
    public class SamsungValidation : Validation
    {
        public SamsungValidation(string status, Receipt response, string transactionId, string offerId, string receiptKey, string receiptData, long timestamp)
            : base(status: status, response: response, transactionId: transactionId, offerId: offerId, receiptKey: receiptKey, receiptData: receiptData, timestamp: timestamp)
        {
            
        }
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