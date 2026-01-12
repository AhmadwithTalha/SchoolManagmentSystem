using iTextSharp.text;
using iTextSharp.text.pdf;
using SchoolManagementSystem.Models;
using System.IO;

namespace SchoolManagementSystem.Services
{
    public class StudentPdfService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StudentPdfService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public byte[] GenerateStudentPdf(List<ApplicationUser> students)
        {
            using (var memoryStream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 40, 40, 80, 60);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                writer.PageEvent = new PdfHeaderFooter();

                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                document.Add(new Paragraph("Student List Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                });

                // Table with 9 columns
                PdfPTable table = new PdfPTable(9)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 1f, 3f, 3f, 4f, 3f, 3f, 3f, 4f, 3f });

                // Table headers
                AddHeaderCell(table, "Sr No");
                AddHeaderCell(table, "First Name");
                AddHeaderCell(table, "Last Name");
                AddHeaderCell(table, "Email");
                AddHeaderCell(table, "Phone Number");
                AddHeaderCell(table, "City");
                AddHeaderCell(table, "Country");
                AddHeaderCell(table, "Address");
                AddHeaderCell(table, "Profile Picture");

                int serialNo = 1;

                foreach (var student in students)
                {
                    table.AddCell(serialNo.ToString());
                    table.AddCell(student.FirstName);
                    table.AddCell(student.LastName);
                    table.AddCell(student.Email);
                    table.AddCell(student.PhoneNumberPublic);
                    table.AddCell(student.City?.Name ?? "N/A");
                    table.AddCell(student.Country?.Name ?? "N/A");
                    table.AddCell(student.Address ?? "");

                    // Profile image column
                    PdfPCell imageCell = new PdfPCell();
                    imageCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    imageCell.VerticalAlignment = Element.ALIGN_MIDDLE;


                    if (!string.IsNullOrEmpty(student.ProfileImage) &&
    student.ProfileImage != "default-user.png")
                    {
                        string imagePath = Path.Combine(
                            _webHostEnvironment.WebRootPath,
                            "images",
                            student.ProfileImage
                        );

                        if (File.Exists(imagePath))
                        {
                            try
                            {
                                using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                                {
                                    var profileImage = iTextSharp.text.Image.GetInstance(fs);
                                    profileImage.ScaleToFit(60f, 60f);
                                    profileImage.Alignment = Element.ALIGN_CENTER;
                                    imageCell.AddElement(profileImage);
                                }
                            }
                            catch
                            {
                                // silently ignore broken images
                            }
                        }
                    }


                    table.AddCell(imageCell); // ✅ Add imageCell, not cell

                    serialNo++;
                } // end foreach

                document.Add(table);
                document.Close();

                return memoryStream.ToArray(); // ✅ Always return
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
