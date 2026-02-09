using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public class ImageFormatMetadata
	{
		public readonly string MimeType;
		public readonly string FileExtension;
		internal readonly Func<IDataStream, Size> SizeDecoder;
		internal readonly byte[][] MagicBytes;
		public ImageFormatMetadata(string mimeType, string fileExtension, Func<IDataStream, Size> sizeDecoder, params byte[][] magicBytes)
		{
			this.MimeType = mimeType;
			this.FileExtension = fileExtension;
			this.MagicBytes = magicBytes;
			this.SizeDecoder = sizeDecoder;
		}
	}
	public static class ImageUtil
	{
		private const string errorMessage = "Could not recognize image format.";
		private static readonly List<ImageFormatMetadata> imageFormatMetadataList = new List<ImageFormatMetadata>();
		public static readonly int MaxMagicBytesLength = 0;
		static ImageUtil()
		{
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/bmp", ".bmp", DecodeBitmap, new byte[] { 0x42, 0x4D }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/gif", ".gif", DecodeGif, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/png", ".png", DecodePng, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
			imageFormatMetadataList.Add(new ImageFormatMetadata("image/jpeg", ".jpg", DecodeJpeg, new byte[] { 0xff, 0xd8 }));

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
			for (int i = 0; i < MaxMagicBytesLength; i += 1)
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
				foreach (byte[] magicBytes in m.MagicBytes)
				{
					if (ByteUtil.StartsWith(imageData, magicBytes))
						return m;
				}
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

		private static Size DecodeBitmap(IDataStream dataStream)
		{
			dataStream.Skip(16);
			int width = dataStream.ReadInt32LE(); // little-endian
			int height = dataStream.ReadInt32LE(); // little-endian
			return new Size(width, height);
		}

		private static Size DecodeGif(IDataStream dataStream)
		{
			int width = dataStream.ReadInt16LE(); // little-endian
			int height = dataStream.ReadInt16LE(); // little-endian
			return new Size(width, height);
		}

		private static Size DecodePng(IDataStream dataStream)
		{
			dataStream.Skip(8);
			int width = dataStream.ReadInt32(); // big-endian
			int height = dataStream.ReadInt32(); // big-endian
			return new Size(width, height);
		}

		private static Size DecodeJpeg(IDataStream dataStream)
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
	}
}
