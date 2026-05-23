using Avalonia.Media;

namespace MyPasswordDesktop.Controls
{
    /// <summary>
    /// Shared static <see cref="Geometry"/> instances for the app's icons. Path
    /// data is parsed once at startup and reused by every control (no per-render
    /// allocation, no PNG asset loading). Reference from XAML via <c>x:Static</c>
    /// or directly from C# code.
    /// </summary>
    public static class AppIcons
    {
        // Bootstrap Icons — bi-eye (16x16)
        public static readonly Geometry EyeOpen = Geometry.Parse(
            "M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8" +
            "M1.173 8a13 13 0 0 1 1.66-2.043C4.12 4.668 5.88 3.5 8 3.5s3.879 1.168 5.168 2.457A13 13 0 0 1 14.828 8" +
            "q-.086.13-.195.288c-.335.48-.83 1.12-1.465 1.755C11.879 11.332 10.119 12.5 8 12.5s-3.879-1.168-5.168-2.457A13 13 0 0 1 1.172 8z " +
            "M8 5.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5" +
            "M4.5 8a3.5 3.5 0 1 1 7 0 3.5 3.5 0 0 1-7 0");

        // Bootstrap Icons — bi-caret-up (16x16)
        public static readonly Geometry CaretUp = Geometry.Parse(
            "M3.204 11h9.592L8 5.519zm-.753-.659 4.796-5.48a1 1 0 0 1 1.506 0l4.796 5.48" +
            "c.566.647.106 1.659-.753 1.659H3.204a1 1 0 0 1-.753-1.659");

        // Bootstrap Icons — bi-caret-down (16x16)
        public static readonly Geometry CaretDown = Geometry.Parse(
            "M3.204 5h9.592L8 10.481zm-.753.659 4.796 5.48a1 1 0 0 0 1.506 0l4.796-5.48" +
            "c.566-.647.106-1.659-.753-1.659H3.204a1 1 0 0 0-.753 1.659");

        // Bootstrap Icons — bi-eye-slash (16x16)
        public static readonly Geometry EyeClosed = Geometry.Parse(
            "M13.359 11.238C15.06 9.72 16 8 16 8s-3-5.5-8-5.5a7 7 0 0 0-2.79.588l.77.771A6 6 0 0 1 8 3.5" +
            "c2.12 0 3.879 1.168 5.168 2.457A13 13 0 0 1 14.828 8q-.086.13-.195.288c-.335.48-.83 1.12-1.465 1.755q-.247.248-.517.486z " +
            "M11.297 9.176a3.5 3.5 0 0 0-4.474-4.474l.823.823a2.5 2.5 0 0 1 2.829 2.829" +
            "zm-2.943 1.299.822.822a3.5 3.5 0 0 1-4.474-4.474l.823.823a2.5 2.5 0 0 0 2.829 2.829 " +
            "M3.35 5.47q-.27.24-.518.487A13 13 0 0 0 1.172 8l.195.288c.335.48.83 1.12 1.465 1.755C4.121 11.332 5.881 12.5 8 12.5" +
            "c.716 0 1.39-.133 2.02-.36l.77.772A7 7 0 0 1 8 13.5C3 13.5 0 8 0 8s.939-1.721 2.641-3.238l.708.709" +
            "zm10.296 8.884-12-12 .708-.708 12 12z");
    }
}
