using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MyPasswordDesktop.Controls
{
    /// <summary>
    /// Attached property that drops a reveal/hide eye toggle <em>inside</em> a
    /// password <see cref="TextBox"/> (via <see cref="TextBox.InnerRightContent"/>),
    /// so any password field becomes a reveal box without a custom control:
    /// <code>&lt;TextBox PasswordChar="●" controls:PasswordReveal.Enabled="True"/&gt;</code>
    /// The eye icon follows action semantics — it shows what a click will do.
    /// </summary>
    public static class PasswordReveal
    {
        public static readonly AttachedProperty<bool> EnabledProperty =
            AvaloniaProperty.RegisterAttached<TextBox, bool>("Enabled", typeof(PasswordReveal));

        public static void SetEnabled(TextBox element, bool value) => element.SetValue(EnabledProperty, value);

        public static bool GetEnabled(TextBox element) => element.GetValue(EnabledProperty);

        static PasswordReveal()
        {
            EnabledProperty.Changed.AddClassHandler<TextBox>(OnEnabledChanged);
        }

        private static void OnEnabledChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is not true)
            {
                textBox.InnerRightContent = null;
                InputMethod.SetIsInputMethodEnabled(textBox, true);
                return;
            }

            // Password fields must not engage the OS IME — a CJK candidate
            // window would surface preedit characters that bypass PasswordChar
            // masking and could leak into the entered value.
            InputMethod.SetIsInputMethodEnabled(textBox, false);

            var icon = new PathIcon { Width = 16, Height = 16 };
            var button = new Button
            {
                Content = icon,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 0),
                MinWidth = 0,
                MinHeight = 0,
                IsTabStop = false,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            button.Classes.Add("transparent");
            button.Click += (_, _) => textBox.RevealPassword = !textBox.RevealPassword;

            // keep the icon in sync whoever flips RevealPassword (the button, or a
            // two-way binding such as ItemEditView's "reveal after Generate").
            UpdateIcon(icon, textBox.RevealPassword);
            textBox.PropertyChanged += (_, args) =>
            {
                if (args.Property == TextBox.RevealPasswordProperty)
                {
                    UpdateIcon(icon, textBox.RevealPassword);
                }
            };

            textBox.InnerRightContent = button;
        }

        private static void UpdateIcon(PathIcon icon, bool revealed)
            => icon.Data = revealed ? AppIcons.EyeClosed : AppIcons.EyeOpen;
    }
}
