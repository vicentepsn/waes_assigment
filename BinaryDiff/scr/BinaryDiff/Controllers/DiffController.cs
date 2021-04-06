using BinaryDiff.Services;
using BinaryDiff.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDiff.Controllers
{
    [ApiController]
    [Route("v1/diff")]
    public class DiffController : Controller
    {
        private readonly DiffService _diffService;
        public DiffController(DiffService diffService)
        {
            _diffService = diffService;
        }

        [HttpPut("{id}/{dataSide}")]
        public async Task<IActionResult> SetLeft([FromRoute] int id, DiffDataSide dataSide)
        {
            try
            {
                string bodyContent;
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    bodyContent = await reader.ReadToEndAsync();
                }

                _diffService.SetData(id, bodyContent, dataSide);
                return Ok();
            }
            catch (HttpResponseException ex)
            {
                return StatusCode(ex.Status, ex.Value);
            }
        }

        [HttpPut("{id}/rightwww")]
        public async Task<IActionResult> SetRight([FromRoute] int id)
        {
            try
            {
                string bodyContent;
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    bodyContent = await reader.ReadToEndAsync();
                }

                _diffService.SetData(id, bodyContent, DiffDataSide.Right);
                return Ok();
            }
            catch (HttpResponseException ex)
            {
                return StatusCode(ex.Status, ex.Value);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiff([FromRoute] int id)
        {
            try
            {
                var diffResult = _diffService.GetDiff(id);
                return Ok(diffResult);
            }
            catch (HttpResponseException ex)
            {
                return StatusCode(ex.Status, ex.Value);
            }
        }
    }
}
