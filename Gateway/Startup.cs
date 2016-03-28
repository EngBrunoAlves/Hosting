﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.ServiceFabric.AspNetCore.Gateway;
using Microsoft.ServiceFabric.Services.Client;
using System;

namespace Gateway
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json")
                                                      .AddEnvironmentVariables()
                                                      .Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //
            // Adds HttpRequestDispatcherProvider that is required by GatewayMiddleware.
            //
            services.AddDefaultHttpRequestDispatcherProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //
            // Scenarios:
            // 1. Multiple services.
            // 2. Various versions or kinds of clients side by side.
            //

            //
            // SMS
            //
            var smsOptions = new GatewayOptions()
            {
                ServiceUri = new Uri("fabric:/Hosting/SmsService", UriKind.Absolute),

                GetServicePartitionKey = context =>
                {
                    var pathSegments = context.Request.Path.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                    string user = null;

                    if (StringComparer.OrdinalIgnoreCase.Equals(context.Request.Method, "GET"))
                    {
                        user = pathSegments[pathSegments.Length - 1];
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(context.Request.Method, "POST"))
                    {
                        user = pathSegments[pathSegments.Length - 2];
                    }

                    return new ServicePartitionKey(Fnv1aHashCode.Get64bitHashCode(user));

                }
            };

            app.Map("/sms",
                subApp =>
                {
                    subApp.RunGateway(smsOptions);
                }
            );

            //
            // Counter
            //
            var counterOptions = new GatewayOptions()
            {
                ServiceUri = new Uri("fabric:/Hosting/CounterService", UriKind.Absolute)
            };

            app.Map("/counter",
                subApp =>
                {
                    subApp.RunGateway(counterOptions);
                }
            );

            app.Map("/Hosting/CounterService",
                subApp =>
                {
                    subApp.RunGateway(counterOptions);
                }
            );

            app.MapWhen(
                context =>
                {
                    StringValues serviceUri;

                    return context.Request.Headers.TryGetValue("SF-ServiceUri", out serviceUri) &&
                           serviceUri.Count == 1 &&
                           serviceUri[0] == "fabric:/Hosting/CounterService";
                },
                subApp =>
                {
                    subApp.RunGateway(counterOptions);
                }
            );
        }
    }
}
