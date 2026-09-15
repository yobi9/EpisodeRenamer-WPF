using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EpisodeRenamer.App.UI;

[ComImport]
[Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    [PreserveSig]
    int Show(IntPtr hwndOwner);

    [PreserveSig]
    int SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);

    [PreserveSig]
    int SetFileTypeIndex(uint iFileType);

    [PreserveSig]
    int GetFileTypeIndex(out uint piFileType);

    [PreserveSig]
    int Advise(IntPtr pfde, out uint pdwCookie);

    [PreserveSig]
    int Unadvise(uint dwCookie);

    [PreserveSig]
    int SetOptions(uint fos);

    [PreserveSig]
    int GetOptions(out uint pfos);

    [PreserveSig]
    int SetDefaultFolder(IShellItem psi);

    [PreserveSig]
    int SetFolder(IShellItem psi);

    [PreserveSig]
    int GetFolder(out IShellItem ppsi);

    [PreserveSig]
    int GetCurrentSelection(out IShellItem ppsi);

    [PreserveSig]
    int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    [PreserveSig]
    int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    [PreserveSig]
    int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

    [PreserveSig]
    int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

    [PreserveSig]
    int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    [PreserveSig]
    int GetResult(out IShellItem ppsi);

    [PreserveSig]
    int AddPlace(IShellItem psi, int fdap);

    [PreserveSig]
    int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

    [PreserveSig]
    int Close(int hr);

    [PreserveSig]
    int SetClientGuid(ref Guid guid);

    [PreserveSig]
    int ClearClientData();

    [PreserveSig]
    int SetFilter(IntPtr pFilter);

    [PreserveSig]
    int GetResults(out IntPtr ppenum);

    [PreserveSig]
    int GetSelectedItems(out IntPtr ppsai);
}

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

    void GetParent(out IShellItem ppsi);

    void GetDisplayName(uint sigdnName, out IntPtr ppszName);

    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    void Compare(IShellItem psi, uint hint, out int piOrder);
}

[ComImport]
[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
[ClassInterface(ClassInterfaceType.None)]
internal class FileOpenDialogRCW { }

internal static class NativeFolderPicker
{
    private const uint FOS_OVERWRITEPROMPT = 0x2;
    private const uint FOS_NOCHANGEDIR = 0x8;
    private const uint FOS_PICKFOLDERS = 0x20;
    private const uint FOS_FORCEFILESYSTEM = 0x40;
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    internal static string? PickFolder(Window? owner, string? initialPath)
    {
        var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
        try
        {
            uint options = FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_NOCHANGEDIR;
            dialog.SetOptions(options);

            if (!string.IsNullOrWhiteSpace(initialPath))
            {
                try
                {
                    Guid shellItemGuid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                    IShellItem? initial = null;
                    int hr = SHCreateItemFromParsingName(initialPath, IntPtr.Zero, ref shellItemGuid, out initial);
                    if (hr == 0 && initial != null) dialog.SetFolder(initial);
                }
                catch { }
            }

            IntPtr ownerHandle = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
            int result = dialog.Show(ownerHandle);
            if (result != 0) return null;

            IShellItem? item;
            int getResult = dialog.GetResult(out item);
            if (getResult != 0 || item == null) return null;

            IntPtr namePtr;
            item.GetDisplayName(SIGDN_FILESYSPATH, out namePtr);
            if (namePtr == IntPtr.Zero) return null;

            try
            {
                return Marshal.PtrToStringUni(namePtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(namePtr);
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(dialog);
        }
    }
}