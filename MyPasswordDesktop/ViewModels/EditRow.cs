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

        public const int MinGenLength = 6;
        public const int MaxGenLength = 30;

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
        private int _genStyle = PasswordUtils.StyleAlphabetNumber;

        [ObservableProperty]
        private bool _isGeneratorOpen;

        [ObservableProperty]
        private bool _present = true;

        public bool IsStyleAlphaNum
        {
            get => GenStyle == PasswordUtils.StyleAlphabetNumber;
            set { if (value) GenStyle = PasswordUtils.StyleAlphabetNumber; }
        }
        public bool IsStyleAlpha
        {
            get => GenStyle == PasswordUtils.StyleAlphabet;
            set { if (value) GenStyle = PasswordUtils.StyleAlphabet; }
        }
        public bool IsStyleNum
        {
            get => GenStyle == PasswordUtils.StyleNumber;
            set { if (value) GenStyle = PasswordUtils.StyleNumber; }
        }
        public bool IsStyleAlphaNumSymbol
        {
            get => GenStyle == PasswordUtils.StyleAlphabetNumberSymbol;
            set { if (value) GenStyle = PasswordUtils.StyleAlphabetNumberSymbol; }
        }

        partial void OnGenStyleChanged(int value)
        {
            OnPropertyChanged(nameof(IsStyleAlphaNum));
            OnPropertyChanged(nameof(IsStyleAlpha));
            OnPropertyChanged(nameof(IsStyleNum));
            OnPropertyChanged(nameof(IsStyleAlphaNumSymbol));
        }

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
        private void ToggleGenerator() => IsGeneratorOpen = !IsGeneratorOpen;

        [RelayCommand]
        private void Generate()
        {
            int len = Math.Clamp(GenLength, MinGenLength, MaxGenLength);
            Value = PasswordUtils.GeneratePassword(len, GenStyle);
            PasswordRevealed = true;
        }

        [RelayCommand]
        private void RemoveSelf()
        {
            Present = false;
            OnRemoved?.Invoke();
        }
    }
}
