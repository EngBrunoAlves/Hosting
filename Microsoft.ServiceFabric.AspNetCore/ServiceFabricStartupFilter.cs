﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Microsoft.ServiceFabric.AspNetCore
{
    public class ServiceFabricStartupFilter : IStartupFilter
    {
        private readonly ServiceFabricOptions _options;

        public ServiceFabricStartupFilter(ServiceFabricOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.UseMiddleware<ServiceFabricMiddleware>(_options);

                next.Invoke(builder);
            };
        }
    }
}
