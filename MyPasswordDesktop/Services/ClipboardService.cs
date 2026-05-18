using System;
using System.Threading;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Services
{
    /// <summary>
    /// Copies values to the system clipboard and clears them after the
    /// configured timeout — the C# equivalent of the Java <c>ClipboardUtils</c>
    /// plus <c>ClearPasswordThread</c>.
    /// </summary>
    public sealed class ClipboardService
    {
        public static ClipboardService Instance { get; } = new();

        private IClipboard _clipboard;
        private string _lastCopied;
        private Timer _clearTimer;

        private ClipboardService() { }

        /// <summary>Wire the clipboard from the main window at startup.</summary>
        public void Attach(IClipboard clipboard) => _clipboard = clipboard;

        /// <summary>Copy <paramref name="text"/> and schedule it to be cleared.</summary>
        public void Copy(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            Dispatcher.UIThread.Post(async () =>
            {
                if (_clipboard == null)
                {
                    return;
                }
                try
                {
                    await _clipboard.SetTextAsync(text);
                    _lastCopied = text;
                    ScheduleClear(text);
                }
                catch (Exception e)
                {
                    Log.Warn("clipboard copy failed", e);
                }
            });
        }

        private void ScheduleClear(string copied)
        {
            int minutes = VaultManager.Current.GetSetting(SettingKey.CLEAR_CLIPBOARD, 1);
            if (minutes <= 0)
            {
                return;
            }
            _clearTimer?.Dispose();
            _clearTimer = new Timer(_ => ClearIfUnchanged(copied), null,
                TimeSpan.FromMinutes(minutes), Timeout.InfiniteTimeSpan);
        }

        private void ClearIfUnchanged(string copied)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                if (_clipboard == null)
                {
                    return;
                }
                try
                {
                    string current = await _clipboard.TryGetTextAsync();
                    if (current != null && current == copied)
                    {
                        await _clipboard.ClearAsync();
                        Log.Info("password cleared from clipboard.");
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("clipboard clear failed", e);
                }
            });
        }
    }
}
