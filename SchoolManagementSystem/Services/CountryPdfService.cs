using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.rtf.headerfooter;
using SchoolManagementSystem.Models;
using System.IO;

namespace SchoolManagementSystem.Services
{
    public class CountryPdfService
    {
        public byte[] GenerateCountryPdf(List<Country> countries)
        {
            using (var memoryStream = new MemoryStream())
            {
                // STEP 1: Create document
                Document document = new Document(PageSize.A4, 40, 40, 80, 60);

                // STEP 2: Create writer
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                writer.PageEvent = new PdfHeaderFooter();

                document.Open();

                // STEP 3: Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var title = new Paragraph("Country List Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // STEP 4: Create table (3 columns)
                PdfPTable table = new PdfPTable(2)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 1f, 5f });

                // STEP 5: Table Header
                AddHeaderCell(table, "Sr No");
                //AddHeaderCell(table, "Country ID");
                AddHeaderCell(table, "Country Name");

                // STEP 6: Table Body
                int serialNo = 1;
                foreach (var country in countries)
                {
                    table.AddCell(serialNo.ToString());
                    //table.AddCell(country.Id.ToString());
                    table.AddCell(country.Name);
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
