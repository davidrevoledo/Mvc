// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class MvcTestFixture<TStartup> : WebApplicationTestFixture<TStartup>
        where TStartup : class
    {
        public MvcTestFixture()
            : base(Path.Combine("test", "WebSites", typeof(TStartup).Assembly.GetName().Name))
        {
        }

        protected MvcTestFixture(string solutionRelativePath)
            : base(solutionRelativePath)
        {
        }

        protected override void ConfigureApplication(IWebHostBuilder builder)
        {
            builder.UseRequestCulture<TStartup>("en-GB", "en-US");
            builder.ConfigureServices(services => {
                var registrations = services.Where(s => s.ImplementationType != null && s.ImplementationType.Equals(typeof(ApplicationAssembliesStartupServicesFilter)));
                foreach (var registration in registrations)
                {
                    services.Remove(registration);
                }

                services.AddSingleton<IStartupConfigureServicesFilter>(
                    new ApplicationAssembliesStartupServicesFilter(new List<Assembly> { typeof(TStartup).GetTypeInfo().Assembly }));
            });
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-GB");
                CultureInfo.CurrentUICulture = new CultureInfo("en-US");
                return base.CreateServer(builder);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }
    }
}
