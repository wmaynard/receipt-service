using System;
using System.IO;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Rumble.Platform.Common.Utilities.JsonTools;
// ReSharper disable InconsistentNaming

namespace Rumble.Platform.ReceiptService.Utilities;

public class GenerateJWT
{
	public static string GenerateJWTToken(RumbleJson header, RumbleJson payload, string rsaPrivateKey)
	{
		RSAParameters rsaParams = GetRsaParameters(rsaPrivateKey);
		IJwtEncoder encoder = GetRS256JWTEncoder(rsaParams);

		string token = encoder.Encode(header, payload, Array.Empty<byte>());

		return token;
	}

	private static IJwtEncoder GetRS256JWTEncoder(RSAParameters rsaParams)
	{
		RSACryptoServiceProvider csp = new();
		csp.ImportParameters(rsaParams);

		RS256Algorithm algorithm = new(csp, csp);
		JsonNetSerializer serializer = new();
		JwtBase64UrlEncoder urlEncoder = new();
		JwtEncoder encoder = new(algorithm, serializer, urlEncoder);

		return encoder;
	}

	private static RSAParameters GetRsaParameters(string rsaPrivateKey)
	{
		// use Bouncy Castle to convert the private key to RSA parameters
		using (StringReader stringReader = new (rsaPrivateKey))
		{
			PemReader pemReader = new (stringReader);
			RsaPrivateCrtKeyParameters privateRsaParams = pemReader.ReadObject() as RsaPrivateCrtKeyParameters;
			return DotNetUtilities.ToRSAParameters(privateRsaParams);
		}
	}
}