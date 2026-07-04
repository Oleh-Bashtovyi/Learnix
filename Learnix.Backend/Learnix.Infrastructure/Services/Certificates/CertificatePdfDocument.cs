using Learnix.Application.Certificates.Abstractions;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Learnix.Infrastructure.Services.Certificates;

internal sealed class CertificatePdfGenerator : ICertificatePdfGenerator
{
    private static byte[] GenerateQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(10);
    }

    private const string LogoSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2200 2100">
            <path d="M880 1928 c-25 -22 -70 -57 -100 -76 -30 -20 -57 -38 -59 -39 -7 -5 111 -123 122 -123 5 0 28 14 53 30 l44 31 0 -83 0 -83 85 -85 85 -85 2 110 c1 61 0 136 -1 168 -2 46 1 57 11 50 7 -4 45 -29 83 -54 111 -74 292 -156 389 -178 l28 -6 -1 -299 -2 -299 58 -59 c31 -32 62 -58 68 -58 6 0 41 30 78 68 l67 68 0 362 0 362 -24 0 c-13 0 -61 7 -107 15 -231 40 -425 127 -586 262 l-51 43 -99 0 -98 -1 -45 -41z" fill="#1e3a8a" />
            <path d="M565 1736 c-55 -24 -219 -65 -307 -76 l-88 -12 0 -640 0 -641 40 6 39 5 3 -87 3 -86 30 3 c62 7 183 34 253 58 147 49 272 117 414 226 l77 60 58 -49 c32 -26 96 -72 141 -102 l83 -54 52 51 51 52 -39 41 c-22 22 -67 59 -100 82 -86 59 -165 136 -165 161 0 14 -31 53 -85 106 l-85 85 0 -112 0 -112 -67 -54 c-87 -70 -167 -121 -251 -159 -69 -31 -168 -68 -184 -68 -10 0 -11 1077 0 1083 4 2 20 7 36 11 28 7 53 -17 564 -529 l536 -536 -127 -127 c-70 -70 -127 -130 -127 -133 0 -4 35 -10 78 -13 42 -4 185 -18 317 -31 132 -13 262 -25 289 -27 l48 -3 -2 260 c-1 143 -5 320 -9 393 l-6 133 -145 -140 -145 -141 -562 563 c-310 309 -568 563 -575 564 -7 1 -26 -3 -43 -11z" fill="#1e3a8a" />
        </svg>
        """;

    public byte[] Generate(CertificateDocumentData data)
    {
        var primaryColor = "#1e3a8a"; // Deep Blue
        var accentColor = "#d97706"; // Gold/Amber
        var textColor = "#1e293b"; // Slate-800
        var mutedColor = "#64748b"; // Slate-500

        byte[] qrCodeBytes = GenerateQrCode(data.VerificationUrl);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(x => x.FontFamily("Georgia").FontColor(textColor));

                // Draw borders in the background so they don't affect pagination/overflow
                page.Background()
                    .Padding(20)
                    .Border(4)
                    .BorderColor(primaryColor)
                    .Padding(2)
                    .Border(1)
                    .BorderColor(accentColor);

                page.Content()
                    .Padding(60)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Spacing(0);

                        // Brand header with Logo
                        col.Item().AlignCenter().Row(row =>
                        {
                            row.AutoItem().Width(40).Height(40).Svg(LogoSvg);
                            row.AutoItem().PaddingLeft(10).AlignMiddle().Text("LEARNIX")
                                .FontSize(36)
                                .Bold()
                                .FontColor(primaryColor)
                                .LetterSpacing(0.2f);
                        });

                        col.Item().Height(4);

                        col.Item()
                            .AlignCenter()
                            .Text("Certificate of Completion")
                            .FontSize(24)
                            .FontColor(accentColor)
                            .Italic();

                        col.Item().Height(30);

                        // Intro text
                        col.Item()
                            .AlignCenter()
                            .Text("This is to certify that")
                            .FontSize(14)
                            .FontColor(mutedColor)
                            .Italic();

                        col.Item().Height(10);

                        // Student name
                        col.Item()
                            .AlignCenter()
                            .Text(data.StudentFullName)
                            .FontSize(42)
                            .Bold()
                            .FontColor(textColor);

                        col.Item().Height(10);

                        col.Item()
                            .AlignCenter()
                            .Text("has successfully completed the course")
                            .FontSize(14)
                            .FontColor(mutedColor)
                            .Italic();

                        col.Item().Height(10);

                        // Course title
                        col.Item()
                            .AlignCenter()
                            .Text(data.CourseTitle)
                            .FontSize(28)
                            .SemiBold()
                            .FontColor(primaryColor);

                        // Spacer instead of ExtendVertical to prevent overflow bug in QuestPDF
                        col.Item().Height(60);

                        // Footer row
                        col.Item().Row(row =>
                        {
                            // Left: Instructor Signature
                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().PaddingBottom(2).Text(data.InstructorName).FontSize(24).Italic().FontColor(primaryColor);
                                c.Item().LineHorizontal(1).LineColor(mutedColor);
                                c.Item().PaddingTop(4).Text("Course Instructor").FontSize(11).FontColor(mutedColor);
                            });

                            // Center: Date & Code
                            row.RelativeItem().AlignMiddle().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Awarded on").FontSize(10).FontColor(mutedColor);
                                c.Item().AlignCenter()
                                    .Text(data.CompletedAt.ToString("MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture))
                                    .FontSize(14).SemiBold().FontColor(textColor);

                                c.Item().Height(10);

                                c.Item().AlignCenter().Text("Certificate ID").FontSize(10).FontColor(mutedColor);
                                c.Item().AlignCenter().Text(data.Code).FontSize(10).FontColor(textColor);
                            });

                            // Right: QR Code & Verify
                            row.RelativeItem().AlignMiddle().AlignRight().Column(c =>
                            {
                                c.Item().AlignCenter().Width(90).Height(90).Hyperlink(data.VerificationUrl).Image(qrCodeBytes);
                                c.Item().Height(4);
                                c.Item().AlignCenter().Hyperlink(data.VerificationUrl).Text("Verify Certificate")
                                    .FontSize(9)
                                    .FontColor(primaryColor)
                                    .Underline();
                            });
                        });
                    });
            });
        }).GeneratePdf();
    }
}
