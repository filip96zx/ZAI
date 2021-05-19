
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Przychodnia.Interfaces;
using Przychodnia.Models;
using Przychodnia.Npgsql;
using Przychodnia.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Przychodnia.Transfer.Email;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Przychodnia
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("Default",
                    builder => {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
            });
            services.Configure<MvcOptions>(options =>
            {
                options.EnableEndpointRouting = false;
            });

            services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DbConnection")));
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddIdentity<User, Role>(options =>
            {
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            })
                .AddEntityFrameworkStores<DatabaseContext>()
                .AddDefaultTokenProviders();
            //services.AddIdentity<User, Role>(options => {
            //    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            //    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            //})
            //    .AddUserStore<UserStore<User,Role,DatabaseContext>>()
            //    .AddDefaultTokenProviders();
            services.Configure<EmailSetting>(Configuration.GetSection("Email"));


            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       ValidIssuer = Configuration["Jwt:Issuer"],
                       ValidAudience = Configuration["Jwt:Issuer"],
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                           .GetBytes(Configuration["Jwt:Key"]))
                   };
               });
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddMvc();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("Default");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseRouting();
            app.UseAuthentication();
            app.UseHttpsRedirection();
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});
            app.UseMvc();
        }
    }
}
