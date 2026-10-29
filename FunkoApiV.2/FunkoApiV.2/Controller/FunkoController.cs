using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Service;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApiV._2.Controller;

[ApiController]
[Route("api/[controller]")]
public class FunkoController(IFunkoService ser) : ControllerBase {
    
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FunkoResponseDto>>> GetAll() {
        var funkos = await ser.GetAll();
        return Ok(funkos.ToList());
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FunkoResponseDto>> GetById(int id) {
        var resultado = await ser.GetById(id);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : resultado.Error.TopHttpResult<FunkoResponseDto>();
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<FunkoResponseDto>> GetDeleted() {
        var get = await ser.GetDeleted();
        return Ok(get.ToList());
    }
    [HttpPost]
    public async Task<ActionResult<FunkoResponseDto>> Create([FromBody] FunkoCreateRequest funko) {
        var result = await ser.Add(funko);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.TopHttpResult<FunkoResponseDto>();
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FunkoResponseDto>> Update([FromBody] FunkoUpdateRequest funko, int id) {
        var act = await ser.Update(id, funko);
        return act.IsSuccess
            ? Ok(act.Value)
            : act.Error.TopHttpResult<FunkoResponseDto>();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<bool>> Delete(int id) {
        var result = await ser.SoftDelete(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.TopHttpResult<bool>();
    }
    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<bool>>  Restore(int id) {
        var result = await ser.Restore(id);
        if (result.IsSuccess) return Ok(result.Value);
        var err = result.Error;
        return result.Error switch {
            NotFound notFound => NotFound(new { message = notFound.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = err.Message })
        };
    }
    [HttpGet("categorias")]
    public ActionResult<List<CategoriaDto>> GetCategorias() {
        var categorias = ser.GetAllCategories();
        return Ok(categorias);
    }

    [HttpGet("categorias/{id:long}")]
    public ActionResult<CategoriaDto> GetCategoriaById(int id) {
        var categoria = ser.GetByIdCategories(id);
        return Ok(categoria);
    }
}
