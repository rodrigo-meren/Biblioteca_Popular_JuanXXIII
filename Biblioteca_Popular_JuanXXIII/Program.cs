using System.IO;
using System.Text;
using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca_Popular_JuanXXIII
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            // Add services to the container.
            builder.Services.AddRazorPages();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
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

            // =========================================================
            // --- INICIALIZACIÓN DE ROLES, ADMIN Y IMPORTACIÓN CSV ---
            // =========================================================
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync(); // <--- ESTA ES LA LÍNEA MÁGICA

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                // 1. Crear los roles si no existen
                string[] roles = { "Administrador", "Bibliotecario", "Socio" };
                foreach (var rol in roles)
                {
                    if (!await roleManager.RoleExistsAsync(rol))
                    {
                        await roleManager.CreateAsync(new IdentityRole(rol));
                    }
                }

                // 2. Crear Administrador por defecto si no existe
                string adminDni = app.Configuration["AdminSetup:AdminDni"];
                string adminPassword = app.Configuration["AdminSetup:AdminPassword"];

                var adminUser = await userManager.FindByNameAsync(adminDni);

                if (adminUser == null)
                {
                    adminUser = new IdentityUser
                    {
                        UserName = adminDni,
                        Email = "admin@bibliotecajuanxxiii.com.ar",
                        EmailConfirmed = true
                    };

                   
                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Administrador");
                    }
                }
            }

                app.Run();
        }
    }
}

       
        
    