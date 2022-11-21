using System;
using System.Collections.Generic;
using System.IO;
using Vtb.PosKeep.Entity;
using Vtb.PosKeep.Entity.Business.Model;

namespace Vtb.PosKeep.Server.Controllers
{   
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    
    public class PositionController : Controller
    {        
        private readonly IServiceProvider services;
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan cacheExpiration;

        public PositionController(IMemoryCache cache, IServiceProvider aservices)
        {
            memoryCache = cache;
            services = aservices;

            cacheExpiration = new TimeSpan(0, 0, services.GetService<IOptions<ConfigOptions>>().Value.CacheExpiration);
        }

        [HttpGet]
        public async Task<JsonResult> Get(int client_id, long moment, int currency)
        {
            return new JsonResult("");
        }
    }
}