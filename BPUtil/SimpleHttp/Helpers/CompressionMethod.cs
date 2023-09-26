using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.Helpers
{
	/// <summary>
	/// Indicates a compression algorithm and preference weight.
	/// </summary>
	public class CompressionMethod
	{
		/// <summary>
		/// Compression Algorithm represented by this object.  Null if the algorithm is unsupported by SimpleHttp.
		/// </summary>
		public readonly CompressionAlgorithm? Algorithm;
		/// <summary>
		/// Gets the name of the Algorithm represented by this object suitable to be put into a "Content-Encoding" header.  It may be an unsupported algorithm.  See <see cref="Algorithm"/>.
		/// </summary>
		public readonly string AlgorithmName;
		/// <summary>
		/// Client-provided weight between 0 and 1 describing the order of priority of this value in a list.  Default if not provided is 1, which is the highest priority.
		/// </summary>
		public float Weight
		{
			get { return _weight; }
			set { Weight = value.Clamp(0, 1); }
		}
		private float _weight = 1.0f;

		/// <summary>
		/// Gets the <see cref="Weight"/> as a string in the correct format to be included in an HTTP "Accept-Encoding" request header (max 3 decimal places).
		/// </summary>
		public string WeightString => Weight.ToString("0.###");

		/// <summary>
		/// Constructs a CompressionMethod from a string pulled from a comma-separated list in an HTTP "Accept-Encoding" request header.
		/// </summary>
		/// <param name="str">A string pulled from a comma-separated list in an HTTP "Accept-Encoding" request header.</param>
		public CompressionMethod(string str)
		{
			str = str.Trim();
			int idxSemicolon = str.IndexOf(';');
			if (idxSemicolon > -1)
			{
				string qStr = str.Substring(idxSemicolon);
				Weight = NumberUtil.FirstFloat(qStr) ?? 1;
				str = str.Substring(0, idxSemicolon);
			}
			AlgorithmName = str;
			if (Enum.TryParse(AlgorithmName, true, out CompressionAlgorithm parsed))
				Algorithm = parsed;
		}
		/// <summary>
		/// Wraps the given stream with one that compresses using the selected algorithm.  You can WRITE TO the returned stream and it will be compressed as it is passed to the underlying stream.  The underlying stream will remain open when the compression stream is closed.
		/// </summary>
		/// <param name="baseStream">Base stream.</param>
		/// <returns>A stream that compresses using the selected algorithm.</returns>
		public Stream CreateCompressionStream(Stream baseStream)
		{
			if (Algorithm != null)
			{
#if NET6_0
				if (Algorithm == CompressionAlgorithm.br)
					return new BrotliStream(baseStream, CompressionLevel.Optimal, true);
#endif
				if (Algorithm == CompressionAlgorithm.gzip)
					return new GZipStream(baseStream, CompressionLevel.Optimal, true);
				else if (Algorithm == CompressionAlgorithm.deflate)
					return new DeflateStream(baseStream, CompressionLevel.Optimal, true);
			}
			throw new ApplicationException("CompressionMethod was configured with an unsupported algorithm: " + AlgorithmName);
		}
		/// <summary>
		/// Wraps the given stream with one that decompresses using the selected algorithm.  You can READ FROM the returned stream and it will decompress data from the underlying stream as it reads.  The underlying stream will remain open when the decompression stream is closed.
		/// </summary>
		/// <param name="baseStream">Base stream.</param>
		/// <returns>A stream that decompresses using the selected algorithm.</returns>
		public Stream CreateDecompressionStream(Stream baseStream)
		{
			if (Algorithm != null)
			{
#if NET6_0
				if (Algorithm == CompressionAlgorithm.br)
					return new BrotliStream(baseStream, CompressionMode.Decompress, true);
#endif
				if (Algorithm == CompressionAlgorithm.gzip)
					return new GZipStream(baseStream, CompressionMode.Decompress, true);
				else if (Algorithm == CompressionAlgorithm.deflate)
					return new DeflateStream(baseStream, CompressionMode.Decompress, true);
			}
			throw new ApplicationException("CompressionMethod was configured with an unsupported algorithm: " + AlgorithmName);
		}
		/// <summary>
		/// Compresses the given payload using the configured <see cref="Algorithm"/>.
		/// </summary>
		/// <param name="uncompressedBody">Uncompressed payload.</param>
		/// <returns>Compressed payload.</returns>
		public byte[] Compress(byte[] uncompressedBody)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (Stream comp = CreateCompressionStream(ms))
				{
					comp.Write(uncompressedBody, 0, uncompressedBody.Length);
				}
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Compresses the given payload using the configured <see cref="Algorithm"/>.
		/// </summary>
		/// <param name="uncompressedBody">Uncompressed payload.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Compressed payload.</returns>
		public async Task<byte[]> CompressAsync(byte[] uncompressedBody, CancellationToken cancellationToken)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (Stream comp = CreateCompressionStream(ms))
				{
					await comp.WriteAsync(uncompressedBody, 0, uncompressedBody.Length, cancellationToken).ConfigureAwait(false);
				}
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Decompresses the given payload using the configured <see cref="Algorithm"/>.
		/// </summary>
		/// <param name="compressedBody">Compressed payload.</param>
		/// <returns>Decompressed payload.</returns>
		public byte[] Decompress(byte[] compressedBody)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (MemoryStream inStream = new MemoryStream(compressedBody))
				{
					using (Stream comp = CreateDecompressionStream(inStream))
					{
						comp.CopyTo(outStream);
					}
				}
				return outStream.ToArray();
			}
		}
		/// <summary>
		/// Decompresses the given payload using the configured <see cref="Algorithm"/>.
		/// </summary>
		/// <param name="compressedBody">Compressed payload.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Decompressed payload.</returns>
		public async Task<byte[]> DecompressAsync(byte[] compressedBody, CancellationToken cancellationToken)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (MemoryStream inStream = new MemoryStream(compressedBody))
				{
					using (Stream comp = CreateDecompressionStream(inStream))
					{
						await comp.CopyToAsync(outStream, 81920, cancellationToken).ConfigureAwait(false);
					}
				}
				return outStream.ToArray();
			}
		}
		/// <summary>
		/// Gets the algorithm name and optional weight argument as a string in the correct format to be included in a comma-separated list in an HTTP "Accept-Encoding" request header. 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Algorithm + (Weight < 1 ? (";q=" + WeightString) : "");
		}
	}
	/// <summary>
	/// <para>Enumeration of HTTP compression algorithms supported by SimpleHttp.</para>
	/// <para>The algorithm names are the exact strings expected to be found in an HTTP "Accept-Encoding" header or "Content-Encoding" header.</para>
	/// <para>Values are ordered by the server's preference, most-preferred first.</para>
	/// </summary>
	public enum CompressionAlgorithm
	{
#if NET6_0
		/// <summary>
		/// Brotli.  Optimal compression level strongly recommended, as SmallestSize is far slower to compress.
		/// </summary>
		br,
#endif
		/// <summary>
		/// GZIP
		/// </summary>
		gzip,
		/// <summary>
		/// DEFLATE
		/// </summary>
		deflate
	}
}
