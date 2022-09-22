# Receipt Service
A service for verifying `receipts` for IAP purchases.

# Introduction
This service allows for checking if a `receipt` for IAP purchases is valid. There are two types of `receipts` that may be 
verified: **GooglePlay** (`android`) and **Apple** (`ios`). **Google** provides a `RSA public key` to verify on a local 
server while **Apple** `receipts` require an external API to verify the `receipts` with the App store. The service 
also temporarily allows users to fetch `receipt` data from the current _Redis_ database and migrate them over to the 
_Mongo_ database.

# Required Environment Variables
|                  Variable | Description                                                                            |
|--------------------------:|:---------------------------------------------------------------------------------------|
|                  GRAPHITE | Link to hosted _graphite_ for analytics and monitoring.                                |
|                LOGGLY_URL | Link to _Loggly_ to analyze logs in greater detail.                                    |
|              MONGODB_NAME | The _MongoDB_ name which the service connects to.                                      |
|               MONGODB_URI | The connection string for the environment's _MongoDB_.                                 |
|          RUMBLE_COMPONENT | The name of the service.                                                               |
|         RUMBLE_DEPLOYMENT | Signifies the deployment environment.                                                  |
|                RUMBLE_KEY | Key to validate for each deployment environment.                                       |
|   RUMBLE_TOKEN_VALIDATION | Link to current validation for player tokens.                                          |
| RUMBLE_TOKEN_VERIFICATION | Link to current validation for admin tokens. Will include player tokens in the future. |
|           VERBOSE_LOGGING | Logs in greater detail.                                                                |

# Temporary Variables
|                Variable | Description                                                                                                             |
|------------------------:|:------------------------------------------------------------------------------------------------------------------------|
|     iosVerifyReceiptUrl | External API to verify **Apple** `receipts`. Not currently in use.                                                      |
| iosVerifyReceiptSandbox | Testing external API to verify **Apple** `receipts`. Not currently in use.                                              |
|            sharedSecret | Password for external API to verify **Apple** `receipts`.                                                               |
|         androidStoreKey | The `RSA public key` provided by **Google** to verify `receipts`.                                                       |
|              REDIS_HOST | The `hosted` location of the current _Redis_ database.                                                                  |
|          REDIS_PASSWORD | The `password` to the current _Redis_ database.                                                                         |
|              REDIS_PORT | The `port` with which the current _Redis_ database is accessed.                                                         |

# Glossary
|          Term | Description                                                                                          |
|--------------:|:-----------------------------------------------------------------------------------------------------|
|       receipt | Contains the data relevant to the purchase to be verified.                                           |
|          game | Information to determine what `game` the `receipt` is for. For now, only `tower`'s is accepted.      |
|       account | `Account ID` for the account performing the purchase.                                                |
|       channel | Determines which App store to verify with. This can be `ios` or `aos`.                               |
|       orderId | Unique identifier for the `receipt`. Used by the services to verify with the appropriate App stores. |
|   packageName | Identifier for the product on the App store.                                                         |
|     productId | Identifier for the product or item that was purchased.                                               |
|  purchaseTime | `Unix timestamp` for the purchase time. Old migrated data has this in milliseconds.                  |
| purchaseState | 0 or 1 depending on the state of the purchase.                                                       |
| purchaseToken | Token attached to a `receipt` for verification purposes.                                             |
|      quantity | Integer present in a `receipt` for the number of the package purchased.                              |
|  acknowledged | Boolean present in a `receipt`.                                                                      |
|            id | _Mongo_ identifier for a receipt.                                                                    |

# Using the Service
All non-health endpoints require a valid admin token from `token-service`.
Requirements to these endpoints should have an `Authorization` header with a `Bearer {token}`, where token is the aforementioned admin token.

All `timestamps` in the service are in the format of a `Unix timestamp`. This is to allow consistency and reduce confusion between time zones.

# Endpoints
All endpoints are reached with the base route `/commerce/`. Any following endpoints listed are appended on to the base route.

**Example**: `GET /commerce/health`

## Top Level
| Method | Endpoint   | Description                                                                                                                          | Required Parameters                                                                  | Google-specific Parameters |
|-------:|:-----------|:-------------------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------------------------------------------|:---------------------------|
|    GET | `/health`  | **INTERNAL** Health check on the status of the following services: `AppleService`, `GoogleService`, `ReceiptService`, `RedisService` |                                                                                      |                            |
|   POST | `/receipt` | **INTERNAL** Submit a `receipt` with other required information to be verified.                                                      | *string*`game`<br />*string*`account`<br />*string*`channel`<br />*Receipt*`receipt` | *string*`signature`        |

## Admin
| Method | Endpoint  | Description                                                                                     | Required Parameters |
|-------:|:----------|:------------------------------------------------------------------------------------------------|:--------------------|
|    GET | `/all`    | **INTERNAL** Fetch all receipts in the database and sort by transaction timestamp               |                     |
|    GET | `/player` | **INTERNAL** Fetch all receipts in the database for a player and sort by transaction timestamp  | *string*`accountId` |
|    GET | `/redis`  | **INTERNAL** Fetch all entries in the current _Redis_ database and migrate them over to _Mongo_ |                     |


### Notes
A `receipt` is structured the same way for all three services and are transformed according to what the external APIs require.

**Google `POST` Example**
```
{
    "game": "57901c6df82a45708018ba73b8d16004",
    "account": "631a47b6f524478c48253655",
    "channel": "aos",
    "receipt": {
        "orderId": "GPA.3390-3229-4373-85961",
        "packageName": "com.plarium.towerheroes",
        "productId": "tower_2499",
        "purchaseTime": 1663091723679,
        "purchaseState": 0,
        "purchaseToken": "klppcbomgghpedbliphjbpep.AO-J1Oy82eVaWnMFpjkmo3o1nuXm-wiEbsWhNa97DhOHUDAoCUrv-uzy58kra7FX0V_1_feD-1cs8jHJmGbs452fffJMqLI_ksbAQc8sKh_hS82wWlb_IqY",
        "quantity": 1,
        "acknowledged": false
    },
    "signature": "wPdgnq5ZeBnlnDYLTM0PeWt1I12rmAn3+/ZA6DhY3NkDnlXOFJQ+YSzLyfIjvqHItI7+ghHp6871rMArRdIFaYWUcr+gfbb84UnV0eCw3s9Oy295GZKkbGrnLEzo+nc+0Vj2dGuBTx1YxJhncPmApQixJoR1pATRAuvfXfAMZu7Gr876CGVbCbkdbavYMConAjzUJKbfkcxclPZttEbiVIMe7OcoLjXSbUQ/fmtsD1Jw9Hr73FDyxRnh8t+X+ZFxnVd2QUaoqx8bqiQ3NXQBV1wkU41A8rVO8YZuDN27+2dvAUOg07Yl+CmszqBxUIHG3uiBow/U5SJ1Hf4QsdXhrQ=="
}
```

For **Google** verification, a `signature` field must be provided. This should be provided with the original `receipt` on purchase.
**Google** does not provide `RSA private keys`. The original `receipt` data must also not be modified, as it needs to match the original encrypted data to verify correctly.

None of the old receipt data use the `account` field, but this is present for ongoing `receipts` to link them to the relevant account.

# Future Updates
- The current temporary environment variables that hold the external API urls, `passwords`, and `RSA public keys` may be changed to use `dynamic config` instead.
- **Apple** verifications will require updates. There are no current **Apple** purchases to use to test.
- **Google** `receipt` structure has historically changed unexpectedly. There is a fallback to work around this but models should be updated when this happens.
- RSA verification for **Google** `receipts` are considered outdated. There is an API that we can switch to using at some point, but will require a key from the Google Play store.
- All code related to `RedisService` and its corresponding endpoint should be removed once it is no longer required.

# Troubleshooting
Any issues should be recorded as a log in _Loggly_. Please reach out if something does not work properly to figure out the issue.