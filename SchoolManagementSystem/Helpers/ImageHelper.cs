namespace SchoolManagementSystem.Helpers
{
    public static class ImageHelper
    {
        public static string SaveBase64Image(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return "default.png";

            var bytes = Convert.FromBase64String(base64.Split(',')[1]);
            var fileName = Guid.NewGuid() + ".png";
            var path = Path.Combine("wwwroot/images", fileName);

            File.WriteAllBytes(path, bytes);
            return fileName;
        }
    }
}
