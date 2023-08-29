using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// HTTP-related extensions.
	/// </summary>
	public static class SimpleHttpExtensions
	{
		/// <summary>
		/// Returns the date and time formatted for insertion as the expiration date in a "Set-Cookie" header.
		/// </summary>
		/// <param name="time">This DateTime instance.</param>
		/// <returns></returns>
		public static string ToCookieTime(this DateTime time)
		{
			return time.ToString("dd MMM yyyy hh:mm:ss GMT");
		}
		/// <summary>
		/// Writes text, followed by the "\r\n" line terminator, regardless of which platform this program is run on.
		/// </summary>
		/// <param name="tw">This TextWriter.</param>
		/// <param name="line">Text content of the line.</param>
		public static void WriteLineRN(this TextWriter tw, string line)
		{
			tw.Write(line + "\r\n");
		}
		/// <summary>
		/// Writes text, followed by the "\r\n" line terminator, regardless of which platform this program is run on.
		/// </summary>
		/// <param name="tw">This TextWriter.</param>
		/// <param name="line">Text content of the line.</param>
		public static async Task WriteLineRNAsync(this TextWriter tw, string line)
		{
			await tw.WriteAsync(line + "\r\n").ConfigureAwait(false);
		}
		/// <summary>
		/// Writes text asynchronously with an optional cancellation token.
		/// </summary>
		/// <param name="tw">This TextWriter.</param>
		/// <param name="sb">StringBuilder containing text.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		public static async Task WriteAsync(this TextWriter tw, StringBuilder sb, CancellationToken cancellationToken = default)
		{
#if NET6_0
			await tw.WriteAsync(sb, cancellationToken).ConfigureAwait(false);
#else
			try
			{
				cancellationToken.Register(tw.Dispose);
				await tw.WriteAsync(sb.ToString()).ConfigureAwait(false);
			}
			catch (ObjectDisposedException)
			{
				cancellationToken.ThrowIfCancellationRequested();
				throw;
			}
#endif
		}
#if NET6_0
		/// <summary>
		/// Writes text, followed by the "\r\n" line terminator, regardless of which platform this program is run on.
		/// </summary>
		/// <param name="tw">This TextWriter.</param>
		/// <param name="line">Text content of the line.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		public static async Task WriteLineRNAsync(this TextWriter tw, string line, CancellationToken cancellationToken)
		{
			await tw.WriteAsync((line + "\r\n").AsMemory(), cancellationToken).ConfigureAwait(false);
		}
#endif
		/// <summary>
		/// Appends text, followed by the "\r\n" line terminator, regardless of which platform this program is run on.
		/// </summary>
		/// <param name="sb">This StringBuilder.</param>
		/// <param name="line">Text content of the line.</param>
		/// <returns></returns>
		public static StringBuilder AppendLineRN(this StringBuilder sb, string line)
		{
			return sb.Append(line + "\r\n");
		}
	}
}