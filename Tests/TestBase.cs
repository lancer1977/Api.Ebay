using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace PolyhydraGames.API.Ebay.Tests
{
    public abstract class TestBase<T>
    {
        protected IConfiguration _configuration;
        protected ILogger<T> Logger;
        protected ILoggerFactory LoggerFactory;
        protected IServiceProvider ServiceProvider;

        protected TestBase()
        {

            _configuration = new ConfigurationBuilder()
                //.SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to the test project
                .AddUserSecrets("26d2164f-022c-490e-ab39-68fcec51d04b") // Use the UserSecretsId generated earlier
                .Build();

            var endMock = new Moq.Mock<ILogger<T>>();

            Logger = endMock.Object;
            var loggers = new List<ILoggerProvider>()
            {
                new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
            };
            LoggerFactory = new LoggerFactory(loggers);

        }

        public void BuildServiceProvider(Action<IServiceCollection>? act)
        {
            var services = new ServiceCollection();

            services.AddSingleton(LoggerFactory);
            
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton(_configuration); 
            services.AddEbayOAuth(_configuration);
            act?.Invoke(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        public T GetFake<T>() where T : class
        {
            var moq = new Moq.Mock<T>();
            return moq.Object;
        }

        public string ReadResourceFile(string resourceName)
        {

            // Get the assembly
            var assembly = Assembly.GetExecutingAssembly();

            // Get the stream of the resource file
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new ArgumentException("Resource not found: " + resourceName);
            }

            // Read the contents of the resource file
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }


    }

    public abstract class TestBase
    {
        protected IHost Host { get; set; }
        [SetUp]
        public async Task Setup()
        {
            //await SystemsDatabase.Instance.Initialize();
        }
    }
}
