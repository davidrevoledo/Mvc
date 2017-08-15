// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Testing.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Testing
{
    /// <summary>
    /// Testing extensions for creating an configuring an <see cref="IWebHostBuilder"/> for end to end in memory tests.
    /// </summary>
    public static class WebHostBuilderTestingExtensions
    {
        private static readonly string CreateDefaultBuilder = nameof(CreateDefaultBuilder);

        /// <summary>
        /// Initializes a new <see cref="IWebHostBuilder"/> instance by calling a method 
        /// <c>public static CreateDefaultBuilder(string [] args)</c> on the
        /// entry point class for the assembly. Tipically, <c>Program</c>.
        /// </summary>
        /// <typeparam name="TStartup">The type of the Startup class.</typeparam>
        /// <param name="args">The arguments passed to CreateDefaultBuilder, typically from <c>Program.Main(string [] args)</c>.</param>
        /// <returns>An <see cref="IWebHostBuilder"/> instance.</returns>
        public static IWebHostBuilder FromStartup<TStartup>(string[] args)
        {
            var factory = typeof(TStartup).Assembly.EntryPoint.DeclaringType.GetMethod(CreateDefaultBuilder, BindingFlags.Static | BindingFlags.Public);
            if (factory == null)
            {
                return null;
            }

            return (IWebHostBuilder)factory.Invoke(null, new object[] { args });
        }

        /// <summary>
        /// Configures <see cref="ApplicationPartManager"/> to include the default set
        /// of <see cref="ApplicationPart"/> provided by <see cref="DefaultAssemblyPartDiscoveryProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <typeparam name="TStartup">The Startup type.</typeparam>
        /// <returns>An instance of this <see cref="IWebHostBuilder"/></returns>
        public static IWebHostBuilder UseApplicationAssemblies<TStartup>(this IWebHostBuilder builder)
        {
            var depsFileName = $"{typeof(TStartup).Assembly.GetName().Name}.deps.json";
            var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
            if (!depsFile.Exists)
            {
                throw new InvalidOperationException($"Can't find'{depsFile.FullName}'. This file is required for functional tests " +
                    "to run properly. There should be a copy of the file on your source project bin folder. If thats not the " +
                    "case, make sure that the property PreserveCompilationContext is set to true on your project file. E.g" +
                    "'<PreserveCompilationContext>true</PreserveCompilationContext>'." +
                    $"For functional tests to work they need to either run from the build output folder or the {Path.GetFileName(depsFile.FullName)} " +
                    $"file from your application's output directory must be copied" +
                    "to the folder where the tests are running on. A common cause for this error is having shadow copying enabled when the" +
                    "tests run.");
            }

            var assemblies = DefaultAssemblyPartDiscoveryProvider
                .DiscoverAssemblyParts(typeof(TStartup).Assembly.GetName().Name)
                .Select(s => ((AssemblyPart)s).Assembly)
                .ToList();

            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupConfigureServicesFilter>(new ApplicationAssembliesStartupServicesFilter(assemblies)));

            return builder;
        }

        /// <summary>
        /// Configures the application content root.
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="solutionName">The glob pattern to use for finding the solution.</param>
        /// <param name="solutionRelativePath">The relative path to the content root from the solution file.</param>
        /// <returns>An instance of this <see cref="IWebHostBuilder"/></returns>
        public static IWebHostBuilder UseSolutionRelativeContentRoot(
            this IWebHostBuilder builder,
            string solutionRelativePath,
            string solutionName = "*.sln")
        {
            if (solutionRelativePath == null)
            {
                throw new ArgumentNullException(nameof(solutionRelativePath));
            }

            var applicationBasePath = AppContext.BaseDirectory;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, solutionName).FirstOrDefault();
                if (solutionPath != null)
                {
                    builder.UseContentRoot(Path.GetFullPath(Path.Combine(directoryInfo.FullName, solutionRelativePath)));
                    return builder;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be located using application root {applicationBasePath}.");
        }
    }
}
