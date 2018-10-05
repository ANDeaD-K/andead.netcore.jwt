using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace andead.netcore.jwt
{
    public class AuthOptions
    {
        public const string ISSUER = "MyAuthServer"; // издатель токена
        public const string AUDIENCE = "http://localhost:51884/";
        const string KEY = "mysupersecret_secretkey!123";
        public const int LIFETIME = 10; // время жизни токена - 1 минута
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }

    public interface IPeople
    {
        List<Person> GetPeople();

        void SetUserRefreshToken(int userId, string refreshToken);
    }

    public class People : IPeople
    {
        private List<Person> people = new List<Person>();
        public People()
        {
            //List<Person> people = new List<Person>();
            
            people.AddRange(new Person[]
                {
                    new Person() {
                        UserName = "first",
                        Password = "123qweQWE",
                        Role = "User"
                    },
                    new Person() {
                        UserName = "tester",
                        Password = "12345678",
                        Role = "User"
                    },
                    new Person() {
                        UserName = "admin",
                        Password = "qwerty",
                        Role = "Admin"
                    }
                });
        }

        public List<Person> GetPeople()
        {
            return people;
        }

        public void SetUserRefreshToken(int userId, string refreshToken)
        {
            people.FirstOrDefault(u => u.Id == userId).RefreshToken = refreshToken;
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

        }

        public void ConfigureServices(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services
                .AddAuthentication(options => 
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(config => 
                {
                    config.RequireHttpsMetadata = false;
                    config.SaveToken = true;
                    config.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = AuthOptions.ISSUER,
                        ValidAudience = AuthOptions.AUDIENCE,
                        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });

            services.AddMvc();

            services.AddMvcCore().AddVersionedApiExplorer();
            services.AddApiVersioning();
            
            services.AddSingleton<IPeople>(new People());

            services.AddSwaggerGen(options =>
            {
                var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var version in provider.ApiVersionDescriptions)
                {
                    string description = "";
                    if (version.IsDeprecated)
                    {
                        description += "This API deprecated";
                    }

                    options.SwaggerDoc(
                        version.GroupName,
                        new Info()
                        {
                            Title = $"Identity Service API",
                            Version = version.ApiVersion.ToString(),
                            Description = description
                        } 
                    );
                }
            });
        }
    }
}
