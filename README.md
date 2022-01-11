# Receipt Service
A service for verifying `receipts` for IAP purchases.

# Introduction
This service allows for checking if a `receipt` for IAP purchases is valid. There are three types of `receipts` that may be verified: **Apple** (`ios`), **GooglePlay** (`android`)
and **Samsung**. **Apple** and **Samsung** `receipts` require an external API to verify the `receipts` with their respective App stores, while **Google** provides a `RSA public key` to
verify on a local server. The service also temporarily allows users to fetch `receipt` data from the current _Redis_ database and migrate them over to the _Mongo_ database.

# Required Environment Variables
| Variable | Description |
| ---: | :--- |
| GRAPHITE | Link to hosted _graphite_ for analytics and monitoring. |
| LOGGLY_URL | Link to _Loggly_ to analyze logs in greater detail. |
| MONGODB_NAME | The _MongoDB_ name which the service connects to. |
| MONGODB_URI | The connection string for the environment's _MongoDB_. |
| RUMBLE_COMPONENT | The name of the service. |
| RUMBLE_DEPLOYMENT | Signifies the deployment environment. |
| RUMBLE_KEY | Key to validate for each deployment environment. |
| RUMBLE_TOKEN_VALIDATION | Link to current validation for player tokens. |
| RUMBLE_TOKEN_VERIFICATION | Link to current validation for admin tokens. Will include player tokens in the future. |
| VERBOSE_LOGGING | Logs in greater detail. |

# Temporary Variables
| Variable | Description |
| ---: | :--- |
| iosVerifyReceiptUrl | External API to verify **Apple** `receipts`. |
| iosVerifyReceiptSandbox | Testing external API to verify **Apple** `receipts`. |
| sharedSecret | Password for external API to verify **Apple** `receipts`. |
| samsungVerifyReceiptUrl | Base external API to verify **Samsung** `receipts`. The `orderId` is appended to the end of this. |
| androidStoreKey | The `RSA public key` provided by **Google** to verify `receipts`. |
| REDIS_HOST | The `hosted` location of the current _Redis_ database. |
| REDIS_PASSWORD | The `password` to the current _Redis_ database. |
| REDIS_PORT | The `port` with which the current _Redis_ database is accessed. |

# Glossary
| Term | Description |
| ---: | :--- |
| Receipt | Contains the data relevant to the purchase to be verified. |
| game | Information to determine what `game` the `receipt` is for. For now, only `tower` is accepted. |
| account | `Account ID` for the account performing the purchase. Not currently in use but required for the future. |
| channel | Determines which App store to verify with. This can be `ios`, `aos`, or `samsung`. |
| orderId | Unique identifier for the `receipt`. Used by the services to verify with the appropriate App stores. |
| packageName | Identifier for the product on the App store. |
| productId | Identifier for the product or item that was purchased. |
| purchaseTime | `Unix timestamp` for the purchase time. Old migrated data has this in milliseconds. |
| purchaseState | 0 or 1 depending on the state of the purchase. |
| purchaseToken | Token attached to a `receipt` for verification purposes. |
| acknowledged | Leftover boolean from old version of `receipt-service` that does not appear to have any functionality. |
| id | _Mongo_ identifier for a receipt. |

# Using the Service
All non-health endpoints require a valid admin token from `token-service`.
Requirements to these endpoints should have an `Authorization` header with a `Bearer {token}`, where token is the aforementioned admin token.

All `timestamps` in the service are in the format of a `Unix timestamp`. This is to allow consistency and reduce confusion between time zones.

# Endpoints
All endpoints are reached with the base route `/commerce/receipt/`. Any following endpoints listed are appended on to the base route.

**Example**: `GET /commerce/receipt/heatlh`

## Top Level
| Method | Endpoint | Description | Required Parameters | Google-specific Parameters |
| ---: | :--- | :--- | :--- | :--- |
| GET | `/health` | **INTERNAL** Health check on the status of the following services: `AppleService`, `GoogleService`, `SamsungService`, `RedisService` |  |  |
| POST | `/` | **INTERNAL** Submit a `receipt` with other required information to be verified. | *string*`game`<br />*string*`account`<br />*string*`channel`<br />*Receipt*`receipt` | *string*`signature` |
| GET | `/redis` | **INTERNAL** Fetch all entries in the current _Redis_ database and migrate them over to _Mongo_ |  |  |

### Notes
A `Receipt` is structured the same way for all three services and are transformed according to what the external APIs require.

**Google `POST` Example**
```
{
    "game": "tower",
    "account": "6140bd998caf79f468e6f8a6",
    "channel": "aos",
    "receipt": {
        "orderId": "GPA.3383-6680-9846-20466",
        "packageName": "com.rumbleentertainment.towerdefense",
        "productId": "tower_9999",
        "purchaseTime": 1613194937777,
        "purchaseState": 0,
        "purchaseToken": "chgkiahhjcnaeoehmbgoeghl.AO-J1OwO6VEgk6LtLx5yAn6jhw5EtcxCqYMLMiMesA07VuYUmN1bzchmWtnRS6TM0X-ujIvqq4bXkjmPsgWq2LSG-K35AUqLyVhj2lZLmcdD-3xnvPEXgV8"
    },
    "signature": "pQhVfMteppjDARcGIUBo1okTU56DJnL44/vwWGcHyvL45Bx+yagd7Ns9y1KkyiqIXnxG60lpQEyn21aM3+53BhBuABFQe6C6Npu7bWoascOsOfufMkgybISr8vYI7a6106pr2LKhESUC+QuOVX7rzMnupCrxSwL02wFottKFKi88YhawVPNlK4DMpi1iRpvtLrhEF3k8a6nIMLxTcyo3V9fBTYzWG1wf64Jln9DTW/IE20rxBpiEjGfsPxnJ7bN3uGCKULNc1rxEkLNWrrlycoPmo5kso2KxQjFbmd5N4EVXrKNtKVuMXoxuUMPYF/N8lLUSR818BeEYQYVvu+qEpw=="
}
```

Specifically for **GooglePlay** verification, a `signature` field must be provided. This should be provided with the original `receipt` on purchase.
**Google** does not provide `RSA private keys`. The original `receipt` data must also not be modified, as it needs to match the original encrypted data to verify correctly.

None of the services currently make use of the `account` field, but this is present for consistency's sake and any future changes that may want the relevant account.

# Future Updates
- The current temporary environment variables that hold the external API urls, `passwords`, and `RSA public keys` may be changed to use `dynamic config` instead.
- **Apple** and **Samsung** verifications should require further testing. There are no current **Apple** or **Samsung** purchases to use to test.
- **Google** verification require further testing. The example provided by **v1**'s documentation may be outdated.
- All code related to `RedisService` and its corresponding endpoint should be removed once this service is fully functional.

# Troubleshooting
Any issues should be recorded as a log in _Loggly_. Please reach out if something does not work property to figure out the issue.