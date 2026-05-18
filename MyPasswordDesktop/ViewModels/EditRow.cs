using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.ViewModels
{
    /// <summary>One value inside a multi-value edit row.</summary>
    public sealed partial class MultiEntry : ObservableObject
    {
        [ObservableProperty]
        private string _text;

        private readonly Action<MultiEntry> _remove;

        public MultiEntry(string text, Action<MultiEntry> remove)
        {
            _text = text;
            _remove = remove;
        }

        [RelayCommand]
        private void Remove() => _remove(this);
    }

    /// <summary>One editable field row in the edit view.</summary>
    public sealed partial class EditRow : ObservableObject
    {
        public const string KindLine = "line";
        public const string KindArea = "area";
        public const string KindPassword = "password";
        public const string KindMulti = "multi";
        public const string KindRemovable = "removable";

        public string Label { get; }
        public string Kind { get; }
        public string Tag { get; }

        [ObservableProperty]
        private string _value;

        [ObservableProperty]
        private bool _passwordRevealed;

        [ObservableProperty]
        private int _genLength = 16;

        [ObservableProperty]
        private bool _present = true;

        public ObservableCollection<MultiEntry> MultiValues { get; } = new();

        /// <summary>Invoked when a "removable" row's delete button is pressed.</summary>
        public Action OnRemoved;

        public EditRow(string tag, string label, string kind, string value)
        {
            Tag = tag;
            Label = label;
            Kind = kind;
            _value = value ?? "";
        }

        public bool IsLine => Kind == KindLine;
        public bool IsArea => Kind == KindArea;
        public bool IsPassword => Kind == KindPassword;
        public bool IsMulti => Kind == KindMulti;
        public bool IsRemovable => Kind == KindRemovable;

        public void SetMulti(System.Collections.Generic.IEnumerable<string> values)
        {
            MultiValues.Clear();
            if (values != null)
            {
                foreach (string v in values)
                {
                    MultiValues.Add(NewEntry(v));
                }
            }
            if (MultiValues.Count == 0)
            {
                MultiValues.Add(NewEntry(""));
            }
        }

        private MultiEntry NewEntry(string text) => new(text, e => MultiValues.Remove(e));

        public System.Collections.Generic.List<string> CollectMulti()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (MultiEntry e in MultiValues)
            {
                string t = (e.Text ?? "").Trim();
                if (t.Length > 0 && !list.Contains(t))
                {
                    list.Add(t);
                }
            }
            return list;
        }

        [RelayCommand]
        private void AddMulti() => MultiValues.Add(NewEntry(""));

        [RelayCommand]
        private void Generate()
        {
            int len = Math.Clamp(GenLength, 8, 32);
            Value = PasswordUtils.GeneratePassword(len, PasswordUtils.StyleAlphabetNumber);
            PasswordRevealed = true;
        }

        [RelayCommand]
        private void ToggleReveal() => PasswordRevealed = !PasswordRevealed;

        [RelayCommand]
        private void RemoveSelf()
        {
            Present = false;
            OnRemoved?.Invoke();
        }
    }
}
