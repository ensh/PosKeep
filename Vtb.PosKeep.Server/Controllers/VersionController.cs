namespace Vtb.PosKeep.Server.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    public class VersionController : Controller
    {
        public VersionController()
        { }

        /// <summary>
        /// Версия компонента WebApi
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Version()
        {
            var versionInfo = new { Version = "test version" };
            return new JsonResult(versionInfo);
        }
    }
}
