using System;
using System.Runtime.InteropServices;
using System.Threading;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    public enum UnlockType
    {
        PASSWORD,
        OAUTH,
    }

    /// <summary>
    /// Singleton session holding the in-memory Data Encryption Key (raw 32-byte
    /// AES key). Auto-lock is driven by OS-level idle time queried via
    /// platform-specific APIs, polled on a background thread.
    /// </summary>
    public sealed class Session
    {
        private const long AutoLockPollInterval = 30_000L; // 30 seconds

        private static readonly Session Instance = new();

        private readonly object _gate = new();
        private Action _onAutoLocked;
        private UnlockType? _unlockType;
        private byte[] _dek;
        private long _lastActiveTime;

        private Session() { }

        public static Session Current => Instance;

        public void SetOnAutoLocked(Action onAutoLocked) => _onAutoLocked = onAutoLocked;

        /// <summary>Start the auto-lock polling thread. Call once at startup.</summary>
        public void StartAutoLockThread()
        {
            var thread = new Thread(AutoLockLoop) { IsBackground = true, Name = "auto-lock" };
            thread.Start();
        }

        private void AutoLockLoop()
        {
            for (; ; )
            {
                Thread.Sleep((int)AutoLockPollInterval);
                if (IsLocked())
                {
                    _onAutoLocked?.Invoke();
                }
            }
        }

        public void RecordActivity()
        {
            lock (_gate)
            {
                _lastActiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        public bool IsLocked()
        {
            lock (_gate)
            {
                if (_dek == null)
                {
                    return true;
                }
                long autoLockMinutes = VaultManager.Current.GetSetting(SettingKey.AUTO_LOCK, 10);
                if (autoLockMinutes <= 0)
                {
                    return false;
                }
                long autoLockMs = autoLockMinutes * 60_000L;
                long idleMs = GetSystemIdleTimeMillis();
                if (idleMs >= autoLockMs)
                {
                    Log.Info($"auto-lock triggered: idle {idleMs}ms >= {autoLockMs}ms");
                    LockInternal();
                    return true;
                }
                return false;
            }
        }

        public void Lock()
        {
            lock (_gate)
            {
                LockInternal();
            }
        }

        private void LockInternal()
        {
            _unlockType = null;
            _dek = null;
            _onAutoLocked?.Invoke();
            VaultManager.Current?.BackupDb();
        }

        public void SetKey(UnlockType unlockType, byte[] key)
        {
            lock (_gate)
            {
                _unlockType = unlockType;
                _dek = key;
            }
            if (key != null)
            {
                RecordActivity();
            }
        }

        public UnlockType? GetUnlockType() => _unlockType;

        /// <summary>The DEK, or <c>null</c> when the vault is locked.</summary>
        public byte[] GetKey()
        {
            lock (_gate)
            {
                if (IsLocked())
                {
                    return null;
                }
                RecordActivity();
                return _dek;
            }
        }

        // ── platform idle-time detection ────────────────────────────────────

        private long GetSystemIdleTimeMillis()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return GetIdleWindows();
                }
                if (OperatingSystem.IsMacOS())
                {
                    return GetIdleMac();
                }
                if (OperatingSystem.IsLinux())
                {
                    return GetIdleLinux();
                }
            }
            catch (Exception e)
            {
                Log.Warn("idle detection failed, falling back to app-level tracking", e);
            }
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastActiveTime;
        }

        // ── Windows ─────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo plii);

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        private long GetIdleWindows()
        {
            var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
            if (GetLastInputInfo(ref info))
            {
                return unchecked(GetTickCount() - info.dwTime);
            }
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastActiveTime;
        }

        // ── macOS ───────────────────────────────────────────────────────────

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern double CGEventSourceSecondsSinceLastEventType(int stateID, uint eventType);

        private long GetIdleMac()
        {
            // kCGEventSourceStateCombinedSessionState = 0, kCGAnyInputEventType = ~0
            double seconds = CGEventSourceSecondsSinceLastEventType(0, 0xFFFFFFFFu);
            return (long)(seconds * 1000);
        }

        // ── Linux / X11 XScreenSaver ────────────────────────────────────────

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr displayName);

        [DllImport("libX11.so.6")]
        private static extern ulong XRootWindow(IntPtr display, int screen);

        [DllImport("libX11.so.6")]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XFree(IntPtr data);

        [DllImport("libXss.so.1")]
        private static extern IntPtr XScreenSaverAllocInfo();

        [DllImport("libXss.so.1")]
        private static extern int XScreenSaverQueryInfo(IntPtr display, ulong drawable, IntPtr info);

        private long GetIdleLinux()
        {
            IntPtr display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastActiveTime;
            }
            IntPtr ssInfo = IntPtr.Zero;
            try
            {
                ulong rootWindow = XRootWindow(display, 0);
                ssInfo = XScreenSaverAllocInfo();
                if (ssInfo == IntPtr.Zero)
                {
                    return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastActiveTime;
                }
                int status = XScreenSaverQueryInfo(display, rootWindow, ssInfo);
                if (status != 0)
                {
                    // XScreenSaverInfo.idle (unsigned long) is at offset 24 on LP64.
                    return Marshal.ReadInt64(ssInfo, 24);
                }
            }
            finally
            {
                if (ssInfo != IntPtr.Zero) XFree(ssInfo);
                XCloseDisplay(display);
            }
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastActiveTime;
        }
    }
}
