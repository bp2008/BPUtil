using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestClass]
	public class TestIdObfuscator
	{
		[TestMethod]
		public void Obfuscate_Deobfuscate_RoundTrips()
		{
			IdObfuscator obfuscator = new IdObfuscator(IdKeyGenerator.GenerateKeys());
			uint[] ids = new uint[] { 0u, 1u, 42u, 123456789u, uint.MaxValue };

			for (int i = 0; i < ids.Length; i++)
			{
				uint id = ids[i];
				Assert.AreEqual(id, obfuscator.Deobfuscate(obfuscator.Obfuscate(id)), "Round-trip failed for id " + id.ToString(CultureInfo.InvariantCulture) + ".");
			}
		}

		[TestMethod]
		public void StringConstructor_ParsesSameAsUintConstructor()
		{
			string key = IdKeyGenerator.GenerateKeys();
			string[] parts = key.Split(new char[] { ':' }, StringSplitOptions.None);
			uint prime = uint.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
			uint inverse = uint.Parse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
			uint xor = uint.Parse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture);

			IdObfuscator fromString = new IdObfuscator(key);
			IdObfuscator fromUints = new IdObfuscator(prime, inverse, xor);

			Assert.AreEqual(fromUints.Obfuscate(1001u), fromString.Obfuscate(1001u));
			Assert.AreEqual(fromUints.Deobfuscate(2002u), fromString.Deobfuscate(2002u));
		}

		[TestMethod]
		public void StringConstructor_TrimsWhitespaceAroundParts()
		{
			IdObfuscator reference = new IdObfuscator(3u, 2863311531u, 7u);
			IdObfuscator spaced = new IdObfuscator(" 3 : 2863311531 : 7 ");

			Assert.AreEqual(reference.Obfuscate(11u), spaced.Obfuscate(11u));
		}

		[TestMethod]
		public void Constructor_NullKey_ThrowsArgumentNullException()
		{
			Expect.Exception<ArgumentNullException>(() =>
			{
				new IdObfuscator((string)null);
			});
		}

		[TestMethod]
		public void Constructor_WrongPartCount_ThrowsFormatException()
		{
			Expect.Exception<FormatException>(() => { new IdObfuscator("1:2"); });
			Expect.Exception<FormatException>(() => { new IdObfuscator("1:2:3:4"); });
			Expect.Exception<FormatException>(() => { new IdObfuscator(""); });
		}

		[TestMethod]
		public void Constructor_InvalidNumbers_ThrowsFormatException()
		{
			Expect.Exception<FormatException>(() => { new IdObfuscator("a:b:c"); });
			Expect.Exception<FormatException>(() => { new IdObfuscator("-1:2:3"); });
			Expect.Exception<FormatException>(() => { new IdObfuscator("1:2:999999999999999999999"); });
		}
	}

	[TestClass]
	public class TestIdKeyGenerator
	{
		[TestMethod]
		public void GenerateKeys_ProducesThreeColonSeparatedDecimalUints()
		{
			string key = IdKeyGenerator.GenerateKeys();

			Assert.IsFalse(string.IsNullOrEmpty(key));
			string[] parts = key.Split(new char[] { ':' }, StringSplitOptions.None);
			Assert.AreEqual(3, parts.Length);

			uint prime = uint.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
			uint inverse = uint.Parse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
			uint xor = uint.Parse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture);

			Assert.IsTrue(prime > 1u, "Expected a nontrivial prime.");
			Assert.AreEqual(1u, prime % 2u, "Prime should be odd.");
			ulong product = (ulong)prime * (ulong)inverse;
			const ulong modulus = 1UL << 32;
			Assert.AreEqual(1u, (uint)(product % modulus), "Inverse must satisfy prime * inverse ≡ 1 (mod 2^32).");
		}

		[TestMethod]
		public void GenerateKeys_OutputIsStableShape()
		{
			for (int i = 0; i < 20; i++)
			{
				string key = IdKeyGenerator.GenerateKeys();
				string[] parts = key.Split(new char[] { ':' }, StringSplitOptions.None);
				Assert.AreEqual(3, parts.Length);
				foreach (string part in parts)
				{
					Assert.IsFalse(string.IsNullOrWhiteSpace(part));
					uint.Parse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
				}
			}
		}
	}
}
