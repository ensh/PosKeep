namespace Vtb.PosKeep.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.AspNetCore.SpaServices;
    using Microsoft.AspNetCore.SpaServices.Webpack;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.ResponseCompression;
    
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;
    using Vtb.PosKeep.Entity.Storage;
    
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var machineName = Environment.MachineName;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("Configs/appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"Configs/appsettings.{machineName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddResponseCompression(options => options.EnableForHttps = true);

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.Configure<ConfigOptions>(config =>
            {
                config.AccountCount = Configuration["InMemory:AccountCount"].ToInt(400_000);
                config.AccruedintCount = Configuration["InMemory:AccruedintCount"].ToInt(4_000);
                config.CurrencyCount = Configuration["InMemory:CurrencyCount"].ToInt(200);
                config.InstrumentCount = Configuration["InMemory:InstrumentCount"].ToInt(40_000);
                config.DealCount = Configuration["InMemory:DealCount"].ToInt(2000);
                config.PositionCount = Configuration["InMemory:PositionCount"].ToInt(2000);
                config.QuoteCount = Configuration["InMemory:QuoteCount"].ToInt(40_000);
                config.RateCount = Configuration["InMemory:RateCount"].ToInt(200_000);
                config.TradeAccCount = Configuration["InMemory:TradeAccCount"].ToInt(800_000);
                config.TradeInsCount = Configuration["InMemory:TradeInsCount"].ToInt(200_000);

                config.AccruedintBlockSize = Configuration["Storage:AccruedintBlockSize"].ToInt(20);
                config.AccruedintBlockCount = Configuration["Storage:AccruedintBlockCount"].ToInt(1000);

                config.DealBlockSize = Configuration["Storage:DealBlockSize"].ToInt(20);
                config.DealBlockCount = Configuration["Storage:DealBlockCount"].ToInt(1000);

                config.PositionBlockSize = Configuration["Storage:PositionBlockSize"].ToInt(20);
                config.PositionBlockCount = Configuration["Storage:PositionBlockCount"].ToInt(1000);

                config.QuoteBlockSize = Configuration["Storage:QuoteBlockSize"].ToInt(20);
                config.QuoteBlockCount = Configuration["Storage:QuoteBlockCount"].ToInt(1000);

                config.RateBlockSize = Configuration["Storage:RateBlockSize"].ToInt(20);
                config.RateBlockCount = Configuration["Storage:RateBlockCount"].ToInt(1000);

                config.CacheExpiration = Configuration["Cache:Expiration"].ToInt(1);

                config.DefaultCurrencyDCode = Configuration["Api:DefaultCurrency"].ToInt(810);
                config.DefaultPointsCount = Configuration["Api:DefaultPointsCount"].ToInt(0);
                config.DefaultInstrumentsPageSize = Configuration["Api:DefaultInstrumentsPageSize"].ToInt(1000);
                config.DefaultAccountsPageSize = Configuration["Api:DefaultAccountsPageSize"].ToInt(1000);

                config.RecalcPositionPeriod = Configuration["Recalc:PositionPeriod"].ToInt(3600*24);

                config.DemoClients = () => XElement.Load(Configuration["DemoData:Clients"] ?? @"Clients.xml");
                config.DemoCurrencies = () => XElement.Load(Configuration["DemoData:Currencies"] ?? @"Currencies.xml");
                config.DemoDeals = () => XElement.Load(Configuration["DemoData:Deals"] ?? @"Deals.xml");
                config.DemoInstruments = () => XElement.Load(Configuration["DemoData:Instruments"] ?? @"Instruments.xml");
                config.DemoQuotes = () => XElement.Load(Configuration["DemoData:Quotes"] ?? @"SockData.xml");

                config.CurrencyFileName = Configuration["Data:Currency"] ?? "";
                config.InstrumentsFileName = Configuration["Data:Instruments"] ?? "";
                config.DealsFileName = Configuration["Data:Deals"] ?? "";
                config.QuotesFileName = Configuration["Data:Quotes"] ?? "";
                config.RatesFileName = Configuration["Data:Rates"] ?? "";
            });

            services.AddSingleton(typeof(Common.Logging.ILogger), service => new Common.Logging.AsyncLogger(Configuration));
            
            services.AddSingleton(typeof(DealStorage), service =>
            {
                var options = service.GetService<IOptions<ConfigOptions>>().Value;
                TradeAccount.Init(options.TradeAccCount);
                TradeInstrument.Init(options.TradeInsCount);
                Deal.Init(options.DealCount);
                return new DealStorage(options.DealStorageFactory());
            });

            services.AddSingleton(typeof(PositionStorage), service => 
            {
                var options = service.GetService<IOptions<ConfigOptions>>().Value;
                Position.Init(options.PositionCount);
                return new PositionRecalcStorage(options.PositionStorageFactory());
            });

            services.AddSingleton(typeof(QuoteStorage), service => 
            {
                var options = service.GetService<IOptions<ConfigOptions>>().Value;
                Quote.Init(options.QuoteCount);
                return new QuoteStorage(options.QuoteStorageFactory());
            });

            services.AddSingleton(typeof(RateStorage), service =>
            {
                var options = service.GetService<IOptions<ConfigOptions>>().Value;
                Rate.Init(options.RateCount);
                return new RateStorage(options.RateStorageFactory(), default(CurrencyKey));
            });

            services.AddSingleton(typeof(PortfolioStateStorage), service =>
            {
                var options = service.GetService<IOptions<ConfigOptions>>().Value;
                return new PortfolioStateStorage(options.PortfolioStateStorageFactory());
            });            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseResponseCompression();

            app.UseMvc(routes =>
            {
                routes.MapRoute("api_info", "api/info/{action}/{id?}/{year?}", new { Controller = "Info" });
                routes.MapRoute("api_port", "api/portfolio/{action}/{client_id}/{place_id?}", new { Controller = "Portfolio" });
                routes.MapRoute("api_data", "api/data/{action}/{client_id?}/{instrument_id?}/{currency_id?}", new { Controller = "Data" });

                //routes.MapRoute("version", "Version/{action = Version}", new { Controller = "Version" });

                routes.MapRoute("default", "{controller=Home}/{action=Index}");
            });
        }
    }
}
