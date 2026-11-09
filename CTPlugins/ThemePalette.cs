using System.Drawing;

namespace CTPlugins
{
    /// <summary>The colors shared by the tray menu and every dialog, for one app theme.</summary>
    public sealed class ThemePalette
    {
        private const double AccentTextMix = 0.35;
        private const double AccentLuminanceForDarkText = 0.6;

        public static readonly ThemePalette Light = new ThemePalette
        {
            IsDark = false,
            Surface = Color.FromArgb(255, 255, 255),
            Footer = Color.FromArgb(243, 243, 243),
            Text = Color.FromArgb(26, 26, 26),
            MutedText = Color.FromArgb(96, 96, 96),
            DisabledText = Color.FromArgb(140, 140, 140),
            Border = Color.FromArgb(213, 213, 213),
            ControlFill = Color.FromArgb(251, 251, 251),
            ControlHover = Color.FromArgb(243, 243, 243),
            ControlPressed = Color.FromArgb(235, 235, 235),
            InputFill = Color.FromArgb(255, 255, 255),
            MenuBackground = Color.FromArgb(249, 249, 249),
            MenuHover = Color.FromArgb(236, 236, 236),
            MenuSection = Color.FromArgb(238, 238, 238),
            MenuSectionHover = Color.FromArgb(224, 224, 224),
            MenuSeparator = Color.FromArgb(229, 229, 229),
            MenuBorder = Color.FromArgb(214, 214, 214),
        };

        public static readonly ThemePalette Dark = new ThemePalette
        {
            IsDark = true,
            Surface = Color.FromArgb(32, 32, 32),
            Footer = Color.FromArgb(27, 27, 27),
            Text = Color.FromArgb(255, 255, 255),
            MutedText = Color.FromArgb(171, 171, 171),
            DisabledText = Color.FromArgb(128, 128, 128),
            Border = Color.FromArgb(70, 70, 70),
            ControlFill = Color.FromArgb(55, 55, 55),
            ControlHover = Color.FromArgb(65, 65, 65),
            ControlPressed = Color.FromArgb(45, 45, 45),
            InputFill = Color.FromArgb(45, 45, 45),
            MenuBackground = Color.FromArgb(44, 44, 44),
            MenuHover = Color.FromArgb(61, 61, 61),
            MenuSection = Color.FromArgb(58, 58, 58),
            MenuSectionHover = Color.FromArgb(74, 74, 74),
            MenuSeparator = Color.FromArgb(64, 64, 64),
            MenuBorder = Color.FromArgb(80, 80, 80),
        };

        /// <summary>Reads the system setting on every use, so a change applies without a restart.</summary>
        public static ThemePalette Current
        {
            get { return NativeTheme.AppsUseDarkTheme ? Dark : Light; }
        }

        public bool IsDark { get; private set; }
        public Color Surface { get; private set; }
        public Color Footer { get; private set; }
        public Color Text { get; private set; }
        public Color MutedText { get; private set; }
        public Color DisabledText { get; private set; }
        public Color Border { get; private set; }
        public Color ControlFill { get; private set; }
        public Color ControlHover { get; private set; }
        public Color ControlPressed { get; private set; }
        public Color InputFill { get; private set; }
        public Color MenuBackground { get; private set; }
        public Color MenuHover { get; private set; }
        public Color MenuSection { get; private set; }
        public Color MenuSectionHover { get; private set; }
        public Color MenuSeparator { get; private set; }
        public Color MenuBorder { get; private set; }

        /// <summary>The user's Windows accent color.</summary>
        public Color Accent
        {
            get { return NativeTheme.AccentColor; }
        }

        /// <summary>The accent shifted toward the text color, so it stays readable as text.</summary>
        public Color AccentText
        {
            get { return Mix(Accent, IsDark ? Color.White : Color.Black, AccentTextMix); }
        }

        /// <summary>Black or white, whichever reads better on a solid accent fill.</summary>
        public Color OnAccent
        {
            get { return Luminance(Accent) > AccentLuminanceForDarkText ? Color.Black : Color.White; }
        }

        /// <summary>Blends <paramref name="toward"/> into <paramref name="color"/> by 0–1.</summary>
        public static Color Mix(Color color, Color toward, double amount)
        {
            return Color.FromArgb(
                (int)(color.R + (toward.R - color.R) * amount),
                (int)(color.G + (toward.G - color.G) * amount),
                (int)(color.B + (toward.B - color.B) * amount));
        }

        private static double Luminance(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
        }
    }
}
