using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides methods to obfuscate and deobfuscate unsigned integer IDs using a reversible mathematical transformation.
	/// </summary>
	public class IdObfuscator
	{
		private readonly uint _prime;
		private readonly uint _inverse;
		private readonly uint _xor;

		/// <summary>
		/// Initializes a new instance of the <see cref="IdObfuscator"/> class with the specified prime, inverse, and XOR values.
		/// </summary>
		/// <param name="prime">A prime number used for multiplication in the obfuscation process.</param>
		/// <param name="inverse">The modular multiplicative inverse of the prime, used for deobfuscation.</param>
		/// <param name="xor">A value used for XOR operation in the obfuscation process.</param>
		public IdObfuscator(uint prime, uint inverse, uint xor)
		{
			_prime = prime;
			_inverse = inverse;
			_xor = xor;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IdObfuscator"/> class using a key string produced by <see cref="IdKeyGenerator.GenerateKeys"/>.
		/// </summary>
		/// <param name="key">A key in the form <c>prime:inverse:xor</c>, with each component a base-10 unsigned integer.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
		/// <exception cref="FormatException">Thrown when <paramref name="key"/> is not a valid key (wrong shape or invalid numbers).</exception>
		public IdObfuscator(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			string[] parts = key.Split(new char[] { ':' }, StringSplitOptions.None);
			if (parts.Length != 3)
			{
				throw new FormatException("The key must contain exactly three colon-separated values.");
			}

			if (!uint.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsedPrime)
				|| !uint.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsedInverse)
				|| !uint.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsedXor))
			{
				throw new FormatException("Each key component must be a valid base-10 unsigned integer.");
			}

			_prime = parsedPrime;
			_inverse = parsedInverse;
			_xor = parsedXor;
		}

		/// <summary>
		/// Obfuscates the specified unsigned integer ID.
		/// </summary>
		/// <param name="id">The ID to obfuscate.</param>
		/// <returns>The obfuscated ID as an unsigned integer.</returns>
		public uint Obfuscate(uint id) => (id * _prime) ^ _xor;

		/// <summary>
		/// Deobfuscates the specified obfuscated unsigned integer ID.
		/// </summary>
		/// <param name="obfuscatedId">The obfuscated ID to deobfuscate.</param>
		/// <returns>The original ID as an unsigned integer.</returns>
		public uint Deobfuscate(uint obfuscatedId) => (obfuscatedId ^ _xor) * _inverse;
	}

	/// <summary>
	/// Generates random cryptographic parameters (prime, modular inverse, XOR) for use with <see cref="IdObfuscator"/>.
	/// </summary>
	public static class IdKeyGenerator
	{
		private static readonly Random _rng = new Random();

		/// <summary>
		/// Creates a new random key suitable for constructing an <see cref="IdObfuscator"/>.
		/// </summary>
		/// <returns>A string in the form <c>prime:inverse:xor</c>, with each component formatted in base 10.</returns>
		public static string GenerateKeys()
		{
			uint prime = GetRandomUint();
			// The prime must be odd to be coprime with 2^32
			if (prime % 2 == 0)
			{
				prime++;
			}

			// Ensure it is actually prime (or at least coprime to 2^32)
			while (!IsPrime(prime))
			{
				prime += 2;
			}

			uint inverse = ModInverse(prime);
			uint xor = GetRandomUint();

			return prime.ToString(CultureInfo.InvariantCulture) + ":"
				+ inverse.ToString(CultureInfo.InvariantCulture) + ":"
				+ xor.ToString(CultureInfo.InvariantCulture);
		}

		private static uint GetRandomUint()
		{
			byte[] buffer = new byte[4];
			_rng.NextBytes(buffer);
			return BitConverter.ToUInt32(buffer, 0);
		}

		private static uint ModInverse(uint n)
		{
			long a = n, b = 4294967296; // 2^32
			long x0 = 0, x1 = 1;
			while (a > 1)
			{
				long q = a / b;
				long t = b; b = a % b; a = t;
				t = x0; x0 = x1 - q * x0; x1 = t;
			}
			return (uint)x1;
		}

		private static bool IsPrime(uint n)
		{
			if (n <= 1)
			{
				return false;
			}

			if (n <= 3)
			{
				return true;
			}

			if (n % 2 == 0 || n % 3 == 0)
			{
				return false;
			}

			for (uint i = 5; i * i <= n; i += 6)
			{
				if (n % i == 0 || n % (i + 2) == 0)
				{
					return false;
				}
			}

			return true;
		}
	}
}
