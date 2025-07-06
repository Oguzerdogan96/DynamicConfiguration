using DynamicConfiguration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ServiceB.Models;

namespace ServiceB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceBController : ControllerBase
    {
        private readonly ConfigurationReader _config;
        public ServiceBController(ConfigurationReader config)
        {
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var allConfigs = await _config.GetAllAsync();
            return Ok(allConfigs);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConfigInsertModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Type))
                return BadRequest("Name ve Type zorunludur.");

            var success = await _config.InsertValueAsync(model.Name, model.Type, model.Value, model.IsActive, model.ApplicationName);
            await _config.LoadConfigurationsAsync();
            if (success)
                return Ok(new { message = "Başarıyla eklendi." });
            else
                return StatusCode(500, new { message = "Kayıt başarısız." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ConfigUpdateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Value))
                return BadRequest("Yeni değer boş olamaz.");

            var success = await _config.UpdateValueAsync(id, model.Name, model.Type, model.Value, model.IsActive, model.ApplicationName);
            await _config.LoadConfigurationsAsync();

            if (success)
                return Ok(new { message = $"{id} başarıyla güncellendi." });
            else
                return NotFound(new { message = $"{id} anahtarı bulunamadı." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _config.DeleteAsync(id);

            if (success)
                return Ok(new { message = $"'{id}' başarıyla silindi." });
            else
                return NotFound(new { message = $"'{id}' bulunamadı veya zaten silinmiş." });
        }
    }
}