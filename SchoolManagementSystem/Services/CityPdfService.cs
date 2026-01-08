using iTextSharp.text;
using iTextSharp.text.pdf;
using SchoolManagementSystem.Models;
using System.IO;

namespace SchoolManagementSystem.Services
{
    public class CityPdfService
    {
        public byte[] GenerateCityPdf(List<City> cities)
        {
            using (var memoryStream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 80, 60);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                writer.PageEvent = new PdfHeaderFooter();

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("City List Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                });

                // Table (3 Columns)
                PdfPTable table = new PdfPTable(3)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 1f, 4f, 4f });

                // Table Headers
                AddHeaderCell(table, "Sr No");
                AddHeaderCell(table, "City Name");
                AddHeaderCell(table, "Country Name");

                // Table Body
                int serialNo = 1;
                foreach (var city in cities)
                {
                    table.AddCell(serialNo.ToString());
                    table.AddCell(city.Name);
                    table.AddCell(city.Country?.Name ?? "N/A");
                    serialNo++;
                }

                document.Add(table);
                document.Close();

                return memoryStream.ToArray();
            }
        }

        private void AddHeaderCell(PdfPTable table, string text)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            PdfPCell cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = new BaseColor(211, 211, 211),
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5
            };
            table.AddCell(cell);
        }
    }
}
