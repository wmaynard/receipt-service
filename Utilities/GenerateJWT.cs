using System;
using System.IO;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using RCL.Logging;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
// ReSharper disable InconsistentNaming

namespace Rumble.Platform.ReceiptService.Utilities;

public class GenerateJWT
{
	public static string GenerateJWTToken(RumbleJson header, RumbleJson payload, string rsaPrivateKey)
	{
		Log.Info(owner: Owner.Nathan, message: "Test log step 1.", data: $"header: {header}. payload: {payload}.");
		var rsaParams = GetRsaParameters(rsaPrivateKey);
		Log.Info(owner: Owner.Nathan, message: "Test log step 2.", data: $"rsaParams: {rsaParams}.");
		var encoder = GetRS256JWTEncoder(rsaParams);
		Log.Info(owner: Owner.Nathan, message: "Test log step 3.", data: $"encoder: {encoder}.");

		var token = encoder.Encode(header, payload, Array.Empty<byte>());
		Log.Info(owner: Owner.Nathan, message: "Test log step 4.", data: $"token: {token}.");

		return token;
	}

	private static IJwtEncoder GetRS256JWTEncoder(RSAParameters rsaParams)
	{
		var csp = new RSACryptoServiceProvider();
		csp.ImportParameters(rsaParams);

		var algorithm = new RS256Algorithm(csp, csp);
		var serializer = new JsonNetSerializer();
		var urlEncoder = new JwtBase64UrlEncoder();
		var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

		return encoder;
	}

	private static RSAParameters GetRsaParameters(string rsaPrivateKey)
	{
		// use Bouncy Castle to convert the private key to RSA parameters
		using (var stringReader = new StringReader(rsaPrivateKey))
		{
			var pemReader = new PemReader(stringReader);
			var privateRsaParams = pemReader.ReadObject() as RsaPrivateCrtKeyParameters;
			Log.Info(owner: Owner.Nathan, message: "Test log to check null ref.", data: $"pemReader: {pemReader}. privateRsaParams: {privateRsaParams}.");
			return DotNetUtilities.ToRSAParameters(privateRsaParams);
		}
	}
}