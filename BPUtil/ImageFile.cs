#if NETFRAMEWORK || NET6_0_WIN

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Encoder = System.Drawing.Imaging.Encoder;

namespace BPUtil
{
	/// <summary>
	/// This class wraps a FileStream and an Image, serving as an alternative to the Image.FromFile() method.
	/// This works around a potential issue with Image.FromFile() where the file handles don't get closed properly even if the image is correctly disposed.
	/// </summary>
	public class ImageFile : IDisposable
	{
		/// <summary>
		/// A reference to the internal FileStream object.  The ImageFile class will dispose this object when the ImageFile is disposed.
		/// </summary>
		private Stream stream;
		/// <summary>
		/// A reference to the internal Image object.  The ImageFile class will dispose this object when the ImageFile is disposed.
		/// </summary>
		public Image img;
		/// <summary>
		/// Opens an image from a file, properly disposing of the image and file handle when this instance is disposed.
		/// </summary>
		/// <param name="path"></param>
		public ImageFile(string path)
		{
			stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			img = Image.FromStream(stream);
		}
		/// <summary>
		/// Opens an image from a byte array containing image file data, properly disposing of the image when this instance is disposed.
		/// </summary>
		/// <param name="rawImgFileData"></param>
		public ImageFile(byte[] rawImgFileData)
		{
			stream = new MemoryStream(rawImgFileData);
			img = Image.FromStream(stream);
		}
		/// <summary>
		/// Wraps an Image, properly disposing of the Image when this instance is disposed.
		/// </summary>
		/// <param name="image">The image to wrap.</param>
		public ImageFile(Image image)
		{
			img = image;
		}

		/// <summary>
		/// Gets the width, in pixels, of the Image.
		/// </summary>
		public int Width
		{
			get { return img.Width; }
		}
		/// <summary>
		/// Gets the height, in pixels, of the Image.
		/// </summary>
		public int Height
		{
			get { return img.Height; }
		}

#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects).
				}

				// free unmanaged resources (unmanaged objects) and override a finalizer below.
				// set large fields to null.
				try
				{
					if (img != null)
					{
						img.Dispose();
						img = null;
					}
				}
				catch { }
				try
				{
					if (stream != null)
					{
						stream.Dispose();
						stream = null;
					}
				}
				catch { }

				disposedValue = true;
			}
		}

		// override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~ImageFile()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// uncomment the following line if the finalizer is overridden above.
			GC.SuppressFinalize(this);
		}
#endregion
		/// <summary>
		/// <para>Gets a thumbnail of this image using the exact dimensions specified.</para>
		/// <para>You will be responsible for disposing the returned Bitmap.</para>
		/// <para>This method differs from the Image class's built-in GetThumbnailImage method in that it converts transparent pixels to white instead of black.</para>
		/// </summary>
		/// <param name="width">Width in pixels of the thumbnail.</param>
		/// <param name="height">Height in pixels of the thumbnail.</param>
		/// <returns>A thumbnail of the specified image.</returns>
		public Bitmap GetThumbnail(int width, int height)
		{
			return GetThumbnail(this.img, width, height);
		}
		/// <summary>
		/// <para>Gets a thumbnail of this image with the specified maximum dimensions.  The returned image will be as large as possible while not exceeding the specified dimensions and without enlarging the original image.</para>
		/// <para>You will be responsible for disposing the returned ImageFile.</para>
		/// <para>This method differs from the Image class's built-in GetThumbnailImage method in that it converts transparent pixels to white instead of black.</para>
		/// </summary>
		/// <param name="width">Maximum width in pixels of the thumbnail.</param>
		/// <param name="height">Maximum height in pixels of the thumbnail.</param>
		/// <returns>A thumbnail of the specified image.</returns>
		public ImageFile GetThumbnailSmart(int width, int height)
		{
			double srcAspect = (double)Width / Height;
			double tgtAspect = (double)width / height;
			if (tgtAspect > srcAspect)
			{
				// Target aspect is wider than source aspect. Honor the height.
				if (height > Height)
					height = Height;
				width = (int)Math.Round(height * srcAspect);
			}
			else
			{
				// Honor the width.
				if (width > Width)
					width = Width;
				height = (int)Math.Round(width / srcAspect);
			}
			return new ImageFile(GetThumbnail(this.img, width, height));
		}
		/// <summary>
		/// Gets a thumbnail of the specified image.  You will be responsible for disposing the returned Bitmap.
		/// This method differs from the Image class's built-in GetThumbnailImage method in that it converts transparent pixels to white instead of black.
		/// </summary>
		/// <param name="src">Source image to get a thumbnail of.</param>
		/// <param name="width">Width in pixels of the thumbnail.</param>
		/// <param name="height">Height in pixels of the thumbnail.</param>
		/// <returns>A thumbnail of the specified image.</returns>
		public static Bitmap GetThumbnail(Image src, int width, int height)
		{
			Bitmap b = new Bitmap(width, height);
			try
			{
				using (Graphics g = Graphics.FromImage(b))
				{
					g.Clear(Color.White);
					g.SmoothingMode = SmoothingMode.HighQuality;
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;
					g.DrawImage(src, 0, 0, width, height);
				}
			}
			catch (Exception)
			{
				b.Dispose();
				throw;
			}
			return b;
		}
		/// <summary>
		/// Saves the image to the file using the specified format. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of file write failure.
		/// </summary>
		/// <param name="path">File path</param>
		/// <param name="format">Format to save in</param>
		public void SaveToFile(string path, ImageFormat format)
		{
			img.SaveExt(path, format);
		}
		/// <summary>
		/// Saves the image to the stream using the specified format. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of stream write failure.
		/// </summary>
		/// <param name="stream">Stream to save to</param>
		/// <param name="format">Format to save in</param>
		public void SaveToStream(Stream stream, ImageFormat format)
		{
			img.SaveExt(stream, format);
		}
		/// <summary>
		/// Saves the image to a new byte array using the specified format. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of stream write failure.
		/// </summary>
		/// <param name="format">Format to save in</param>
		public byte[] SaveToBytes(ImageFormat format)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				SaveToStream(ms, format);
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Saves the image to the file using the specified JPEG quality. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of file write failure.
		/// </summary>
		/// <param name="path">File path</param>
		/// <param name="quality">JPEG quality [0-100]</param>
		public void SaveToFileJpeg(string path, long quality)
		{
			img.SaveExtJpeg(path, quality);
		}
		/// <summary>
		/// Saves the image to the stream using the specified JPEG quality. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of write failure.
		/// </summary>
		/// <param name="stream">Stream to save to.</param>
		/// <param name="quality">JPEG quality [0-100]</param>
		public void SaveToStreamJpeg(Stream stream, long quality)
		{
			img.SaveExtJpeg(stream, quality);
		}
		/// <summary>
		/// Saves the image to a new byte array using the specified JPEG quality. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of write failure.
		/// </summary>
		/// <param name="quality">JPEG quality [0-100]</param>
		public byte[] SaveToBytesJpeg(long quality)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				SaveToStreamJpeg(ms, quality);
				return ms.ToArray();
			}
		}
	}
	public static class ImageExtensions
	{
		/// <summary>
		/// If true, all output files written by SaveExt() calls will have "Full Control" permission allowed for the "Users" group.
		/// This may be helpful for allowing these files to be deleted by web applications running as their app pool identity.
		/// </summary>
		public static bool GiveFullControlToAllUsers = false;
		[ThreadStatic] static int retryCount = 0; // I believe this initializer does not execute per thread except on the first thread.  It will probably just be default int (0) no matter what we set it to here.
		const int maxRetries = 20;

		/// <summary>
		/// Resets the retry count to 0, allowing (more) image write retries for this thread.
		/// </summary>
		public static void InitImageWriteRetryLimit()
		{
			retryCount = 0;
		}
		/// <summary>
		/// Saves the image to the file using the specified format. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of file write failure.
		/// </summary>
		/// <param name="img">Image instance</param>
		/// <param name="path">File path</param>
		/// <param name="format">Format to save in</param>
		public static void SaveExt(this Image img, string path, ImageFormat format)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				img.SaveExt(ms, format);
				byte[] data = ms.ToArray();
				WriteToFile(path, data);
			}
		}
		/// <summary>
		/// Saves the image to the file using the specified format. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of file write failure.
		/// </summary>
		/// <param name="img">Image instance</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="format">Format to save in</param>
		public static void SaveExt(this Image img, Stream stream, ImageFormat format)
		{
			img.Save(stream, format);
		}
		/// <summary>
		/// Saves the image to the file using the specified JPEG quality. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of file write failure.
		/// </summary>
		/// <param name="img">Image instance</param>
		/// <param name="path">File path</param>
		/// <param name="quality">JPEG quality [0-100]</param>
		public static void SaveExtJpeg(this Image img, string path, long quality)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				img.SaveExtJpeg(ms, quality);
				byte[] data = ms.ToArray();
				WriteToFile(path, data);
			}
		}
		/// <summary>
		/// Saves the image to the file using the specified JPEG quality. Compared to using the native Image.Save() method, this method should provide better exception messages in the event of file write failure.
		/// </summary>
		/// <param name="img">Image instance</param>
		/// <param name="stream">Stream to write to</param>
		/// <param name="quality">JPEG quality [0-100]</param>
		public static void SaveExtJpeg(this Image img, Stream stream, long quality)
		{
			ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
			if (jpegCodec != null)
			{
				quality = quality.Clamp(0, 100);
				using (EncoderParameters encoderParams = new EncoderParameters(1))
				using (encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality)) // Quality must be a 64 bit integer or else the encoder will crash.  MS sucks sometimes.
					img.Save(stream, jpegCodec, encoderParams);
			}
			else
				img.SaveExt(stream, ImageFormat.Jpeg);
		}
		private static ImageCodecInfo GetEncoderInfo(string mimeType)
		{
			ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

			for (int i = 0; i < encoders.Length; i++)
				if (encoders[i].MimeType == mimeType)
					return encoders[i];
			return null;
		}
		/// <summary>
		/// Attempts to write the data to the path (overwriting existing files), with built-in retry logic.
		/// </summary>
		/// <param name="path">File path</param>
		/// <param name="data">Data to write</param>
		private static void WriteToFile(string path, byte[] data)
		{
			try
			{
				File.WriteAllBytes(path, data);
			}
			catch (Exception)
			{
				if (retryCount >= maxRetries)
					throw;

				while (retryCount < maxRetries)
				{
					retryCount++;
					Thread.Sleep(StaticRandom.Next(5, 21));
					try
					{
						File.WriteAllBytes(path, data);
						break;
					}
					catch (Exception)
					{
						if (retryCount >= maxRetries - 1)
							throw;
					}
				}
			}
			if (GiveFullControlToAllUsers)
				FileUtil.FullControlToUsers(path);
		}
	}
}
#endif
