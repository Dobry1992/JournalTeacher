using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Portal.Data;
using System.IO;

namespace Portal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Настройка non-commercial лицензии перед любым использованием ExcelPackage
            ExcelPackage.License.SetNonCommercialPersonal("DOBRY");
            // — или, если вы представляете некоммерческую организацию:
            // ExcelPackage.License.SetNonCommercialOrganization("<Название организации>");

            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AcademyContext>();
                }
                catch
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError("Ошибка чтение БД.");
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                    webBuilder.UseWebRoot("wwwroot");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
