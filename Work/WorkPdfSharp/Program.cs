using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

GlobalFontSettings.FontResolver = new FontResolver(Path.GetFullPath("ipaexg.ttf"));

// Create a new PDF document.
var document = new PdfDocument();
document.Info.Title = "Created with PDFsharp";
document.Info.Subject = "Just a simple Hello-World program.";

// Create an empty page in this document.
var page = document.AddPage();
//page.Size = PageSize.Letter;

// Get an XGraphics object for drawing on this page.
var gfx = XGraphics.FromPdfPage(page);

// Draw two lines with a red default pen.
var width = page.Width.Point;
var height = page.Height.Point;
gfx.DrawLine(XPens.Red, 0, 0, width, height);
gfx.DrawLine(XPens.Red, width, 0, 0, height);

// Draw a circle with a red pen which is 1.5 point thick.
var r = width / 5;
gfx.DrawEllipse(new XPen(XColors.Red, 1.5), XBrushes.White, new XRect(width / 2 - r, height / 2 - r, 2 * r, 2 * r));

// Create a font.
var font = new XFont("IPAexGothic", 20, XFontStyleEx.BoldItalic);

// Draw the text.
gfx.DrawString("はろー PDFsharp!", font, XBrushes.Black,
    new XRect(0, 0, page.Width.Point, page.Height.Point), XStringFormats.Center);

// Save the document...
document.Save("test.pdf");

public class FontResolver : IFontResolver
{
    private readonly string path;

    public FontResolver(string path)
    {
        this.path = path;
    }

    public byte[] GetFont(string faceName)
    {
        using var fontStream = File.OpenRead(path);
        var fontData = new byte[fontStream.Length];
        _ = fontStream.Read(fontData, 0, fontData.Length);
        return fontData;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(familyName);
    }
}
