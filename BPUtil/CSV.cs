using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace BPUtil
{
	/// <summary>
	/// Contains utility methods for CSV (comma-separated values) file formatting.
	/// </summary>
	public static class CSV
	{
		/// <summary>
		/// Encodes a string such that it can be written directly to a CSV file as a field.  Any invalid characters will be removed from the output string.  Valid characters are defined by RFC 4180 as the ASCII characters 10 (\n), 13 (\r), and 32-126. Notably, TAB is not considered a valid character.
		/// </summary>
		/// <param name="str">The string to encode as a CSV field</param>
		/// <returns></returns>
		public static string EncodeAsCsvField(string str)
		{
			return QuoteCsvField(StripInvalidCsvCharacters(str));
		}
		/// <summary>
		/// Encodes a string such that it can be written to a CSV file as a field. This method does not remove out-of-range characters.
		/// </summary>
		/// <param name="str">The string to encode as a CSV field</param>
		/// <returns></returns>
		public static string LooseEncodeAsCsvField(string str)
		{
			bool containsQuotationMark = str.Contains('"');
			if (containsQuotationMark)
				str = str.Replace("\"", "\"\"");
			if (containsQuotationMark || str.Contains('\r') || str.Contains('\n') || str.Contains(','))
				str = "\"" + str + "\"";
			return str;
		}
		/// <summary>
		/// Quote every CSV field whether it needs it or not, to help make it idiot-proof.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string QuoteCsvField(string str)
		{
			return "\"" + str.Replace("\"", "\"\"") + "\"";
		}

		/// <summary>
		/// Removes characters that are not allowed in a CSV file according to RFC 4180. Allowed characters are ASCII 10 (\n), 13 (\r), and 32-126. Notably, TAB is not considered a valid character.
		/// </summary>
		/// <param name="str">The string to process.</param>
		/// <returns>The string with invalid characters removed.</returns>
		public static string StripInvalidCsvCharacters(string str)
		{
			StringBuilder sb = new StringBuilder(str.Length);
			foreach (char c in str)
				if ((c > (char)31 && c < (char)127) || c == '\r' || c == '\n')
					sb.Append(c);
			return sb.ToString();
		}
		/// <summary>
		/// Throws an exception if the string contains any characters that are not allowed in a CSV file according to RFC 4180. Allowed characters are ASCII 10 (\n), 13 (\r), and 32-126. Notably, TAB is not considered a valid character.
		/// </summary>
		/// <param name="str">The string to validate.</param>
		/// <exception cref="ArgumentException">Thrown if the string contains any invalid characters.</exception>
		public static void ThrowIfStringIsInvalidCSVValue(string str)
		{
			if (str != null)
			{
				foreach (char c in str)
				{
					if ((c > (char)31 && c < (char)127) || c == '\r' || c == '\n') { }
					else
						throw new ArgumentException("Value contained (char)" + ((int)c) + " which is not allowed in CSV (comma-separated values) files.");
				}
			}
		}
		/// <summary>
		/// Returns a copy of the given string with any invalid characters replaced with spaces.
		/// </summary>
		/// <param name="str">The string to process.</param>
		/// <returns>The string with invalid characters replaced.</returns>
		public static string ReplaceInvalidCharactersWithSpaces(string str)
		{
			if (str == null)
				return "";
			StringBuilder sb = new StringBuilder();
			foreach (char c in str)
			{
				if ((c > (char)31 && c < (char)127) || c == '\r' || c == '\n')
					sb.Append(c);
				else
					sb.Append(' ');
			}
			return sb.ToString();
		}
		/// <summary>
		/// RFS 4180 declares that the CSV line ending is CRLF (\r\n).
		/// </summary>
		public const string lineEnding = "\r\n";
		/// <summary>
		/// Returns a string that contains this table in CSV (comma-separated values) format.
		/// </summary>
		/// <param name="table">This DataTable.</param>
		/// <param name="includeHeaders">If true, the first row of the file will contain the column names.</param>
		/// <param name="replaceOutOfRangeCharactersWithSpaces">If true, all out-of-range characters will be replaced with spaces.  If false, an exception will be thrown if an out-of-range character is encountered while creating the CSV string.</param>
		/// <returns>A string that contains this table in CSV (comma-separated values) format.</returns>
		/// <exception cref="ArgumentException">If any of the values in the table contain a character that is out of range or reserved in CSV files, and <paramref name="replaceOutOfRangeCharactersWithSpaces"/> is false.</exception>
		public static string ToCommaSeparatedValues(this DataTable table, bool includeHeaders = false, bool replaceOutOfRangeCharactersWithSpaces = false)
		{
			StringBuilder result = new StringBuilder();

			// Write column names
			if (includeHeaders)
			{
				for (int i = 0; i < table.Columns.Count; i++)
				{
					string str = table.Columns[i].ColumnName;
					if (replaceOutOfRangeCharactersWithSpaces)
						str = ReplaceInvalidCharactersWithSpaces(str);
					else
						ThrowIfStringIsInvalidCSVValue(str);

					result.Append(LooseEncodeAsCsvField(str));
					result.Append(i == table.Columns.Count - 1 ? lineEnding : ",");
				}
			}

			// Write rows
			foreach (DataRow row in table.Rows)
			{
				for (int i = 0; i < table.Columns.Count; i++)
				{
					string str = row[i]?.ToString();
					if (replaceOutOfRangeCharactersWithSpaces)
						str = ReplaceInvalidCharactersWithSpaces(str);
					else
						ThrowIfStringIsInvalidCSVValue(str);

					result.Append(LooseEncodeAsCsvField(str));
					result.Append(i == table.Columns.Count - 1 ? lineEnding : ",");
				}
			}

			return result.ToString();
		}
	}
	/// <summary>
	/// A class capable of reading and parsing a CSV file.
	/// </summary>
	public class CSVFile
	{
		/// <summary>
		/// The first row of the CSV file, if hasHeadings was true during construction, otherwise an empty string array.
		/// </summary>
		public string[] Headings;
		/// <summary>
		/// <para>An array of string arrays containing the data from the CSV file.</para>
		/// <example>
		/// <para>Here is an example of how to iterate through the data.</para>
		/// <code>
		/// <para>for (int i = 0; i &lt; Rows.Length; i++)</para>
		/// <para>{</para>
		///	<para>    string[] row = Rows[i];</para>
		///	<para>    for (int x = 0; x &lt; row.Length; x++)</para>
		///	<para>    {</para>
		///	<para>        string cell = row[x];</para>
		///	<para>    }</para>
		/// <para>}</para>
		/// </code>
		/// </example>
		/// </summary>
		public string[][] Rows;
		int columnCount = 0;
		public CSVFile(string csvString, bool interpretStringAsFilePath, bool hasHeadings)
		{
			if (interpretStringAsFilePath)
				LoadCSVString(File.ReadAllText(csvString), hasHeadings);
			else
				LoadCSVString(csvString, hasHeadings);
		}

		private void LoadCSVString(string csv, bool hasHeadings)
		{
			Headings = new string[0];
			Rows = new string[0][];
			if (string.IsNullOrEmpty(csv))
				return;

			List<string[]> rows = new List<string[]>();
			using (StringReader sr = new StringReader(csv))
			{
				string line;
				while ((line = ReadLogicalLine(sr)) != null)
				{
					string[] row = ParseCSVRow(line);
					if (row.Length == 0)
						throw new Exception("Empty CSV row");
					if (columnCount == 0)
					{
						columnCount = row.Length;
						if (hasHeadings)
						{
							Headings = row;
							continue;
						}
					}
					if (columnCount != row.Length)
						throw new Exception("Unequal number of columns in CSV rows");
					rows.Add(row);
				}
			}
			Rows = rows.ToArray();
		}

		private static string[] ParseCSVRow(string line)
		{
			List<string> cells = new List<string>();
			int currentParsingIndex = 0;
			while (currentParsingIndex < line.Length)
			{
				string cell = ParseCSVCell(line, ref currentParsingIndex);
				cells.Add(cell);
			}
			return cells.ToArray();
		}

		private static string ParseCSVCell(string line, ref int currentParsingIndex)
		{
			// Move past the comma, if there is one
			if (currentParsingIndex < line.Length && line[currentParsingIndex] == ',')
				currentParsingIndex++;
			StringBuilder sb = new StringBuilder();
			int start = currentParsingIndex;
			bool isQuotedValue = false;
			for (; currentParsingIndex < line.Length; currentParsingIndex++)
			{
				char c = line[currentParsingIndex];
				if (c == '"')
				{
					// Is this the start of the cell?
					if (currentParsingIndex == start)
					{
						isQuotedValue = true;
					}
					else
					{
						// Is this a quotation mark in the middle of the cell?
						if (currentParsingIndex + 1 < line.Length && line[currentParsingIndex + 1] == '"')
						{
							sb.Append('"');
							currentParsingIndex++;
						}
						else
						{
							if (isQuotedValue)
								break; // This is the end of the cell
							else
								throw new Exception("Invalid quotation mark found");
						}
					}
				}
				else if (c == ',')
				{
					if (isQuotedValue)
						sb.Append(',');
					else
						break; // This is the end of the cell
				}
				else
					sb.Append(c);
			}
			// Move up to the comma.
			if (isQuotedValue)
			{
				// The comma will be the next character
				currentParsingIndex++;
			}
			return sb.ToString();
		}

		/// <summary>
		/// <para>Reads CSV rows from the stream and allows them to be processed row by row without needing to load the entire stream into memory at once.</para>
		/// <para>When each row is read, it is sent to [rowCallback].</para>
		/// <para>If rowCallback returns true, the next row is read.</para>
		/// <para>If rowCallback returns false, the function returns early.</para>
		/// <para>Exceptions may be thrown if the CSV file is determined to be invalid.</para>
		/// </summary>
		/// <param name="tr">A <c>TextReader</c> (<c>StreamReader</c> or <c>StringReader</c>) positioned at the beginning of a line in a csv file.</param>
		/// <param name="rowCallback">A callback method which is called with the value of each row as it is read.  If this function returns false, streaming of the CSV file will end immediately.</param>
		public static void StreamingRead(TextReader tr, Func<string[], bool> rowCallback)
		{
			string line;
			int columnCount = 0;
			while ((line = ReadLogicalLine(tr)) != null)
			{
				string[] row = ParseCSVRow(line);
				if (columnCount == 0)
					columnCount = row.Length;
				if (row.Length == 0)
					throw new Exception("Empty CSV row");
				if (columnCount != row.Length)
					throw new Exception("Unequal number of columns in CSV rows");
				if (!rowCallback(row))
					break;
			}
		}

		/// <summary>
		/// <para>Reads CSV rows from the stream and allows them to be processed row by row without needing to load the entire stream into memory at once.</para>
		/// <para>When each row is read, it is returned via yield return.</para>
		/// <para>If rowCallback returns true, the next row is read.</para>
		/// <para>If rowCallback returns false, the function returns early.</para>
		/// <para>Exceptions may be thrown if the CSV file is determined to be invalid.</para>
		/// </summary>
		/// <param name="tr">A <c>TextReader</c> (<c>StreamReader</c> or <c>StringReader</c>) positioned at the beginning of a line in a csv file.</param>
		public static IEnumerable<string[]> StreamingRead(TextReader tr)
		{
			string line;
			int columnCount = 0;
			while ((line = ReadLogicalLine(tr)) != null)
			{
				string[] row = ParseCSVRow(line);
				if (columnCount == 0)
					columnCount = row.Length;
				if (row.Length == 0)
					throw new Exception("Empty CSV row");
				if (columnCount != row.Length)
					throw new Exception("Unequal number of columns in CSV rows");
				yield return row;
			}
		}

		private static int CountQuotes(string s)
		{
			if (string.IsNullOrEmpty(s))
				return 0;
			return s.Count(ch => ch == '"');
		}

		private static string ReadLogicalLine(TextReader tr)
		{
			string line = tr.ReadLine();
			if (line == null)
				return null;

			int quoteCount = CountQuotes(line);
			// Keep reading physical lines until quotes are balanced (even count).
			while ((quoteCount & 1) == 1)
			{
				string next = tr.ReadLine();
				if (next == null)
					break;
				line += Environment.NewLine + next;
				quoteCount += CountQuotes(next);
			}
			return line;
		}

		/// <summary>
		/// Produces a string representation of the first row (or headings) for debugging visualization purposes.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			string[] row;
			if (Headings.Length > 0)
			{
				row = Headings;
			}
			else if (Rows.Length > 0)
				row = Rows[0];
			else
				row = new string[0];
			for (int i = 0; i < row.Length; i++)
			{
				if (i > 0)
					sb.Append(',');
				sb.Append(CSV.EncodeAsCsvField(row[i]));
			}
			return sb.ToString();
		}
		/// <summary>
		/// Produces CSV output that could be written to a .csv file.
		/// </summary>
		/// <returns></returns>
		public string DumpToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < Headings.Length; i++)
			{
				if (i > 0)
					sb.Append(',');
				sb.Append(CSV.EncodeAsCsvField(Headings[i]));
			}
			if (Headings.Length > 0)
				sb.Append(Environment.NewLine);
			for (int n = 0; n < Rows.Length; n++)
			{
				if (n > 0)
					sb.Append(Environment.NewLine);
				string[] row = Rows[n];
				for (int i = 0; i < row.Length; i++)
				{
					if (i > 0)
						sb.Append(',');
					sb.Append(CSV.EncodeAsCsvField(row[i]));
				}
			}
			return sb.ToString();
		}
	}
}
