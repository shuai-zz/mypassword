using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Services;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>One read-only field row in the detail view.</summary>
    public sealed partial class DetailRow : ObservableObject
    {
        public const string KindText = "text";
        public const string KindPassword = "password";
        public const string KindMulti = "multi";
        public const string KindTotp = "totp";

        public string Label { get; }
        public string Kind { get; }

        public ObservableCollection<string> Values { get; } = new();

        [ObservableProperty]
        private string _value;

        [ObservableProperty]
        private bool _revealed;

        /// <summary>TOTP countdown pie geometry (only set for <see cref="KindTotp"/> rows).</summary>
        [ObservableProperty]
        private Geometry _totpPie;

        /// <summary>TOTP countdown pie fill — turns red in the final seconds.</summary>
        [ObservableProperty]
        private IBrush _totpPieBrush;

        /// <summary>True during the final seconds before the TOTP code rolls over.</summary>
        [ObservableProperty]
        private bool _totpWarning;

        private readonly string _plain;

        public DetailRow(string label, string kind, string value)
        {
            Label = label;
            Kind = kind;
            _plain = value ?? "";
            _value = kind == KindPassword ? Mask(_plain) : _plain;
        }

        public DetailRow(string label, System.Collections.Generic.IEnumerable<string> values)
        {
            Label = label;
            Kind = KindMulti;
            if (values != null)
            {
                foreach (string v in values)
                {
                    Values.Add(v);
                }
            }
            if (Values.Count == 0)
            {
                Values.Add("");
            }
        }

        public bool IsText => Kind == KindText;
        public bool IsPassword => Kind == KindPassword;
        public bool IsMulti => Kind == KindMulti;
        public bool IsTotp => Kind == KindTotp;
        public bool CanCopy => Kind is KindPassword or KindTotp || (Kind == KindText && !string.IsNullOrEmpty(_plain));

        private static string Mask(string s) => string.IsNullOrEmpty(s) ? "" : new string('•', 8);

        [RelayCommand]
        private void ToggleReveal()
        {
            if (!IsPassword)
            {
                return;
            }
            Revealed = !Revealed;
            Value = Revealed ? _plain : Mask(_plain);
        }

        [RelayCommand]
        private void Copy()
        {
            ClipboardService.Instance.Copy(IsPassword ? _plain : Value);
        }
    }
}
