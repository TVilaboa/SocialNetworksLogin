using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SocialNetworksLogin.Data;
using SocialNetworksLogin.Models;
using SocialNetworksLogin.Services;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace SocialNetworksLogin
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();


          
           
            services.AddAuthentication()
                .AddFacebook(options =>
                {
                    options.AppId = "1889486091362759";
                    options.AppSecret = "215a45d1fda94c7b0c8e308e42cb46cd";
                    options.Scope.Add("user_birthday");
                    options.Scope.Add("public_profile");
                    options.Fields.Add("birthday");
                    options.Fields.Add("picture");
                    options.Fields.Add("name");
                    options.Fields.Add("gender");
                    options.Fields.Add("picture");
                    options.SaveTokens = true;
                    //options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    //options.SaveTokens = true;
                });

          
            services.AddAuthentication().AddTwitter(twitterOptions =>
            {
                //twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                //twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
                twitterOptions.ConsumerKey = "Z3fUDKtxPhN2ZuXakigoy6fe9";
                twitterOptions.ConsumerSecret = "h4hF4Ce48LTmXnCbhK45wiLX7H2JrztHfTJAY5GqwOj3rijUwu";
                twitterOptions.RetrieveUserDetails = true;
                twitterOptions.SaveTokens = true;
            });

            services.AddAuthentication().AddInstagram(options =>
            {
                options.ClientId = "d0fdd1b380a94c4781f0f8ad8921531f";
                options.ClientSecret = "1c4d6a9c48764d72b8dac65960e2df14";
                options.AuthorizationEndpoint = "https://api.instagram.com/oauth/authorize/";
                options.CallbackPath = "/signin-instagram";
                options.TokenEndpoint = "https://api.instagram.com/oauth/access_token";
                options.Scope.Add("basic");
                options.ClaimsIssuer = "Instagram";
                options.SaveTokens = true;
                options.UserInformationEndpoint = "https://api.instagram.com/v1/users/self";
                options.SignInScheme =IdentityConstants.ExternalScheme;
                options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
                {

                    
                    OnCreatingTicket = async context =>
                        {
                            var url = context.Options.UserInformationEndpoint + "?access_token=" + context.AccessToken;
                            var request = new HttpRequestMessage(HttpMethod.Get, url);
                      
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var data = JObject.Parse(await response.Content.ReadAsStringAsync());
                            var user = data["data"];
                            var userId = user.Value<string>("id");
                            if (!string.IsNullOrEmpty(userId))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            }

                            var formattedName = user.Value<string>("full_name");
                            if (!string.IsNullOrEmpty(formattedName))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.Name, formattedName, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            }

                            var email = user.Value<string>("emailAddress");
                            if (!string.IsNullOrEmpty(email))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.Email, email, ClaimValueTypes.String,
                                    context.Options.ClaimsIssuer));
                            }
                            var pictureUrl = user.Value<string>("profile_picture");
                            if (!string.IsNullOrEmpty(pictureUrl))
                            {
                                context.Identity.AddClaim(new Claim("profile-picture", pictureUrl, ClaimValueTypes.String,
                                    context.Options.ClaimsIssuer));
                            }
                        }
                };
            });

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

         app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
