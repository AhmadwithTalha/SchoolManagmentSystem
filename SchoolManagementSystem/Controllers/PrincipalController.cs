using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Helpers;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers
{
    [Authorize(Roles = "Principal")]
    public class PrincipalController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PrincipalController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            user!.FirstName = EncryptionHelper.Decrypt(user.FirstName!);
            user.LastName = EncryptionHelper.Decrypt(user.LastName!);
            user.CountryId = (user.CountryId!);
            user.CityId = (user.CityId!);
            user.Address = EncryptionHelper.Decrypt(user.Address!);
            user.PhoneNumber = EncryptionHelper.Decrypt(user.PhoneNumber!);

            return View(user);
        }
    }
}
