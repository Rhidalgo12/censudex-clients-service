using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using censudex.src.models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace censudex.src.data
{
    public class DataSeeder
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {

            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<DataContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();


            var faker = new Faker("es");
            if (!await userManager.Users.AnyAsync())
            {
                for (int i = 0; i < 10; i++)
                {
                    var name = faker.Name.FirstName();
                    var lastName = faker.Name.LastName();
                    var email = $"{name.ToLower()}.{lastName.ToLower()}@censudex.cl";
                    if (await userManager.FindByEmailAsync(email) != null)
                        continue;

                    var user = new AppUser
                    {
                        UserName = faker.Internet.UserName(name, lastName),
                        Email = email,
                        FullName = $"{name} {lastName}",
                        DateOfBirth = DateOnly.FromDateTime(faker.Date.Past(30, DateTime.Now.AddYears(-18))),
                        Address = faker.Address.FullAddress(),
                        IsActive = true,
                        PhoneNumber = faker.Phone.PhoneNumber("+569########"),
                        CreatedAt = DateTime.UtcNow
                    };

                    string password = "Passw0rd2!";
                    var createUser = await userManager.CreateAsync(user, password);
                    if (!createUser.Succeeded)
                    {
                        foreach (var error in createUser.Errors)
                        {
                            Console.WriteLine($"Error al crear {email}: {error.Description}");
                        }
                        continue;
                    }
                    var roleResult = await userManager.AddToRoleAsync(user, "User");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"Usuario {user.Email} creado exitosamente");
                    }
                    else
                    {
                        Console.WriteLine($"Error al asignar rol a {user.Email}");
                    }
                }

                var adminEmail = "admin@censudex.cl";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new AppUser
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        FullName = "Administrador Censudex",
                        DateOfBirth = DateOnly.FromDateTime(faker.Date.Past(30, DateTime.Now.AddYears(-18))),
                        Address = faker.Address.FullAddress(),
                        IsActive = true,
                        PhoneNumber = faker.Phone.PhoneNumber("+569########"),
                        CreatedAt = DateTime.UtcNow
                    };

                    string adminPassword = "AdminPassw0rd!";
                    var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createAdmin.Succeeded)
                    {
                        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                        if (roleResult.Succeeded)
                        {
                            Console.WriteLine($"Usuario administrador {adminUser.Email} creado exitosamente");
                        }
                    }
                    else
                    {
                        foreach (var error in createAdmin.Errors)
                        {
                            Console.WriteLine($"Error al crear {adminEmail}: {error.Description}");
                        }
                    }
                }
            }
        }
    }
}