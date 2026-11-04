// HSH Gamepad | Telegram: @HSHGamepad
// منوی راست‌کلیک روی آیکون برنامه تو taskbar (Jump List) مثل تلگرام: «خروج از برنامه»
// کنار «Close window» که خودش ویندوز میذاره (اون فقط پنجره رو میبره تو tray).
// «خروج از برنامه» یه بار خود exe رو با --exit اجرا میکنه؛ اون نسخه به نسخه‌ی در حال اجرا خبر میده که خارج بشه (Program.cs).
// همه‌چیز تو try/catch ـه: اگه ویندوز (مثلاً ویندوز 7 بدون این قابلیت) نپذیرفت، برنامه عادی کار میکنه.
#nullable disable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace GamepadServer
{
    internal static class JumpListHelper
    {
        public const string ExitArg = "--exit";

        public static void Register()
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;

                var list = (ICustomDestinationList)new CDestinationList();
                uint maxSlots;
                Guid iidArray = typeof(IObjectArray).GUID;
                object removed;
                if (list.BeginList(out maxSlots, ref iidArray, out removed) != 0) return;

                var tasks = (IObjectCollection)new CEnumerableObjectCollection();

                var link = (IShellLinkW)new CShellLink();
                link.SetPath(exe);
                link.SetArguments(ExitArg);
                string icon = ExitIconFile();      // آیکون خروج؛ اگه ساخته نشد آیکون خود برنامه
                link.SetIconLocation(icon ?? exe, 0);
                link.SetDescription("خروج کامل از HSH Gamepad");

                // عنوان آیتم (بدون این، آیتم تو منو دیده نمیشه)
                var store = (IPropertyStore)link;
                var key = new PropertyKey { fmtid = new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), pid = 2 };    // PKEY_Title
                var pv = new PropVariant { vt = 31, ptr = Marshal.StringToCoTaskMemUni("خروج از برنامه") };       // VT_LPWSTR
                try
                {
                    store.SetValue(ref key, ref pv);
                    store.Commit();
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pv.ptr);
                }

                tasks.AddObject(link);
                list.AddUserTasks((IObjectArray)tasks);
                list.CommitList();
            }
            catch { }
        }

        /// <summary>تم ویندوز (taskbar / منوها) روشنه؟ (ویندوز 7 و 8 همیشه روشن حساب میشن)</summary>
        private static bool SystemIsLight()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object v = k == null ? null : k.GetValue("SystemUsesLightTheme");
                    if (v is int) return (int)v != 0;
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// منوی Jump List آیکون رو فقط از روی فایل میخونه، پس آیکون جاسازی‌شده تو exe رو
        /// یه بار تو %AppData%\GamepadServer مینویسیم. تم تیره => آیکون سفید، تم روشن => آیکون تیره.
        /// </summary>
        private static string ExitIconFile()
        {
            try
            {
                string res = SystemIsLight() ? "exit-dark.ico" : "exit-light.ico";
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamepadServer");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, res);

                var asm = Assembly.GetExecutingAssembly();
                foreach (string n in asm.GetManifestResourceNames())
                {
                    if (!n.EndsWith(res, StringComparison.OrdinalIgnoreCase)) continue;
                    using (Stream s = asm.GetManifestResourceStream(n))
                    {
                        if (s == null) break;
                        if (!File.Exists(file) || new FileInfo(file).Length != s.Length)
                        {
                            using (var f = File.Create(file)) s.CopyTo(f);
                        }
                    }
                    return file;
                }
            }
            catch { }
            return null;
        }

        // ---------- COM ----------
        [ComImport, Guid("77F10CF0-3DB5-4966-B520-B7C54FD35ED6"), ClassInterface(ClassInterfaceType.None)]
        private class CDestinationList { }

        [ComImport, Guid("2D3468C1-36A7-43B6-AC24-D3F02FD9607A"), ClassInterface(ClassInterfaceType.None)]
        private class CEnumerableObjectCollection { }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
        private class CShellLink { }

        [ComImport, Guid("6332DEBF-87B5-4670-90C0-5E57B408A49E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICustomDestinationList
        {
            void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
            [PreserveSig]
            int BeginList(out uint pcMaxSlots, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
            [PreserveSig]
            int AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory, IObjectArray poa);
            void AppendKnownCategory(int category);
            [PreserveSig]
            int AddUserTasks(IObjectArray poa);
            void CommitList();
            void GetRemovedDestinations(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
            void DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
            void AbortList();
        }

        [ComImport, Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IObjectArray
        {
            void GetCount(out uint pcObjects);
            void GetAt(uint uiIndex, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        }

        [ComImport, Guid("5632B1A4-E38A-400A-928A-D4CD63230295"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IObjectCollection
        {
            // IObjectArray
            void GetCount(out uint pcObjects);
            void GetAt(uint uiIndex, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
            // IObjectCollection
            void AddObject([MarshalAs(UnmanagedType.Interface)] object pvObject);
            void AddFromArray(IObjectArray poaSource);
            void RemoveObjectAt(uint uiIndex);
            void Clear();
        }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PropVariant pv);
            void SetValue(ref PropertyKey key, ref PropVariant pv);
            void Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr ptr;
            [FieldOffset(16)] private long pad;      // PROPVARIANT تو x64 ۲۴ بایته
        }
    }
}
