using Microsoft.AspNetCore.Identity;

public static class DbInitializer
{
    public static async Task SeedRolesAsync(IServiceProvider service)
    {
        var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = { "Principal", "Teacher" , "Student" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
