using System;
using System.Security.Cryptography;

namespace Armine.Utils
{
	public static class Hash
	{
		private static readonly HashAlgorithm hashAlgo = MD5.Create();

		public static string ComputeHash(byte[] data, int offset, int count)
		{
			return Convert.ToBase64String(hashAlgo.ComputeHash(data, offset, count));
		}
	}
}
