using System;
using System.IO;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Rumble.Platform.Data;
// ReSharper disable InconsistentNaming

namespace Rumble.Platform.ReceiptService.Utilities;

public class GenerateJWT
{
	public static string GenerateJWTToken(RumbleJson header, RumbleJson payload, string rsaPrivateKey)
	{
		var rsaParams = GetRsaParameters(rsaPrivateKey);
		var encoder = GetRS256JWTEncoder(rsaParams);

		var token = encoder.Encode(header, payload, Array.Empty<byte>());

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
			return DotNetUtilities.ToRSAParameters(privateRsaParams);
		}
	}
}