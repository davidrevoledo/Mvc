// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Testing.Internal
{
    public class ApplicationAssembliesStartupServicesFilter : IStartupConfigureServicesFilter
    {
        public ApplicationAssembliesStartupServicesFilter(IList<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            Assemblies = assemblies;
        }

        public IList<Assembly> Assemblies { get; }

        public Action<IServiceCollection> ConfigureServices(Action<IServiceCollection> next)
        {
            return ConfigureServices;

            void ConfigureServices(IServiceCollection services)
            {
                var manager = new ApplicationPartManager();
                for (var i = 0; i < Assemblies.Count; i++)
                {
                    manager.ApplicationParts.Add(new AssemblyPart(Assemblies[i]));
                }

                next(services);
            }
        }
    }
}
