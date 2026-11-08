using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
namespace NeonRift {
// All normal launches share a mutex, even when started from different copies
// of the release. Diagnostic render/smoke processes never save or play audio.
public sealed class GameInstance : IDisposable {
    Mutex mutex;
    public bool IsPrimary {get;private set;}
    public GameInstance(bool diagnostic) {
        if(diagnostic){IsPrimary=true;return;}
        mutex=new Mutex(false,"Local\\NeonRift.Game.Session");
        try {IsPrimary=mutex.WaitOne(0,false);}catch(AbandonedMutexException){IsPrimary=true;}
    }
    delegate bool WindowCallback(IntPtr handle,IntPtr data);
    [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr handle,StringBuilder text,int count);
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr handle,int command);
    [DllImport("user32.dll")]static extern bool IsIconic(IntPtr handle);
    public void ActivateExisting() {
        EnumWindows(delegate(IntPtr h,IntPtr unused){
            StringBuilder title=new StringBuilder(256);GetWindowText(h,title,title.Capacity);
            if(!title.ToString().StartsWith("霓虹跃迁 · NEON RIFT",StringComparison.Ordinal))return true;
            if(IsIconic(h))ShowWindow(h,9);SetForegroundWindow(h);return false;
        },IntPtr.Zero);
    }
    public void Dispose(){if(mutex==null)return;if(IsPrimary)mutex.ReleaseMutex();mutex.Dispose();mutex=null;}
}
}
