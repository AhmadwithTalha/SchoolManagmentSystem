using SchoolManagementSystem.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Helpers
{
    public class RequiredIfCreatingAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var model = (StudentViewModel)validationContext.ObjectInstance;
            if (string.IsNullOrEmpty(model.Id) && string.IsNullOrEmpty(value?.ToString()))
            {
                return new ValidationResult("Password is required when creating a student");
            }
            return ValidationResult.Success;
        }
    }
}
