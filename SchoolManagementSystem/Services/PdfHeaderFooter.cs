using iTextSharp.text;
using iTextSharp.text.pdf;

namespace SchoolManagementSystem.Services
{
    public class PdfHeaderFooter : PdfPageEventHelper
    {
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfPTable footerTable = new PdfPTable(1)
            {
                TotalWidth = document.PageSize.Width - 80
            };

            PdfPCell cell = new PdfPCell(
                new Phrase($"Page {writer.PageNumber}",
                FontFactory.GetFont(FontFactory.HELVETICA, 9)))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            };

            footerTable.AddCell(cell);
            footerTable.WriteSelectedRows(
                0, -1,
                document.LeftMargin,
                document.BottomMargin - 10,
                writer.DirectContent
            );
        }
    }
}
