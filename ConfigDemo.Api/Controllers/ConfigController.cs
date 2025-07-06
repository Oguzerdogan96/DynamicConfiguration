using DynamicConfiguration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConfigDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly ConfigurationReader _config;
        public ConfigController(ConfigurationReader config)
        {
            _config = config;
        }

        [HttpGet("name")]
        public IActionResult GetName()
        {
            var siteName = _config.GetValue<string>("SiteName");
            var isBasketEnabled = _config.GetValue<string>("IsBasketEnabled");
            var maxItemCount = _config.GetValue<int>("MaxItemCount");

            return Ok(new
            {
                SiteName = siteName,
                IsBasketEnabled = isBasketEnabled,
                MaxItemCount = maxItemCount
            });
        }
    }
}
