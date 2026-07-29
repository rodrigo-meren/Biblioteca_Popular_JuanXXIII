using Biblioteca_Popular_JuanXXIII.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Biblioteca_Popular_JuanXXIII
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

          
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            // --------------------------------------

            builder.Services.AddDefaultIdentity<IdentityUser>(options => {
                options.SignIn.RequireConfirmedAccount = false;
              
                options.Password.RequireDigit = true; // Exige números (El DNI los tiene)
                options.Password.RequiredLength = 6;  // Mínimo 6 caracteres
                options.Password.RequireNonAlphanumeric = false; // No pide símbolos (!, @, #)
                options.Password.RequireUppercase = false; // No pide mayúsculas
                options.Password.RequireLowercase = false; // No pide minúsculas
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            // --- INICIALIZACIÓN DE ROLES Y ADMINISTRADOR ---
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                string[] roles = { "Administrador", "Bibliotecario", "Socio" };

                foreach (var rol in roles)
                {
                    if (!await roleManager.RoleExistsAsync(rol))
                    {
                        await roleManager.CreateAsync(new IdentityRole(rol));
                    }
                }

                // CORONAR A TU USUARIO COMO ADMINISTRADOR
                var adminUser = await userManager.FindByNameAsync("34888336");

                if (adminUser != null)
                {
                    if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
                    {
                        await userManager.AddToRoleAsync(adminUser, "Administrador");
                    }
                }
            }

            app.Run();
        }
    }
}