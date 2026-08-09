using CavalosPOC.Data;
using CavalosPOC.Models;
using Microsoft.AspNetCore.Mvc;

namespace CavalosPOC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CavalosController : ControllerBase
{
    private readonly CavaloRepository _repository;

    public CavalosController(CavaloRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("buscar")]
    [ProducesResponseType(typeof(IEnumerable<CavaloRegInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CavaloRegInfo>>> BuscarPorNome([FromQuery] string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return BadRequest("O parâmetro 'nome' é obrigatório.");
        }

        try
        {
            var resultado = await _repository.ObterCavalosPorNomeAsync(nome);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao buscar cavalos: {ex.Message}");
        }
    }
}