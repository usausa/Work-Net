using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

FontManager.RegisterFont(File.OpenRead("ipaexg.ttf"));

Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily("IPAexGothic").FontSize(20));

            page.Header()
                .Text("はろー PDF!")
                .SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(x =>
                {
                    x.Spacing(20);

                    x.Item().Text(Placeholders.LoremIpsum());
                    x.Item().Image(Placeholders.Image(200, 100));
                });

            page.Footer()
                .AlignCenter()
                .Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
        });
    })
    .GeneratePdf("hello.pdf");
