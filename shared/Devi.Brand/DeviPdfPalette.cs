// DEVI print palette and record header for every DEVI PDF.
//
// Linked into the core project (<Compile Include="..\..\shared\Devi.Brand\DeviPdfPalette.cs" />)
// so every DEVI record shares one header and one set of print colors.
// This file and shared/Devi.Theme/Themes/Tokens.xaml are the only places a DEVI tool may
// write a color value.
using PdfSharp.Drawing;

namespace Devi.Brand
{
    /// <summary>Print colors. The band matches the app canvas; the body is white paper with near-black ink.</summary>
    internal static class DeviPdfPalette
    {
        /// <summary>Header band, the app canvas #0A0A0B.</summary>
        public static XColor Band => XColor.FromArgb(0x0A, 0x0A, 0x0B);

        /// <summary>Tool name on the band.</summary>
        public static XColor BandText => XColor.FromArgb(0xFF, 0xFF, 0xFF);

        /// <summary>Version or running title on the band, the app secondary text #A1A1A8.</summary>
        public static XColor BandMuted => XColor.FromArgb(0xA1, 0xA1, 0xA8);

        public static XColor Paper => XColor.FromArgb(0xFF, 0xFF, 0xFF);

        public static XColor Ink => XColor.FromArgb(0x16, 0x16, 0x16);

        public static XColor Muted => XColor.FromArgb(0x5C, 0x5A, 0x55);

        public static XColor Soft => XColor.FromArgb(0xF6, 0xF4, 0xEF);

        public static XColor RuleLine => XColor.FromArgb(0xDC, 0xD8, 0xD0);

        public static XColor Accent => XColor.FromArgb(0x4B, 0x8D, 0xF8);

        public static XColor Danger => XColor.FromArgb(0x9E, 0x1C, 0x1C);

        public static XColor DangerSoft => XColor.FromArgb(0xF8, 0xE4, 0xE4);

        public static XColor Success => XColor.FromArgb(0x0E, 0x5C, 0x3A);

        public static XColor SuccessSoft => XColor.FromArgb(0xE5, 0xF6, 0xEC);

        public static XColor Info => XColor.FromArgb(0xEE, 0xF3, 0xF8);
    }

    /// <summary>
    /// The dark header band at the top of every page of a DEVI record: white DEVI wordmark,
    /// tool name under it, and the version (first page) or running title (later pages) on the right.
    /// </summary>
    internal static class DeviPdfBand
    {
        /// <summary>
        /// Draws the band and returns its height in points. Same lockup as the app title bar:
        /// white wordmark, a hairline divider, the tool name, and muted side text on the right.
        /// </summary>
        public static double Draw(XGraphics gfx, double pageWidth, double margin, XImage wordmark, bool continuation,
            string toolName, string sideText, string sansFamily)
        {
            var band = continuation ? 32d : 46d;
            var imageHeight = continuation ? 11d : 15d;
            var width = imageHeight * wordmark.PixelWidth / wordmark.PixelHeight;
            var nameFont = new XFont(sansFamily, continuation ? 9 : 11, XFontStyleEx.Regular);
            var sideFont = new XFont(sansFamily, 8.5, XFontStyleEx.Regular);
            gfx.DrawRectangle(new XSolidBrush(DeviPdfPalette.Band), 0, 0, pageWidth, band);
            var imageTop = (band - imageHeight) / 2;
            gfx.DrawImage(wordmark, margin, imageTop, width, imageHeight);
            var dividerX = margin + width + (continuation ? 9 : 12);
            gfx.DrawLine(new XPen(DeviPdfPalette.BandMuted, 0.6), dividerX, imageTop, dividerX, imageTop + imageHeight);
            var measured = gfx.MeasureString(toolName, nameFont);
            gfx.DrawString(toolName, nameFont, new XSolidBrush(DeviPdfPalette.BandText),
                new XRect(dividerX + (continuation ? 9 : 12), (band - measured.Height) / 2, measured.Width + 4, measured.Height + 1), XStringFormats.TopLeft);
            var sideSize = gfx.MeasureString(sideText, sideFont);
            gfx.DrawString(sideText, sideFont, new XSolidBrush(DeviPdfPalette.BandMuted),
                new XRect(pageWidth - margin - sideSize.Width, (band - sideSize.Height) / 2, sideSize.Width + 1, sideSize.Height),
                XStringFormats.TopLeft);
            return band;
        }
    }
}
