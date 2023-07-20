using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	///// <summary>
	///// Provides methods to get and set the volume of the default audio device.  This class is redundant now that we have AudioManager.cs.
	///// Based on https://eskerahn.dk/?p=2089, but that code had a bug.  The GUID needs to be either a ref parameter or given specific marshalling rules.
	///// Also based on https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/cb3de43e-3abb-4428-b58a-dc3e5f29db30/vista-master-volume-control-with-c?forum=windowspro-audiodevelopment
	///// Also based on https://gist.github.com/sverrirs/d099b34b7f72bb4fb386
	///// </summary>
	//public static class Audio
	//{
	//	/// <summary>
	//	/// A reference to the program's GUID.
	//	/// </summary>
	//	public static readonly Guid APPGUID = getGuid();
	//	private static Guid getGuid()
	//	{
	//		GuidAttribute attr = Assembly.GetEntryAssembly().GetCustomAttribute<GuidAttribute>();
	//		return attr == null ? Guid.Empty : new Guid(attr.Value);
	//	}
	//	private static IAudioEndpointVolume GetDefaultAudioDevice()
	//	{
	//		IMMDeviceEnumerator deviceEnumerator = null;
	//		IMMDevice speakers = null;
	//		try
	//		{
	//			deviceEnumerator = MMDeviceEnumeratorFactory.CreateInstance();
	//			int res = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
	//			if (res != 0)
	//				throw new Exception ("deviceEnumerator.GetDefaultAudioEndpoint returned " + new Win32Exception(res).Message);

	//			Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
	//			object aepv_obj;
	//			res = speakers.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out aepv_obj);
	//			if (res != 0)
	//				throw new Exception("speakers.Activate returned " + new Win32Exception(res).Message);

	//			IAudioEndpointVolume aepv = (IAudioEndpointVolume)aepv_obj;
	//			return aepv;
	//		}
	//		catch (Exception ex)
	//		{
	//			throw new Exception("Could not get default audio device. " + ex.Message);
	//		}
	//		finally
	//		{
	//			if (speakers != null) Marshal.ReleaseComObject(speakers);
	//			if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
	//		}
	//	}
	//	/// <summary>
	//	/// Sets the volume of the default audio device.
	//	/// </summary>
	//	/// <param name="level">Volume from 0 to 100.</param>
	//	public static void SetVolume(int level)
	//	{
	//		IAudioEndpointVolume audioDevice = GetDefaultAudioDevice();
	//		if (audioDevice == null)
	//			throw new Exception("audioDevice was null");
	//		int res = audioDevice.SetMasterVolumeLevelScalar(level.Clamp(0, 100) / 100f, APPGUID);
	//		if (res != 0)
	//			throw new Exception("SetMasterVolumeLevelScalar returned " + new Win32Exception(res).Message);
	//	}
	//	/// <summary>
	//	/// Gets the volume of the default audio device, from 0 to 100.
	//	/// </summary>
	//	/// <returns>Returns the volume from 0 to 100.</returns>
	//	public static int GetVolume()
	//	{
	//		float pfLevel = 0;
	//		int res = GetDefaultAudioDevice().GetMasterVolumeLevelScalar(ref pfLevel);
	//		if (res != 0)
	//			throw new Exception("GetMasterVolumeLevelScalar returned " + new Win32Exception(res).Message);
	//		return (int)Math.Round(pfLevel.Clamp(0, 1) * 100);
	//	}
	//	/// <summary>
	//	/// Sets the mute state of the default audio device.
	//	/// </summary>
	//	/// <param name="mute">True to mute, false to unmute.</param>
	//	public static void SetMute(bool mute)
	//	{
	//		IAudioEndpointVolume audioDevice = GetDefaultAudioDevice();
	//		if (audioDevice == null)
	//			throw new Exception("audioDevice was null");

	//		bool isMuted = mute;
	//		int res = audioDevice.SetMute(isMuted, Guid.Empty);
	//		if (res != 0)
	//			throw new Exception("SetMute returned " + new Win32Exception(res).Message);
	//	}
	//	/// <summary>
	//	/// Gets the mute state of the default audio device. Returns true if muted. False if unmuted.
	//	/// </summary>
	//	/// <returns>Returns true if muted. False if unmuted.</returns>
	//	public static bool GetMute()
	//	{
	//		int res = GetDefaultAudioDevice().GetMute(out bool isMuted);
	//		if (res != 0)
	//			throw new Exception("GetMute returned " + new Win32Exception(res).Message);
	//		return isMuted;
	//	}
	//	#region Native Stuff

	//	internal enum EDataFlow
	//	{
	//		eRender,
	//		eCapture,
	//		eAll,
	//		EDataFlow_enum_count
	//	}

	//	internal enum ERole
	//	{
	//		eConsole,
	//		eMultimedia,
	//		eCommunications,
	//		ERole_enum_count
	//	}
	//	[ComImport]
	//	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	//	internal class MMDeviceEnumerator
	//	{
	//	}
	//	[ComImport]
	//	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//	private interface IMMDeviceEnumerator
	//	{
	//		int _VtblGap1_1();
	//		[PreserveSig]
	//		int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
	//	}
	//	private static class MMDeviceEnumeratorFactory
	//	{
	//		public static IMMDeviceEnumerator CreateInstance()
	//		{
	//			return (IMMDeviceEnumerator)(new MMDeviceEnumerator());
	//			//return (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"))); // a MMDeviceEnumerator
	//		}
	//	}
	//	[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//	private interface IMMDevice
	//	{
	//		[PreserveSig]
	//		int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
	//	}

	//	[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//	public interface IAudioEndpointVolume
	//	{
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE RegisterControlChangeNotify(/* [in] */__in IAudioEndpointVolumeCallback *pNotify) = 0;
	//		int RegisterControlChangeNotify(IntPtr pNotify);
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE UnregisterControlChangeNotify(/* [in] */ __in IAudioEndpointVolumeCallback *pNotify) = 0;
	//		int UnregisterControlChangeNotify(IntPtr pNotify);
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetChannelCount(/* [out] */ __out UINT *pnChannelCount) = 0;
	//		int GetChannelCount(ref uint pnChannelCount);
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetMasterVolumeLevel( /* [in] */ __in float fLevelDB,/* [unique][in] */ LPCGUID pguidEventContext) = 0;
	//		/// <summary>
	//		/// Sets the volume level in decibels (correctness is highly questionable)
	//		/// </summary>
	//		/// <param name="fLevelDB">Volume level in decibels. Guessed minimum: -64, Guessed maximum: 0</param>
	//		/// <param name="pguidEventContext">Reference to the GUID of the app making the request.</param>
	//		/// <returns></returns>
	//		int SetMasterVolumeLevel(float fLevelDB, [In][MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetMasterVolumeLevelScalar( /* [in] */ __in float fLevel,/* [unique][in] */ LPCGUID pguidEventContext) = 0;
	//		/// <summary>
	//		/// Sets the volume level between 0.0 and 1.0.
	//		/// </summary>
	//		/// <param name="fLevel">A volume number between 0.0 and 1.0.</param>
	//		/// <param name="pguidEventContext">Reference to the GUID of the app making the request.</param>
	//		/// <returns></returns>
	//		int SetMasterVolumeLevelScalar(float fLevel, [In][MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetMasterVolumeLevel(/* [out] */ __out float *pfLevelDB) = 0;
	//		/// <summary>
	//		/// Gets the volume level in decibels (correctness is highly questionable)
	//		/// </summary>
	//		/// <param name="pfLevelDB"></param>
	//		/// <returns></returns>
	//		int GetMasterVolumeLevel(ref float pfLevelDB);
	//		//virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetMasterVolumeLevelScalar( /* [out] */ __out float *pfLevel) = 0;
	//		/// <summary>
	//		/// Gets the volume level between 0.0 and 1.0.
	//		/// </summary>
	//		/// <param name="pfLevel"></param>
	//		/// <returns></returns>
	//		int GetMasterVolumeLevelScalar(ref float pfLevel);

	//		/// <summary>
	//		/// Sets the volume level, in decibels, of the specified channel of the audio stream.
	//		/// </summary>
	//		/// <param name="channelNumber">The channel number.</param>
	//		/// <param name="level">The new volume level in decibels.</param>
	//		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int SetChannelVolumeLevel(
	//			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
	//			[In][MarshalAs(UnmanagedType.R4)] float level,
	//			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	//		/// <summary>
	//		/// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
	//		/// </summary>
	//		/// <param name="channelNumber">The channel number.</param>
	//		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
	//		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int SetChannelVolumeLevelScalar(
	//			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
	//			[In][MarshalAs(UnmanagedType.R4)] float level,
	//			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	//		/// <summary>
	//		/// Gets the volume level, in decibels, of the specified channel in the audio stream.
	//		/// </summary>
	//		/// <param name="channelNumber">The zero-based channel number.</param>
	//		/// <param name="level">The volume level in decibels.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int GetChannelVolumeLevel(
	//			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
	//			[Out][MarshalAs(UnmanagedType.R4)] out float level);

	//		/// <summary>
	//		/// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
	//		/// </summary>
	//		/// <param name="channelNumber">The zero-based channel number.</param>
	//		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int GetChannelVolumeLevelScalar(
	//			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
	//			[Out][MarshalAs(UnmanagedType.R4)] out float level);

	//		/// <summary>
	//		/// Sets the muting state of the audio stream.
	//		/// </summary>
	//		/// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
	//		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int SetMute(
	//			[In][MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
	//			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	//		/// <summary>
	//		/// Gets the muting state of the audio stream.
	//		/// </summary>
	//		/// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int GetMute(
	//			[Out][MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

	//		/// <summary>
	//		/// Gets information about the current step in the volume range.
	//		/// </summary>
	//		/// <param name="step">The current zero-based step index.</param>
	//		/// <param name="stepCount">The total number of steps in the volume range.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int GetVolumeStepInfo(
	//			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 step,
	//			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

	//		/// <summary>
	//		/// Increases the volume level by one step.
	//		/// </summary>
	//		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int VolumeStepUp(
	//			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	//		/// <summary>
	//		/// Decreases the volume level by one step.
	//		/// </summary>
	//		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int VolumeStepDown(
	//			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	//		/// <summary>
	//		/// Queries the audio endpoint device for its hardware-supported functions.
	//		/// </summary>
	//		/// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int QueryHardwareSupport(
	//			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

	//		/// <summary>
	//		/// Gets the volume range of the audio stream, in decibels.
	//		/// </summary>
	//		/// <param name="volumeMin">The minimum volume level in decibels.</param>
	//		/// <param name="volumeMax">The maximum volume level in decibels.</param>
	//		/// <param name="volumeStep">The volume increment level in decibels.</param>
	//		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	//		[PreserveSig]
	//		int GetVolumeRange(
	//			[Out][MarshalAs(UnmanagedType.R4)] out float volumeMin,
	//			[Out][MarshalAs(UnmanagedType.R4)] out float volumeMax,
	//			[Out][MarshalAs(UnmanagedType.R4)] out float volumeStep);
	//	}
	//	#endregion
	//}
}
