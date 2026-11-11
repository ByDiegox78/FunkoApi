using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApiV._2.Controller;

[ApiController]
[Route("api/funkos")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
public class FunkoController(IFunkoService ser) : ControllerBase {

    /// <summary>Lista Funkos paginados y permite filtrar y ordenar los resultados.</summary>
    /// <param name="filter">Filtros: nombre, categoría, proveedor, precio mínimo/máximo, stock mínimo y si incluye eliminados; página base cero, tamaño 1–100 y orden. Incluir eliminados requiere ADMIN.</param>
    /// <response code="200">Página de resultados y total coincidente.</response>
    /// <response code="400">Algún parámetro de filtro o paginación no es válido.</response>
    [ProducesResponseType(typeof(PagedResult<FunkoResponseDto>), StatusCodes.Status200OK)]
    [HttpGet]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<FunkoResponseDto>>> GetAll([FromQuery] FunkoFilterRequest filter) {
        if (filter.IncludeDeleted && !User.IsInRole("ADMIN")) {
            return User.Identity?.IsAuthenticated == true
                ? Forbid()
                : Unauthorized();
        }
        return Ok(await ser.GetAll(filter));
    }

    /// <summary>Obtiene un Funko por su identificador.</summary>
    /// <param name="id">Identificador numérico de 64 bits.</param>
    /// <response code="200">Funko encontrado.</response>
    /// <response code="404">No existe un Funko activo con ese identificador.</response>
    /// <response code="401">Falta autenticación.</response>
    /// <response code="403">El usuario no tiene el rol ADMIN.</response>
    [ProducesResponseType(typeof(FunkoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<FunkoResponseDto>> GetById(long id) {
        var resultado = await ser.GetById(id);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : resultado.Error.TopHttpResult<FunkoResponseDto>();
    }

    /// <summary>Crea un Funko.</summary>
    /// <param name="funko">Nombre, precio, stock, imagen y los identificadores existentes de categoría y proveedor.</param>
    /// <response code="201">Funko creado; incluye su representación.</response>
    /// <response code="400">Entrada inválida o campos que no pasan validación.</response>
    /// <response code="409">La categoría o el proveedor no existen o están eliminados.</response>
    /// <response code="401">Falta autenticación.</response>
    /// <response code="403">El usuario no tiene el rol ADMIN.</response>
    [ProducesResponseType(typeof(FunkoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<FunkoResponseDto>> Create([FromBody] FunkoCreateRequest funko) {
        var result = await ser.Add(funko);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.TopHttpResult<FunkoResponseDto>();
    }

    /// <summary>Actualiza los datos de un Funko existente.</summary>
    /// <param name="id">Identificador numérico de 64 bits.</param>
    /// <param name="funko">Datos actualizados, incluidos categoría y proveedor existentes.</param>
    /// <response code="200">Funko actualizado.</response>
    /// <response code="400">Entrada inválida o campos que no pasan validación.</response>
    /// <response code="404">El Funko no existe.</response>
    /// <response code="409">La categoría o el proveedor no existen o están eliminados, o el Funko está eliminado.</response>
    /// <response code="401">Falta autenticación.</response>
    /// <response code="403">El usuario no tiene el rol ADMIN.</response>
    [ProducesResponseType(typeof(FunkoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<FunkoResponseDto>> Update(long id, [FromBody] FunkoUpdateRequest funko) {
        var result = await ser.Update(id, funko);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.TopHttpResult<FunkoResponseDto>();
    }

    /// <summary>Elimina lógicamente un Funko.</summary>
    /// <param name="id">Identificador numérico de 64 bits.</param>
    /// <response code="204">Funko marcado como eliminado; no devuelve cuerpo.</response>
    /// <response code="404">El Funko no existe.</response>
    /// <response code="401">Falta autenticación.</response>
    /// <response code="403">El usuario no tiene el rol ADMIN.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<bool>> Delete(long id) {
        var result = await ser.Delete(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.TopHttpResult<bool>();
    }

    /// <summary>Guarda una imagen para un Funko y devuelve su ruta relativa.</summary>
    /// <remarks>Requiere rol ADMIN.</remarks>
    /// <param name="file">Archivo multipart; extensiones y tamaño máximo dependen de la configuración de almacenamiento.</param>
    /// <response code="200">Imagen guardada; devuelve la ruta relativa.</response>
    /// <response code="422">Archivo vacío, extensión o tipo no permitido, o tamaño superior al límite.</response>
    /// <response code="500">Error interno al guardar el archivo.</response>
    [ProducesResponseType(typeof(ImagenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "ADMIN")]
    [HttpPost("imagen")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImagenResponseDto>> UploadImagen(IFormFile file) {
        var result = await ser.UploadImagen(file);
        return result.IsSuccess
            ? Ok(new ImagenResponseDto(result.Value))
            : result.Error.TopHttpResult<ImagenResponseDto>();
    }

    /// <summary>Reemplaza la imagen de un Funko existente.</summary>
    /// <remarks>Requiere rol ADMIN.</remarks>
    /// <param name="id">Identificador numérico de 64 bits.</param>
    /// <param name="file">Archivo multipart de imagen.</param>
    /// <response code="200">Funko actualizado con la nueva imagen.</response>
    /// <response code="422">Archivo vacío, extensión o tipo no permitido, o tamaño superior al límite.</response>
    /// <response code="404">El Funko no existe.</response>
    /// <response code="500">Error interno al guardar o limpiar el archivo.</response>
    [ProducesResponseType(typeof(FunkoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{id:long}/imagen")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FunkoResponseDto>> UpdateImagen(long id, IFormFile file) {
        var result = await ser.UpdateImagen(id, file);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.TopHttpResult<FunkoResponseDto>();
    }
}
