using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ServerC.Model;

using Microsoft.EntityFrameworkCore;
using ServerC.Hubs;

namespace ServerC
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Добавляем контекст базы данных
            services.AddDbContext<BookingContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Добавляем SignalR
            services.AddSignalR();

            // Регистрация всех зависимостей
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Использование HTTPS и редиректа на безопасные запросы
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Использование маршрутов
            app.UseRouting();

            // Использование SignalR
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WorkHub>("/registration");
                endpoints.MapHub<BookingHub>("/bookingHub"); // Добавлен новый хаб
                endpoints.MapControllers();
            });
        }
    }
}
