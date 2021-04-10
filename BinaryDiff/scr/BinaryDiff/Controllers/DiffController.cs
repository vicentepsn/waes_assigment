using BinaryDiff.Model;
using BinaryDiff.Services;
using BinaryDiff.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// Stores a comparable object with the data of one side (left or right). Creates a New object if it does not
        /// exists or override a information of a existing one.
        /// </summary>
        /// <param name="id">The Id of the comparable object</param>
        /// <param name="dataSide">The side to store the data, left or right</param>
        /// <param name="diffPayload">Contein the data to be compared, a base64 encoded binary data</param>
        [HttpPut("{id}/{dataSide}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromRoute] DiffDataSide dataSide, [FromBody] DiffPayload diffPayload)
        {
            try
            {
                _diffService.SetData(id, diffPayload.EncodedBinaryData, dataSide);
                return Ok();
            }
            catch (HttpResponseException ex)
            {
                return StatusCode(ex.Status, ex.Value);
            }
        }

        /// <summary>
        /// Compares both sides of a comparable object and return the detais of the comparison
        /// </summary>
        /// <param name="id">The Id of the comparable object</param>
        /// <returns>Returns a DiffResult objetc containing the detais of the comparison</returns>
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
