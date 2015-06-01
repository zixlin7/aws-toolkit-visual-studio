using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Threading;

using log4net;

namespace Amazon.AWSToolkit.EC2.ConnectionUtils
{
    public class RemoteDesktopUtil
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RemoteDesktopUtil));
        const int LENGTH_TILL_DELETE_CONFIG = 10 * 1000;

        public static void Connect(string computer, string username, string password, bool mapDrives)
        {
            string encryptedPassword = CreateRDPHashPassword(password);
            string configFile = writeRDPLaunchFile(computer, username, encryptedPassword, mapDrives);
            try
            {
                launchMstsc(configFile);
            }
            finally
            {
                ToolsUtil.SetupThreadToDeleteFile(configFile);
            }
        }


        static void launchMstsc(string configFile)
        {
            Process rdcProcess = new Process();

            string executable = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");
            if (executable != null)
            {
                rdcProcess.StartInfo.FileName = executable;
                rdcProcess.StartInfo.Arguments = string.Format("\"{0}\"", configFile);
                rdcProcess.Start();
            }
        }

        static string writeRDPLaunchFile(string computer, string username, string encryptedPassword, bool mapDrives)
        {
            string file = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.WriteLine("password 51:b:{0}", encryptedPassword);
                writer.WriteLine("username:s:{0}", username);
                writer.WriteLine("full address:s:{0}", computer);
                writer.WriteLine("redirectdrives:i:{0}", mapDrives ? "1" : "0");
            }

            return file;
        }


        #region Crypto API
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRYPTPROTECT_PROMPTSTRUCT
        {
            public int cbSize;
            public CryptProtectPromptFlags dwPromptFlags;
            public IntPtr hwndApp;
            public String szPrompt;
        }

        [Flags]
        private enum CryptProtectPromptFlags
        {
            // prompt on unprotect
            CRYPTPROTECT_PROMPT_ON_UNPROTECT = 0x1,

            // prompt on protect
            CRYPTPROTECT_PROMPT_ON_PROTECT = 0x2
        }

        [Flags]
        private enum CryptProtectFlags
        {
            // for remote-access situations where ui is not an option
            // if UI was specified on protect or unprotect operation, the call
            // will fail and GetLastError() will indicate ERROR_PASSWORD_RESTRICTION
            CRYPTPROTECT_UI_FORBIDDEN = 0x1,

            // per machine protected data -- any user on machine where CryptProtectData
            // took place may CryptUnprotectData
            CRYPTPROTECT_LOCAL_MACHINE = 0x4,

            // force credential synchronize during CryptProtectData()
            // Synchronize is only operation that occurs during this operation
            CRYPTPROTECT_CRED_SYNC = 0x8,

            // Generate an Audit on protect and unprotect operations
            CRYPTPROTECT_AUDIT = 0x10,

            // Protect data with a non-recoverable key
            CRYPTPROTECT_NO_RECOVERY = 0x20,


            // Verify the protection of a protected blob
            CRYPTPROTECT_VERIFY_PROTECTION = 0x40
        }

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(
            ref DATA_BLOB pDataIn,
            String szDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct,
            CryptProtectFlags dwFlags,
            ref DATA_BLOB pDataOut
        );

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn,
            String szDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct,
            CryptProtectFlags dwFlags,
            ref DATA_BLOB pDataOut
        );

        static string CreateRDPHashPassword(string password)
        {
            CryptProtectFlags flags = CryptProtectFlags.CRYPTPROTECT_UI_FORBIDDEN;
            DATA_BLOB blobPassword = ConvertData(Encoding.Unicode.GetBytes(password));
            DATA_BLOB encryptedBlob = new DATA_BLOB();
            DATA_BLOB dataOption = new DATA_BLOB();

            try
            {
                // RDP always sets description to psw 
                string pwDescription = "psw";

                CRYPTPROTECT_PROMPTSTRUCT prompt = new CRYPTPROTECT_PROMPTSTRUCT();
                if (!CryptProtectData(ref blobPassword, pwDescription, ref dataOption, IntPtr.Zero, ref prompt, flags, ref encryptedBlob))
                {
                    int errCode = Marshal.GetLastWin32Error();
                    throw new Exception("CryptProtectData failed.", new Win32Exception(errCode));
                }

                byte[] outData = new byte[encryptedBlob.cbData];
                Marshal.Copy(encryptedBlob.pbData, outData, 0, outData.Length);


                StringBuilder encrypted = new StringBuilder();
                for (int i = 0; i <= outData.Length - 1; i++)
                {
                    encrypted.Append(
                        Convert.ToString(outData[i], 16).PadLeft(2, '0').ToUpper());
                }

                string encryptedPassword = encrypted.ToString().ToUpper();
                return encryptedPassword;
            }
            finally
            {
                if (blobPassword.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(blobPassword.pbData);
                if (encryptedBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(encryptedBlob.pbData);
            }
        }

        static DATA_BLOB ConvertData(byte[] data)
        {
            DATA_BLOB blob = new DATA_BLOB();
            blob.pbData = Marshal.AllocHGlobal(data.Length);
            blob.cbData = data.Length;
            Marshal.Copy(data, 0, blob.pbData, data.Length);

            return blob;
        }

        #endregion
    }
}
