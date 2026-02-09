using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace BPUtil
{
	public class ImageFormatMetadata
	{
		public readonly string MimeType;
		public readonly string FileExtension;
		internal readonly Func<IDataStream, Size> SizeDecoder;
		internal readonly byte[][] MagicBytes;
		/// <summary>
		/// A method matching the signature of <see cref="DefaultImpl_IsMagicByteMatch"/>, called to determine if a data buffer contains data of this image format.
		/// </summary>
		internal Func<byte[], bool> IsMagicByteMatch;
		/// <summary>
		/// Returns true if the given bytes from the start of an image file match any of the registered magic byte sequences for this image format.  This method can be overridden to supply custom logic for matching magic bytes, such as for formats with more complex magic byte patterns.  By default, this method simply checks if the given bytes start with any of the byte sequences in the MagicBytes array.  The given byte array may be longer than the magic byte sequences; only the starting portion of the array will be checked for a match.  The image format is considered a match if any of the magic byte sequences match.  Note that the GetFormat methods will call this method with a byte array containing the first N bytes of the image file, where N is equal to the length of the longest magic byte sequence among all registered image formats.  Therefore, implementations of this method should only check the starting portion of the given byte array up to the length of their own magic byte sequences, and should not assume that the entire byte array is relevant for matching their format.
		/// </summary>
		/// <param name="bytes">Array of bytes from the start of an image file.</param>
		/// <returns>True if the bytes match this format; false otherwise.</returns>
		private bool DefaultImpl_IsMagicByteMatch(byte[] bytes)
		{
			foreach (byte[] magicBytes in MagicBytes)
			{
				if (ByteUtil.StartsWith(bytes, magicBytes))
					return true;
			}
			return false;
		}
		public ImageFormatMetadata(string mimeType, string fileExtension, Func<IDataStream, Size> sizeDecoder, params byte[][] magicBytes)
		{
			this.MimeType = mimeType;
			this.FileExtension = fileExtension;
			this.MagicBytes = magicBytes;
			this.SizeDecoder = sizeDecoder;
			IsMagicByteMatch = DefaultImpl_IsMagicByteMatch;
		}

		/// <summary>
		/// WebP is a RIFF container: starts with "RIFF" then [4 bytes (file size - 8)] then "WEBP".
		/// </summary>
		/// <param name="b">Byte array to test for WebP magic bytes.</param>
		/// <returns>True if the bytes indicate a WebP container.</returns>
		internal static bool WebP_IsMagicByteMatch(byte[] b)
		{
			// Need at least 12 bytes: "RIFF"(4) + size(4) + "WEBP"(4)
			if (b == null || b.Length < 12)
				return false;
			// Check "RIFF" at 0..3
			if (b[0] != (byte)'R' || b[1] != (byte)'I' || b[2] != (byte)'F' || b[3] != (byte)'F')
				return false;
			// Check "WEBP" at 8..11
			if (b[8] != (byte)'W' || b[9] != (byte)'E' || b[10] != (byte)'B' || b[11] != (byte)'P')
				return false;
			return true;
		}
	}
	public static class ImageUtil
	{
		private const string errorMessage = "Could not recognize image format.";
		private static readonly List<ImageFormatMetadata> imageFormatMetadataList = new List<ImageFormatMetadata>();
		public static readonly int MaxMagicBytesLength = 0;
		static ImageUtil()
		{
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/bmp", ".bmp", DecodeBitmapSize, new byte[] { 0x42, 0x4D }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/gif", ".gif", DecodeGifSize, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/png", ".png", DecodePngSize, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/jpeg", ".jpg", DecodeJpegSize, new byte[] { 0xff, 0xd8 }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/webp", ".webp", DecodeWebPSize, new byte[12]) { IsMagicByteMatch = ImageFormatMetadata.WebP_IsMagicByteMatch });

			foreach (ImageFormatMetadata m in imageFormatMetadataList)
			{
				foreach (byte[] magicBytes in m.MagicBytes)
				{
					if (MaxMagicBytesLength < magicBytes.Length)
						MaxMagicBytesLength = magicBytes.Length;
				}
			}
		}

		/// <summary>
		/// Returns the <see cref="ImageFormatMetadata"/> for the image at the given path. If the image format is not recognized, an ArgumentException will be thrown. Note that this method does not read the entire image file, and is therefore much faster than using <c>System.Drawing.Image.FromFile()</c> or similar methods to determine the image format.
		/// </summary>
		/// <param name="path">Image file path.</param>
		/// <returns>An <see cref="ImageFormatMetadata"/> corresponding to the image format.</returns>
		/// <exception cref="ArgumentException">If the image format is not recognized.</exception>
		public static ImageFormatMetadata GetFormat(string path)
		{
			int retries = 5;
			try
			{
				while (retries > 0)
				{
					try
					{
						using (FileStream fs = File.OpenRead(path))
						using (BasicDataStream dataStream = new BasicDataStream(fs))
						{
							return GetFormat(dataStream);
						}
					}
					catch (IOException ex)
					{
						retries--;
						if (retries <= 0)
							ex.Rethrow();
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error reading image format from file '" + path + "'", ex);
			}
			throw new Exception("Error reading image format from file '" + path + "'");
		}
		/// <summary>
		/// Reads the minimal number of magic bytes from the given <see cref="IDataStream"/> to determine the image format, and returns the corresponding <see cref="ImageFormatMetadata"/>. The <see cref="IDataStream"/> must be seeked to the start of the image file, and upon return will be seeked to the byte immediately following the magic bytes, the length of which depends on the image format. If the image format is not recognized, an ArgumentException will be thrown. Note that this method does not read the entire image file, and is therefore much faster than using <c>System.Drawing.Image.FromStream()</c> or similar methods to determine the image format.
		/// </summary>
		/// <param name="dataStream">Data stream that is seeked to the start of an image file.</param>
		/// <returns>An <see cref="ImageFormatMetadata"/> corresponding to the image format.</returns>
		/// <exception cref="ArgumentException">If the image format is not recognized.</exception>
		public static ImageFormatMetadata GetFormat(IDataStream dataStream)
		{
			byte[] magicBytes = new byte[MaxMagicBytesLength];
			for (int i = 0; i < MaxMagicBytesLength; i++)
			{
				magicBytes[i] = dataStream.ReadActualByte();
				ImageFormatMetadata m = GetFormatOrNull(magicBytes);
				if (m != null)
					return m;
			}
			throw new ArgumentException(errorMessage, "binaryReader");
		}
		/// <summary>
		/// Returns the <see cref="ImageFormatMetadata"/> for the given image data, or throws an Exception if the image format is not recognized.
		/// </summary>
		/// <param name="imageData">Byte array containing at least the first <see cref="MaxMagicBytesLength"/> bytes of the image file.</param>
		/// <returns>An <see cref="ImageFormatMetadata"/> corresponding to the image format.</returns>
		/// <exception cref="Exception">If the image format is not recognized.</exception>
		public static ImageFormatMetadata GetFormat(byte[] imageData)
		{
			ImageFormatMetadata m = GetFormatOrNull(imageData);
			if (m != null)
				return m;
			throw new Exception("Image format not recognized.");
		}
		/// <summary>
		/// Returns the ImageFormatMetadata for the given image data, or null if the image format is not recognized.
		/// </summary>
		/// <param name="imageData">Byte array containing at least the first <see cref="MaxMagicBytesLength"/> bytes of the image file.</param>
		/// <returns></returns>
		public static ImageFormatMetadata GetFormatOrNull(byte[] imageData)
		{
			foreach (ImageFormatMetadata m in imageFormatMetadataList)
			{
				if (m.IsMagicByteMatch(imageData))
					return m;
			}
			return null;
		}

		/// <summary>        
		/// Gets the dimensions of an image.        
		/// </summary>        
		/// <param name="data">Compressed image data from a file.</param>        
		/// <returns>The dimensions of the specified image.</returns>   
		/// <exception cref="ArgumentException">If unable to read the image dimensions from this buffer.</exception>
		public static Size GetDimensions(byte[] data)
		{
			using (MemoryDataStream ms = new MemoryDataStream(data))
			{
				return GetDimensions(ms);
			}
		}
		/// <summary>        
		/// Gets the dimensions of an image.        
		/// </summary>        
		/// <param name="path">The path of the image to get the dimensions of.</param>        
		/// <returns>The dimensions of the specified image.</returns>        
		public static Size GetDimensions(string path)
		{
			int retries = 5;
			try
			{
				while (retries > 0)
				{
					try
					{
						using (FileStream fs = File.OpenRead(path))
						using (BasicDataStream dataStream = new BasicDataStream(fs))
						{
							return GetDimensions(dataStream);
						}
					}
					catch (IOException ex)
					{
						retries--;
						if (retries <= 0)
							ex.Rethrow();
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error parsing image dimensions from file '" + path + "'", ex);
			}
			throw new Exception("Error parsing image dimensions from file '" + path + "'");
		}
		/// <summary>        
		/// Gets the dimensions of an image.        
		/// </summary>        
		/// <param name="dataStream">An <see cref="IDataStream"/> seeked to the start of the image file.</param>        
		/// <returns>The dimensions of the specified image.</returns>        
		/// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>            
		public static Size GetDimensions(IDataStream dataStream)
		{
			ImageFormatMetadata m = GetFormat(dataStream);
			return m.SizeDecoder(dataStream);
		}

		private static Size DecodeBitmapSize(IDataStream dataStream)
		{
			dataStream.Skip(16);
			int width = dataStream.ReadInt32LE(); // little-endian
			int height = dataStream.ReadInt32LE(); // little-endian
			return new Size(width, height);
		}

		private static Size DecodeGifSize(IDataStream dataStream)
		{
			int width = dataStream.ReadInt16LE(); // little-endian
			int height = dataStream.ReadInt16LE(); // little-endian
			return new Size(width, height);
		}

		private static Size DecodePngSize(IDataStream dataStream)
		{
			dataStream.Skip(8);
			int width = dataStream.ReadInt32(); // big-endian
			int height = dataStream.ReadInt32(); // big-endian
			return new Size(width, height);
		}

		private static Size DecodeJpegSize(IDataStream dataStream)
		{
			while (dataStream.Position < dataStream.Length)
			{
				byte markerStart = dataStream.ReadActualByte();
				if (markerStart != 0xFF)
					throw new InvalidDataException("Invalid JPEG marker.");

				byte markerType = dataStream.ReadActualByte();

				// Skip padding bytes (0xFF)
				while (markerType == 0xFF)
					markerType = dataStream.ReadActualByte();

				// SOF0, SOF2 markers (Start of Frame)
				if (markerType >= 0xC0 && markerType <= 0xC3)
				{
					ushort blockLength = dataStream.ReadUInt16(); // big-endian
					byte precision = dataStream.ReadActualByte(); // Usually 8
					ushort height = dataStream.ReadUInt16(); // big-endian
					ushort width = dataStream.ReadUInt16(); // big-endian
					return new Size(width, height);
				}
				else
				{
					ushort blockLength = dataStream.ReadUInt16(); // big-endian
					dataStream.Skip(blockLength - 2); // Skip the rest of this block
				}
			}
			throw new InvalidDataException("JPEG SOF marker not found.");
		}

		/// <summary>
		/// Decodes width/height for WebP images (supports VP8, VP8L, VP8X).
		/// DataStream is positioned immediately after the magic bytes ("RIFF") when called.
		/// </summary>
		private static Size DecodeWebPSize(IDataStream dataStream)
		{
			// At this point dataStream is positioned at byte 12.
			if (dataStream.Position != 12)
				throw new ArgumentException(nameof(dataStream) + " was at position " + dataStream.Position + ", expected 12.");

			// Iterate chunks until we find VP8 , VP8L or VP8X chunk with dimensions
			while (dataStream.Position < dataStream.Length)
			{
				// Read chunk header: FourCC (4 bytes) + size (uint32 LE)
				byte[] fourccBytes = dataStream.ReadNBytes(4);
				if (fourccBytes.Length < 4)
					break;
				string fourcc = Encoding.ASCII.GetString(fourccBytes);
				uint chunkSize = dataStream.ReadUInt32LE();

				// For safety, ensure chunkSize doesn't exceed remaining length
				long payloadStart = dataStream.Position;
				long payloadEnd = payloadStart + chunkSize;
				if (payloadEnd > dataStream.Length)
					payloadEnd = dataStream.Length;

				try
				{
					if (fourcc == "VP8 ")
					{
						// Lossy bitstream. Need at least 10 bytes of payload to read frame header.
						if (chunkSize < 10)
							throw new InvalidDataException("VP8 chunk too small.");

						// Read first 10 bytes of payload
						byte[] header = dataStream.ReadNBytes(10);
						// Verify key frame signature at bytes 3..5 == 0x9d 0x01 0x2a
						if (header[3] != 0x9d || header[4] != 0x01 || header[5] != 0x2a)
							throw new InvalidDataException("Invalid VP8 frame signature.");
						// width and height are little-endian 16-bit, but only lower 14 bits are used
						int width = (header[6] | (header[7] << 8)) & 0x3FFF;
						int height = (header[8] | (header[9] << 8)) & 0x3FFF;
						return new Size(width, height);
					}
					else if (fourcc == "VP8L")
					{
						// Lossless bitstream. Payload starts with a signature byte 0x2f, then 4 bytes little-endian.
						if (chunkSize < 5)
							throw new InvalidDataException("VP8L chunk too small.");
						byte signature = dataStream.ReadActualByte();
						if (signature != 0x2f)
							throw new InvalidDataException("Invalid VP8L signature.");
						uint packed = dataStream.ReadUInt32LE();
						int width = (int)((packed & 0x3FFF) + 1);
						int height = (int)(((packed >> 14) & 0x3FFF) + 1);
						return new Size(width, height);
					}
					else if (fourcc == "VP8X")
					{
						// Extended format. Payload contains flags(1) + 3 bytes reserved + 3 bytes width-1 + 3 bytes height-1
						if (chunkSize < 10)
							throw new InvalidDataException("VP8X chunk too small.");
						byte[] ext = dataStream.ReadNBytes(10);
						// width is 24-bit little-endian at offset 4..6, height at 7..9
						int width = (ext[4] | (ext[5] << 8) | (ext[6] << 16)) + 1;
						int height = (ext[7] | (ext[8] << 8) | (ext[9] << 16)) + 1;
						return new Size(width, height);
					}
				}
				finally
				{
					// Seek to the end of this chunk (account for padding byte: RIFF chunks are padded to even sizes)
					long toSkip = (payloadStart + chunkSize) - dataStream.Position;
					if (toSkip > 0)
						dataStream.Skip((int)toSkip);
					// If chunkSize is odd, there is a pad byte
					if ((chunkSize & 1) == 1)
					{
						// Might be at EOF; guard with try/catch
						try { dataStream.Skip(1); } catch { }
					}
				}
			}

			throw new ArgumentException("Could not read WEBP dimensions.");
		}
	}
}