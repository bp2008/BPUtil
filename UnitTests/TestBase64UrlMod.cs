using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;

namespace UnitTests
{
	[TestClass]
	public class TestBase64UrlMod
	{
		[TestMethod]
		public void TestBase64UrlModBasic()
		{
			// Test encoding
			byte[] input1 = new byte[256];
			for (int i = 0; i < input1.Length; i++)
				input1[i] = (byte)i;
			string expectedOutput1 = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0-P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6e3x9fn-AgYKDhIWGh4iJiouMjY6PkJGSk5SVlpeYmZqbnJ2en6ChoqOkpaanqKmqq6ytrq-wsbKztLW2t7i5uru8vb6_wMHCw8TFxsfIycrLzM3Oz9DR0tPU1dbX2Nna29zd3t_g4eLj5OXm5-jp6uvs7e7v8PHy8_T19vf4-fr7_P3-_w";
			Assert.AreEqual(expectedOutput1, Base64UrlMod.ToBase64UrlMod(input1));

			byte[] input2 = new byte[512];
			for (int i = 0; i < input2.Length; i++)
				input2[i] = (byte)(i / 2);
			string expectedOutput2 = "AAABAQICAwMEBAUFBgYHBwgICQkKCgsLDAwNDQ4ODw8QEBEREhITExQUFRUWFhcXGBgZGRoaGxscHB0dHh4fHyAgISEiIiMjJCQlJSYmJycoKCkpKiorKywsLS0uLi8vMDAxMTIyMzM0NDU1NjY3Nzg4OTk6Ojs7PDw9PT4-Pz9AQEFBQkJDQ0RERUVGRkdHSEhJSUpKS0tMTE1NTk5PT1BQUVFSUlNTVFRVVVZWV1dYWFlZWlpbW1xcXV1eXl9fYGBhYWJiY2NkZGVlZmZnZ2hoaWlqamtrbGxtbW5ub29wcHFxcnJzc3R0dXV2dnd3eHh5eXp6e3t8fH19fn5_f4CAgYGCgoODhISFhYaGh4eIiImJioqLi4yMjY2Ojo-PkJCRkZKSk5OUlJWVlpaXl5iYmZmampubnJydnZ6en5-goKGhoqKjo6SkpaWmpqenqKipqaqqq6usrK2trq6vr7CwsbGysrOztLS1tba2t7e4uLm5urq7u7y8vb2-vr-_wMDBwcLCw8PExMXFxsbHx8jIycnKysvLzMzNzc7Oz8_Q0NHR0tLT09TU1dXW1tfX2NjZ2dra29vc3N3d3t7f3-Dg4eHi4uPj5OTl5ebm5-fo6Onp6urr6-zs7e3u7u_v8PDx8fLy8_P09PX19vb39_j4-fn6-vv7_Pz9_f7-__8";
			Assert.AreEqual(expectedOutput2, Base64UrlMod.ToBase64UrlMod(input2));

			// Test decoding
			Assert.AreEqual(string.Join(",", input1), string.Join(",", Base64UrlMod.FromBase64UrlMod(expectedOutput1)));
			Assert.AreEqual(string.Join(",", input2), string.Join(",", Base64UrlMod.FromBase64UrlMod(expectedOutput2)));

			// Decoding should also accept standard Base64
			Assert.AreEqual(string.Join(",", input2), string.Join(",", Base64UrlMod.FromBase64UrlMod(Convert.ToBase64String(input2))));
		}
	}
}
