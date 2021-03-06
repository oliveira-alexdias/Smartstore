﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Core.Bootstrapping;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Core.Web;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.IO;
using Smartstore.Net;
using Smartstore.Web.Bootstrapping;
using Smartstore.Web.Modelling;

namespace Smartstore.Web
{
    internal class MvcStarter : StarterBase
    {
        public MvcStarter()
        {
            RunAfter<WebStarter>();
        }
        
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            // Add action context accessor
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            // Configure Cookie Policy Options
            services.AddSingleton<IConfigureOptions<CookiePolicyOptions>, CookiePolicyOptionsConfigurer>();

            // Add AntiForgery
            services.AddAntiforgery(o => 
            {
                o.Cookie.Name = CookieNames.Antiforgery;
                o.HeaderName = "X-XSRF-Token";
            });

            // Add HTTP client feature
            services.AddHttpClient();
            
            // Add session feature
            services.AddSession(o => 
            {
                o.Cookie.Name = CookieNames.Session;
                o.Cookie.IsEssential = true;
            });

            // Detailed database related error notifications
            services.AddDatabaseDeveloperPageExceptionFilter();

            // Add RazorPages
            services.AddRazorPages();

            services.Configure<RazorViewEngineOptions>(o =>
            {
                // TODO: (core) Register view location formats/expanders
            });

            services.Configure<WebEncoderOptions>(o =>
            {
                o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });

            // TODO: (core) Implement localization stuff
            //services.TryAddSingleton<IStringLocalizerFactory, SmartStringLocalizerFactory>();
            //services.TryAddScoped(typeof(IStringLocalizer<>), typeof(SmartStringLocalizer<>));

            services.AddRouting(o => 
            {
                // TODO: (core) Make this behave like in SMNET
                o.AppendTrailingSlash = true;
                o.LowercaseUrls = true;
            });

            var mvcBuilder = services
                .AddControllersWithViews(o =>
                {
                    //o.EnableEndpointRouting = false;
                    // TODO: (core) AddModelBindingMessagesLocalizer
                    // TODO: (core) Add model binders
                    o.Filters.AddService<IViewDataAccessor>(int.MinValue);
                })
                .AddRazorRuntimeCompilation(o =>
                {
                    // TODO: (core) FileProvider
                })
                //// TODO: (core) Add FluentValidation
                .AddNewtonsoftJson(o =>
                {
                    var settings = o.SerializerSettings;
                    settings.ContractResolver = SmartContractResolver.Instance;
                    settings.TypeNameHandling = TypeNameHandling.Objects;
                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.MaxDepth = 32;
                })
                .AddControllersAsServices()
                .AddAppLocalization()
                .AddMvcOptions(o =>
                {
                    // TODO: (core) More MVC config?

                    // Register custom metadata provider
                    o.ModelMetadataDetailsProviders.Add(new SmartDisplayMetadataProvider());
                });

            // Add TempData feature
            if (appContext.AppConfiguration.UseCookieTempDataProvider)
            {
                mvcBuilder.AddCookieTempDataProvider(o =>
                {
                    o.Cookie.Name = CookieNames.TempData;
                    o.Cookie.IsEssential = true;
                });
            }
            else
            {
                mvcBuilder.AddSessionStateTempDataProvider();
            }

            // Replace BsonTempDataSerializer that was registered by AddNewtonsoftJson()
            // with our own serializer which is capable of serializing more stuff.
            services.AddSingleton<TempDataSerializer, SmartTempDataSerializer>();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterDecorator<SmartLinkGenerator, LinkGenerator>();
            builder.RegisterDecorator<SmartRouteValuesAddressScheme, IEndpointAddressScheme<RouteValuesAddress>>();
            builder.RegisterType<DefaultViewDataAccessor>().As<IViewDataAccessor>().InstancePerLifetimeScope();

            // Convenience: Register IUrlHelper as transient dependency.
            builder.Register<IUrlHelper>(c =>
            {
                var actionContext = c.Resolve<IActionContextAccessor>().ActionContext;

                if (actionContext == null)
                {
                    return null;
                }

                return c.Resolve<IUrlHelperFactory>().GetUrlHelper(actionContext);
            }).InstancePerDependency();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            var appContext = builder.ApplicationContext;

            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app => 
            {
                if (appContext.HostEnvironment.IsDevelopment() || appContext.AppConfiguration.UseDeveloperExceptionPage)
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            });

            builder.Configure(StarterOrdering.StaticFilesMiddleware, app =>
            {
                // TODO: (core) Set StaticFileOptions
                app.UseStaticFiles(new StaticFileOptions 
                { 
                    FileProvider = appContext.WebRoot,
                    ContentTypeProvider = MimeTypes.ContentTypeProvider
                }); 
            });

            builder.Configure(StarterOrdering.BeforeRoutingMiddleware, app =>
            {
                app.UseMiniProfiler();
            });

            builder.Configure(StarterOrdering.RoutingMiddleware, app =>
            {
                app.UseRouting();
            });

            builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
            {
                // TODO: (core) Use Swagger
                // TODO: (core) Use Response compression
            });

            builder.Configure(StarterOrdering.EarlyMiddleware, app =>
            {
                // TODO: (core) Configure session
                app.UseSession();

                if (appContext.IsInstalled)
                {
                    app.UseUrlPolicy();
                    app.UseRequestCulture();
                    app.UseMiddleware<SerilogHttpContextMiddleware>();
                }
            });

            builder.Configure(StarterOrdering.DefaultMiddleware, app =>
            {
                // TODO: (core) Configure cookie policy
                app.UseCookiePolicy();
            });
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.EarlyRoute, routes =>
            {
                //routes.MapControllerRoute(
                //    name: "areas",
                //    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                routes.MapXmlSitemap();

                routes.MapControllers();

                routes.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}