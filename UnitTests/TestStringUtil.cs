using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestStringUtil
	{
		[TestMethod]
		public void TestHtmlAttributeEncode()
		{
			for (char c = (char)0; c < 128; c++)
			{
				string input = c.ToString();
				string result = StringUtil.HtmlAttributeEncode(input);
				if (c == '\'')
					Assert.AreEqual("&#39;", result);
				else if (c == '"')
					Assert.AreEqual("&quot;", result);
				else if (c == '<')
					Assert.AreEqual("&lt;", result);
				else if (c == '>')
					Assert.AreEqual("&gt;", result);
				else if (c == '&')
					Assert.AreEqual("&amp;", result);
				else
					Assert.AreEqual(input, result);
			}
		}

		[TestMethod]
		public void TestSplitIntoSegments()
		{
			//           "0----+----1----+----2----+----3----+----4----+----5----+----6----+-";
			//           "0123456789012345678901234567890123456789012345678901234567890123456";
			string str = "Now is the time for all good men to come to the aid of their party.";

			List<string> actual = StringUtil.SplitIntoSegments(str, 10, false);
			Assert.AreEqual(7, actual.Count);
			for (int i = 0; i < 6; i++)
				Assert.AreEqual(str.Substring(i * 10, 10), actual[i]);
			Assert.AreEqual(str.Substring(60, 7), actual[6]);

			actual = StringUtil.SplitIntoSegments(str, str.Length, true);
			Assert.AreEqual(1, actual.Count);
			Assert.AreEqual(str, actual[0]);
		}

		[TestMethod]
		public void TestSplitIntoSegments_SmartBreak()
		{
			//           "0----+----1----+----2----+----3----+----4----+----5----+----6----+-";
			//           "0123456789012345678901234567890123456789012345678901234567890123456";
			string str = "Now is the time for all good men to come to the aid of their party.";

			List<string> actual = StringUtil.SplitIntoSegments(str, 10, true);
			Assert.AreEqual(9, actual.Count);
			Assert.AreEqual("Now is ", actual[0]);
			Assert.AreEqual("the time ", actual[1]);
			Assert.AreEqual("for all ", actual[2]);
			Assert.AreEqual("good men ", actual[3]);
			Assert.AreEqual("to come ", actual[4]);
			Assert.AreEqual("to the ", actual[5]);
			Assert.AreEqual("aid of ", actual[6]);
			Assert.AreEqual("their ", actual[7]);
			Assert.AreEqual("party.", actual[8]);

			//    "0----+----1----+----2----+";
			//    "01234567890123456789012345";
			str = "Aaaaaaaaaa bbbbbbbbbbbbbb.";
			// There are 10 letter 'A'/'a' and 14 letter 'b'.
			actual = StringUtil.SplitIntoSegments(str, 7, true);
			Assert.AreEqual(5, actual.Count);
			Assert.AreEqual("Aaaaaaa", actual[0]); // No word breaks found in first segment, so it is max length (7 chars)
			Assert.AreEqual("aaa ", actual[1]); // Word break found
			Assert.AreEqual("bbbbbbb", actual[2]); // Word break found only at the start. A buggy function might infinite loop here.
			Assert.AreEqual("bbbbbbb", actual[3]); // No word breaks found
			Assert.AreEqual(".", actual[4]); // Word break found

			actual = StringUtil.SplitIntoSegments(str, 8, true);
			Assert.AreEqual(4, actual.Count);
			Assert.AreEqual("Aaaaaaaa", actual[0]);
			Assert.AreEqual("aa ", actual[1]);
			Assert.AreEqual("bbbbbbbb", actual[2]);
			Assert.AreEqual("bbbbbb.", actual[3]);

			actual = StringUtil.SplitIntoSegments(str, 9, true);
			Assert.AreEqual(4, actual.Count);
			Assert.AreEqual("Aaaaaaaaa", actual[0]);
			Assert.AreEqual("a ", actual[1]);
			Assert.AreEqual("bbbbbbbbb", actual[2]);
			Assert.AreEqual("bbbbb.", actual[3]);

			actual = StringUtil.SplitIntoSegments(str, 10, true);
			Assert.AreEqual(4, actual.Count);
			Assert.AreEqual("Aaaaaaaaaa", actual[0]);
			Assert.AreEqual(" ", actual[1]);
			Assert.AreEqual("bbbbbbbbbb", actual[2]);
			Assert.AreEqual("bbbb.", actual[3]);

			actual = StringUtil.SplitIntoSegments(str, 11, true);
			Assert.AreEqual(3, actual.Count);
			Assert.AreEqual("Aaaaaaaaaa ", actual[0]);
			Assert.AreEqual("bbbbbbbbbbb", actual[1]);
			Assert.AreEqual("bbb.", actual[2]);

			actual = StringUtil.SplitIntoSegments(str, 12, true);
			Assert.AreEqual(3, actual.Count);
			Assert.AreEqual("Aaaaaaaaaa ", actual[0]);
			Assert.AreEqual("bbbbbbbbbbbb", actual[1]);
			Assert.AreEqual("bb.", actual[2]);

			actual = StringUtil.SplitIntoSegments(str, 13, true);
			Assert.AreEqual(3, actual.Count);
			Assert.AreEqual("Aaaaaaaaaa ", actual[0]);
			Assert.AreEqual("bbbbbbbbbbbbb", actual[1]);
			Assert.AreEqual("b.", actual[2]);

			//    "0----+--_--1-_---+_----2----+-";
			//    "01234567_8901_2345_67890123456";
			str = "Aaa bbb\nccc\rddd\teee.fff ggg";
			for (int splitSize = 4; splitSize <= 6; splitSize++)
			{
				actual = StringUtil.SplitIntoSegments(str, 4, true);
				Assert.AreEqual(7, actual.Count);
				Assert.AreEqual("Aaa ", actual[0]);
				Assert.AreEqual("bbb\n", actual[1]);
				Assert.AreEqual("ccc\r", actual[2]);
				Assert.AreEqual("ddd\t", actual[3]);
				Assert.AreEqual("eee.", actual[4]);
				Assert.AreEqual("fff ", actual[5]);
				Assert.AreEqual("ggg", actual[6]);
			}
			actual = StringUtil.SplitIntoSegments(str, 7, true);
			Assert.AreEqual(6, actual.Count);
			Assert.AreEqual("Aaa ", actual[0]);
			Assert.AreEqual("bbb\n", actual[1]);
			Assert.AreEqual("ccc\r", actual[2]);
			Assert.AreEqual("ddd\t", actual[3]);
			Assert.AreEqual("eee.", actual[4]);
			Assert.AreEqual("fff ggg", actual[5]);

			for (int splitSize = 8; splitSize <= 11; splitSize++)
			{
				actual = StringUtil.SplitIntoSegments(str, splitSize, true);
				Assert.AreEqual(4, actual.Count);
				Assert.AreEqual("Aaa bbb\n", actual[0]);
				// \r and \n are preferred break points, otherwise this would go up to the tab character.
				Assert.AreEqual("ccc\r", actual[1]);
				Assert.AreEqual("ddd\teee.", actual[2]);
				Assert.AreEqual("fff ggg", actual[3]);
			}

			for (int splitSize = 12; splitSize <= 14; splitSize++)
			{
				actual = StringUtil.SplitIntoSegments(str, 12, true);
				Assert.AreEqual(3, actual.Count);
				Assert.AreEqual("Aaa bbb\nccc\r", actual[0]);
				Assert.AreEqual("ddd\teee.fff ", actual[1]);
				Assert.AreEqual("ggg", actual[2]);
			}

			for (int splitSize = 15; splitSize < str.Length; splitSize++)
			{
				actual = StringUtil.SplitIntoSegments(str, 15, true);
				Assert.AreEqual(2, actual.Count);
				// \r and \n are preferred break points, otherwise the first substring would break later
				Assert.AreEqual("Aaa bbb\nccc\r", actual[0]);
				Assert.AreEqual("ddd\teee.fff ggg", actual[1]);
			}

			actual = StringUtil.SplitIntoSegments(str, str.Length, true);
			Assert.AreEqual(1, actual.Count);
			Assert.AreEqual(str, actual[0]);
		}
		[TestMethod]
		public void TestSplitIntoSegments_SegmentLength()
		{
			// Test segment lengths from -1 to 6 with an input string of length 4.
			// Values less than 1 are expected to be treated as if they were 1
			string str = "test";
			List<string> actual;
			for (int x = 0; x < 2; x++)
			{
				bool smartSplit = x == 0;
				for (int i = -1; i <= 1; i++)
				{
					actual = StringUtil.SplitIntoSegments(str, i, smartSplit);
					Assert.AreEqual(str.Length, actual.Count);
					for (int n = 0; n < str.Length; n++)
						Assert.AreEqual(str[n].ToString(), actual[n]);
				}
				actual = StringUtil.SplitIntoSegments(str, 2, smartSplit);
				Assert.AreEqual(2, actual.Count);
				Assert.AreEqual("te", actual[0]);
				Assert.AreEqual("st", actual[1]);

				actual = StringUtil.SplitIntoSegments(str, 3, smartSplit);
				Assert.AreEqual(2, actual.Count);
				Assert.AreEqual("tes", actual[0]);
				Assert.AreEqual("t", actual[1]);

				for (int i = 4; i <= 6; i++)
				{
					actual = StringUtil.SplitIntoSegments(str, i, smartSplit);
					Assert.AreEqual(1, actual.Count);
					Assert.AreEqual(str, actual[0]);
				}
			}
		}
		[TestMethod]
		public void TestReplaceMultipleCharToString()
		{
			string input = "abcdefghijklmnopqrstuvwxyz";
			Dictionary<char, string> replacementMap = new Dictionary<char, string>();
			replacementMap['c'] = "b";
			replacementMap['f'] = "gg";
			replacementMap['g'] = "b";
			replacementMap['h'] = "b";
			replacementMap['o'] = "ooooo";
			replacementMap['n'] = "o";
			replacementMap['p'] = "o";
			replacementMap['r'] = "";
			string expectedOutput = "abbdeggbbijklmoooooooqstuvwxyz";
			Assert.AreEqual(expectedOutput, StringUtil.ReplaceMultiple(input, replacementMap));
		}
		[TestMethod]
		public void TestReplaceMultipleCharToChar()
		{
			string input = "abcdefghijklmnopqrstuvwxyz";
			Dictionary<char, char> replacementMap = new Dictionary<char, char>();
			replacementMap['c'] = 'b';
			replacementMap['f'] = 'g';
			replacementMap['g'] = 'b';
			replacementMap['h'] = 'b';
			replacementMap['i'] = 'i';
			string expectedOutput = "abbdegbbijklmnopqrstuvwxyz";
			Assert.AreEqual(expectedOutput, StringUtil.ReplaceMultiple(input, replacementMap));
		}
		[TestMethod]
		public void TestRepairBase64Padding()
		{
			string source = "UnitTests";
			/*
				[1] U:         VQ==
				[2] Un:        VW4=
				[3] Uni:       VW5p
				[4] Unit:      VW5pdA==
				[5] UnitT:     VW5pdFQ=
				[6] UnitTe:    VW5pdFRl
				[7] UnitTes:   VW5pdFRlcw==
				[8] UnitTest:  VW5pdFRlc3Q=
				[9] UnitTests: VW5pdFRlc3Rz
			 */
			for (int i = 0; i <= source.Length; i++)
			{
				string str = source.Substring(0, i);
				string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));

				// Test with valid Base64 input:
				Assert.AreEqual(b64, StringUtil.RepairBase64Padding(b64));

				string trimmed = b64.TrimEnd('=');
				if (i == 0 || i == 3 || i == 6 || i == 9)
				{
					// In these versions, there are no padding characters expected.
					Assert.AreEqual(b64, trimmed);
				}
				else
				{
					// In these versions, there are padding characters expected.
					Assert.AreNotEqual(b64, trimmed);
					Assert.AreEqual(b64, StringUtil.RepairBase64Padding(trimmed));
				}
			}
		}
		[TestMethod]
		public void TestDeNullify()
		{
			Assert.AreEqual("test", StringUtil.DeNullify("test"));
			Assert.AreEqual("", StringUtil.DeNullify(""));
			Assert.AreEqual("", StringUtil.DeNullify(null));
		}
		[TestMethod]
		public void TestOrdinalSuffix()
		{
			Assert.AreEqual("1st", StringUtil.ToOrdinal(1));
			Assert.AreEqual("2nd", StringUtil.ToOrdinal(2));
			Assert.AreEqual("3rd", StringUtil.ToOrdinal(3));
			Assert.AreEqual("4th", StringUtil.ToOrdinal(4));
			Assert.AreEqual("5th", StringUtil.ToOrdinal(5));
			Assert.AreEqual("6th", StringUtil.ToOrdinal(6));
			Assert.AreEqual("7th", StringUtil.ToOrdinal(7));
			Assert.AreEqual("8th", StringUtil.ToOrdinal(8));
			Assert.AreEqual("9th", StringUtil.ToOrdinal(9));
			Assert.AreEqual("10th", StringUtil.ToOrdinal(10));
			Assert.AreEqual("11th", StringUtil.ToOrdinal(11));
			Assert.AreEqual("12th", StringUtil.ToOrdinal(12));
			Assert.AreEqual("13th", StringUtil.ToOrdinal(13));
			Assert.AreEqual("14th", StringUtil.ToOrdinal(14));
			Assert.AreEqual("15th", StringUtil.ToOrdinal(15));
			Assert.AreEqual("16th", StringUtil.ToOrdinal(16));
			Assert.AreEqual("17th", StringUtil.ToOrdinal(17));
			Assert.AreEqual("18th", StringUtil.ToOrdinal(18));
			Assert.AreEqual("19th", StringUtil.ToOrdinal(19));
			Assert.AreEqual("20th", StringUtil.ToOrdinal(20));
			Assert.AreEqual("21st", StringUtil.ToOrdinal(21));
			Assert.AreEqual("22nd", StringUtil.ToOrdinal(22));
			Assert.AreEqual("23rd", StringUtil.ToOrdinal(23));
			Assert.AreEqual("24th", StringUtil.ToOrdinal(24));
			Assert.AreEqual("25th", StringUtil.ToOrdinal(25));
			Assert.AreEqual("26th", StringUtil.ToOrdinal(26));
			Assert.AreEqual("27th", StringUtil.ToOrdinal(27));
			Assert.AreEqual("28th", StringUtil.ToOrdinal(28));
			Assert.AreEqual("29th", StringUtil.ToOrdinal(29));
			Assert.AreEqual("30th", StringUtil.ToOrdinal(30));
			Assert.AreEqual("31st", StringUtil.ToOrdinal(31));
			Assert.AreEqual("32nd", StringUtil.ToOrdinal(32));
			Assert.AreEqual("33rd", StringUtil.ToOrdinal(33));
			Assert.AreEqual("34th", StringUtil.ToOrdinal(34));
			Assert.AreEqual("35th", StringUtil.ToOrdinal(35));
			Assert.AreEqual("36th", StringUtil.ToOrdinal(36));
			Assert.AreEqual("37th", StringUtil.ToOrdinal(37));
			Assert.AreEqual("38th", StringUtil.ToOrdinal(38));
			Assert.AreEqual("39th", StringUtil.ToOrdinal(39));
			// ...
			Assert.AreEqual("99th", StringUtil.ToOrdinal(99));
			Assert.AreEqual("100th", StringUtil.ToOrdinal(100));
			Assert.AreEqual("101st", StringUtil.ToOrdinal(101));
			Assert.AreEqual("102nd", StringUtil.ToOrdinal(102));
			Assert.AreEqual("103rd", StringUtil.ToOrdinal(103));
			Assert.AreEqual("104th", StringUtil.ToOrdinal(104));
			Assert.AreEqual("105th", StringUtil.ToOrdinal(105));
			Assert.AreEqual("106th", StringUtil.ToOrdinal(106));
			Assert.AreEqual("107th", StringUtil.ToOrdinal(107));
			Assert.AreEqual("108th", StringUtil.ToOrdinal(108));
			Assert.AreEqual("109th", StringUtil.ToOrdinal(109));
			Assert.AreEqual("110th", StringUtil.ToOrdinal(110));
			Assert.AreEqual("111th", StringUtil.ToOrdinal(111));
			Assert.AreEqual("112th", StringUtil.ToOrdinal(112));
			Assert.AreEqual("113th", StringUtil.ToOrdinal(113));
			Assert.AreEqual("114th", StringUtil.ToOrdinal(114));
			Assert.AreEqual("115th", StringUtil.ToOrdinal(115));
			Assert.AreEqual("116th", StringUtil.ToOrdinal(116));
			Assert.AreEqual("117th", StringUtil.ToOrdinal(117));
			Assert.AreEqual("118th", StringUtil.ToOrdinal(118));
			Assert.AreEqual("119th", StringUtil.ToOrdinal(119));
			Assert.AreEqual("120th", StringUtil.ToOrdinal(120));
			Assert.AreEqual("121st", StringUtil.ToOrdinal(121));
			Assert.AreEqual("122nd", StringUtil.ToOrdinal(122));
			Assert.AreEqual("123rd", StringUtil.ToOrdinal(123));
			Assert.AreEqual("124th", StringUtil.ToOrdinal(124));
			Assert.AreEqual("125th", StringUtil.ToOrdinal(125));
			Assert.AreEqual("126th", StringUtil.ToOrdinal(126));
			Assert.AreEqual("127th", StringUtil.ToOrdinal(127));
			Assert.AreEqual("128th", StringUtil.ToOrdinal(128));
			Assert.AreEqual("129th", StringUtil.ToOrdinal(129));
			Assert.AreEqual("130th", StringUtil.ToOrdinal(130));
			Assert.AreEqual("131st", StringUtil.ToOrdinal(131));
			Assert.AreEqual("132nd", StringUtil.ToOrdinal(132));
			Assert.AreEqual("133rd", StringUtil.ToOrdinal(133));
			Assert.AreEqual("134th", StringUtil.ToOrdinal(134));
			Assert.AreEqual("135th", StringUtil.ToOrdinal(135));
			Assert.AreEqual("136th", StringUtil.ToOrdinal(136));
			Assert.AreEqual("137th", StringUtil.ToOrdinal(137));
			Assert.AreEqual("138th", StringUtil.ToOrdinal(138));
			Assert.AreEqual("139th", StringUtil.ToOrdinal(139));
			// ...
			Assert.AreEqual("10099th", StringUtil.ToOrdinal(10099));
			Assert.AreEqual("100000th", StringUtil.ToOrdinal(100000));
			Assert.AreEqual("100001st", StringUtil.ToOrdinal(100001));
			Assert.AreEqual("100002nd", StringUtil.ToOrdinal(100002));
			Assert.AreEqual("100003rd", StringUtil.ToOrdinal(100003));
			Assert.AreEqual("100004th", StringUtil.ToOrdinal(100004));
			Assert.AreEqual("100005th", StringUtil.ToOrdinal(100005));
		}
		[TestMethod]
		public void TestReplaceFileExtension()
		{
			Assert.AreEqual("file.a", StringUtil.ReplaceFileExtension("file.b", ".a"), "basic test");
			Assert.AreEqual("folder/file.1", StringUtil.ReplaceFileExtension("folder/file.2", ".1"), "basic test with folder");
			Assert.AreEqual("folder\\file.t0", StringUtil.ReplaceFileExtension("folder\\file.s", ".t0"), "basic test with folder and different extension length");
			Assert.AreEqual("C:\\Folder\\File.cfg", StringUtil.ReplaceFileExtension("C:\\Folder\\File.txt", ".cfg"), "basic test with absolute path");
			Assert.AreEqual(".tfignore", StringUtil.ReplaceFileExtension(".gitignore", ".tfignore"), "extension-only file");
			Assert.AreEqual("FILE.dat", StringUtil.ReplaceFileExtension("FILE", ".dat"), "no-extension file");
		}
		[TestMethod]
		public void TestIndent()
		{
			Assert.AreEqual("\tA\r\tB", StringUtil.Indent("A\rB", "\t"));
			Assert.AreEqual("\tA\n\tB", StringUtil.Indent("A\nB", "\t"));
			Assert.AreEqual("\tA\r\n\tB", StringUtil.Indent("A\r\nB", "\t"));
			Assert.AreEqual("\tA\r\tB\r\n\tC", StringUtil.Indent("A\rB\r\nC", "\t"));
			Assert.AreEqual("\tA\r\n\tB\r\tC\n\tD\r\n\tE\r\tF\n\tG", StringUtil.Indent("A\r\nB\rC\nD\r\nE\rF\nG", "\t"));
		}
		[TestMethod]
		public void TestParseCommandLine()
		{
			string s1 = "\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\" --rtsp-tcp \"rtsp://127.0.0.1/\"";
			string s2 = "\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\" --rtsp-tcp \"rtsp://127.0.0.1/\" ";
			string s3 = "\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\" --rtsp-tcp \"rtsp://127.0.0.1/\"  ";
			string s4 = "  \"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\"  --rtsp-tcp \"rtsp://127.0.0.1/\"";

			string[] r1 = StringUtil.ParseCommandLine(s1);
			string[] r2 = StringUtil.ParseCommandLine(s2);
			string[] r3 = StringUtil.ParseCommandLine(s3);
			string[] r4 = StringUtil.ParseCommandLine(s4);

			Assert.AreEqual(3, r1.Length);
			Assert.AreEqual("\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\"", r1[0]);
			Assert.AreEqual("--rtsp-tcp", r1[1]);
			Assert.AreEqual("\"rtsp://127.0.0.1/\"", r1[2]);

			Assert.AreEqual(4, r2.Length);
			Assert.AreEqual("\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\"", r2[0]);
			Assert.AreEqual("--rtsp-tcp", r2[1]);
			Assert.AreEqual("\"rtsp://127.0.0.1/\"", r2[2]);
			Assert.AreEqual("", r2[3]);

			Assert.AreEqual(5, r3.Length);
			Assert.AreEqual("\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\"", r3[0]);
			Assert.AreEqual("--rtsp-tcp", r3[1]);
			Assert.AreEqual("\"rtsp://127.0.0.1/\"", r3[2]);
			Assert.AreEqual("", r3[3]);
			Assert.AreEqual("", r3[4]);

			Assert.AreEqual(6, r4.Length);
			Assert.AreEqual("", r4[0]);
			Assert.AreEqual("", r4[1]);
			Assert.AreEqual("\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\"", r4[2]);
			Assert.AreEqual("", r4[3]);
			Assert.AreEqual("--rtsp-tcp", r4[4]);
			Assert.AreEqual("\"rtsp://127.0.0.1/\"", r4[5]);

			Assert.AreEqual(s1, string.Join(" ", r1));
			Assert.AreEqual(s2, string.Join(" ", r2));
			Assert.AreEqual(s3, string.Join(" ", r3));
			Assert.AreEqual(s4, string.Join(" ", r4));
		}
		[TestMethod]
		public void TestDetectionOfTextEncodings()
		{
			_TestDetectionOfTextEncodings(new UTF32Encoding(true, true));
			_TestDetectionOfTextEncodings(new UTF32Encoding(false, true));
			_TestDetectionOfTextEncodings(new UTF32Encoding(true, false));
			_TestDetectionOfTextEncodings(new UTF32Encoding(false, false));
			_TestDetectionOfTextEncodings(new UnicodeEncoding(true, true));
			_TestDetectionOfTextEncodings(new UnicodeEncoding(false, true));
			_TestDetectionOfTextEncodings(new UnicodeEncoding(false, false));
			_TestDetectionOfTextEncodings(new UTF8Encoding(true));
			_TestDetectionOfTextEncodings(new UTF8Encoding(false));
			_TestDetectionOfTextEncodings(new UTF32Encoding(true, true), "Hello, World!");
			_TestDetectionOfTextEncodings(new UTF32Encoding(false, true), "Hello, World!");
			//_TestDetectionOfTextEncodings(new UTF32Encoding(true, false), "Hello, World!"); // Disabled because of false-positive UTF-16 detections in CSS files; UTF8 is now the default if there is no byte order mark.
			//_TestDetectionOfTextEncodings(new UTF32Encoding(false, false), "Hello, World!"); // Disabled because of false-positive UTF-16 detections in CSS files; UTF8 is now the default if there is no byte order mark.
			_TestDetectionOfTextEncodings(new UnicodeEncoding(true, true), "Hello, World!");
			_TestDetectionOfTextEncodings(new UnicodeEncoding(false, true), "Hello, World!");
			//_TestDetectionOfTextEncodings(new UnicodeEncoding(false, false), "Hello, World!"); // Disabled because of false-positive UTF-16 detections in CSS files; UTF8 is now the default if there is no byte order mark.
			_TestDetectionOfTextEncodings(new UTF8Encoding(true), "Hello, World!");
			_TestDetectionOfTextEncodings(new UTF8Encoding(false), "Hello, World!");
			Expect.Exception(() =>
			{
				_TestDetectionOfTextEncodings(Encoding.GetEncoding("windows-1252"), "Hello, World!");
			}, "Expected exception: \"Hello, World!\" is indistinguishable between Windows-1252 and UTF-8 encodings, so the detector should have chosen UTF-8.");
			// A character that encodes correctly in Windows-1252 but not in UTF-8 is the character "Û" (U+00DB). In Windows-1252 encoding, this character is represented by the byte 0xFB. However, in UTF-8, this byte sequence does not represent a valid character, leading to incorrect encoding.
			_TestDetectionOfTextEncodings(Encoding.GetEncoding("windows-1252"), "Hello, Û!");
		}
		private void _TestDetectionOfTextEncodings(Encoding encoding, string originalStr = "Hello, 世界! 👋")
		{
			string resultStr;
			Encoding result = StringUtil.DetectTextEncodingFromStream(MakeTestEncodingData(originalStr, encoding), out resultStr);
			Assert.IsTrue(originalStr.CompareTo(resultStr) == 0, "\"" + originalStr + "\" != \"" + resultStr + "\"");
			Assert.AreEqual(encoding.EncodingName, result.EncodingName);
		}
		private MemoryStream MakeTestEncodingData(string str, Encoding encoding)
		{
			byte[] preamble = encoding.GetPreamble();
			byte[] data = encoding.GetBytes(str);
			if (preamble == null || preamble.Length == 0)
				return new MemoryStream(data);
			MemoryStream ms = new MemoryStream(preamble.Length + data.Length);
			ms.Write(preamble, 0, preamble.Length);
			ms.Write(data, 0, data.Length);
			return ms;
		}
		[TestMethod]
		public void TestIsValidSystemdServiceName()
		{
			Assert.IsTrue(StringUtil.IsValidSystemdServiceName("BPUtil"));
			Assert.IsTrue(StringUtil.IsValidSystemdServiceName("BP:Util"));
			Assert.IsTrue(StringUtil.IsValidSystemdServiceName(":-_.\\"));
			Assert.IsFalse(StringUtil.IsValidSystemdServiceName("BP Util"));
			Assert.IsFalse(StringUtil.IsValidSystemdServiceName("BP;Util"));
		}
	}
}
