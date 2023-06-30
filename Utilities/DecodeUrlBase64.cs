using System;

namespace Rumble.Platform.ReceiptService.Utilities;

public class DecodeUrlBase64
{
	public static byte[] Decode(string s)
	{
		s = s.Replace('-', '+').Replace('_', '/').PadRight(4*((s.Length+3)/4), '=');
		return Convert.FromBase64String(s);
	}
}