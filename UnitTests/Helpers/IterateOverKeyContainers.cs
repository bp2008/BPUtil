﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Helpers
{
	public static class IterateOverKeyContainers
	{
		static long CRYPT_MACHINE_KEYSET = 0x20;
		static long CRYPT_VERIFYCONTEXT = 0xF0000000;
		static uint CRYPT_FIRST = 1;
		static uint CRYPT_NEXT = 2;

		static uint PROV_RSA_FULL = 1;
		static uint PP_ENUMCONTAINERS = 2;

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool CryptGetProvParam(
		   IntPtr hProv,
		   uint dwParam,
		   [MarshalAs(UnmanagedType.LPStr)] StringBuilder pbData,
		   ref uint dwDataLen,
		   uint dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CryptAcquireContext(
			ref IntPtr hProv,
			string pszContainer,
			string pszProvider,
			uint dwProvType,
			uint dwFlags);

		[DllImport("advapi32.dll", EntryPoint = "CryptReleaseContext", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern bool CryptReleaseContext(
		   IntPtr hProv,
		   Int32 dwFlags);

		public static List<string> GetKeyContainerNames(bool useMachineKeyStore)
		{
			List<string> keyContainerNameList = new List<string>();

			IntPtr hProv = IntPtr.Zero;
			uint flags = (uint)(CRYPT_VERIFYCONTEXT);
			if (useMachineKeyStore)
				flags |= (uint)CRYPT_MACHINE_KEYSET;
			if (CryptAcquireContext(ref hProv, null, null, PROV_RSA_FULL, flags) == false)
				throw new Exception("CryptAcquireContext");

			uint bufferLength = 2048;
			StringBuilder stringBuilder = new StringBuilder((int)bufferLength);
			if (CryptGetProvParam(hProv, PP_ENUMCONTAINERS, stringBuilder, ref bufferLength, CRYPT_FIRST) == false)
				return keyContainerNameList;

			keyContainerNameList.Add(stringBuilder.ToString());

			while (CryptGetProvParam(hProv, PP_ENUMCONTAINERS, stringBuilder, ref bufferLength, CRYPT_NEXT))
			{
				keyContainerNameList.Add(stringBuilder.ToString());
			}

			if (hProv != IntPtr.Zero)
			{
				CryptReleaseContext(hProv, 0);
			}

			return keyContainerNameList;
		}
	}
}
