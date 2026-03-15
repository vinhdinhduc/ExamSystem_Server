using System.ComponentModel.DataAnnotations;
using ExamSystem.DTOs;
using ExamSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExamDto>>> GetAllAsync()
    {
        var exams = await _examService.GetAllAsync();
        return Ok(exams);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExamDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var exam = await _examService.GetByIdAsync(id);
            return Ok(exam);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<ExamDto>> CreateAsync([FromBody] ExamCreateDto dto)
    {
        try
        {
            var exam = await _examService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = exam.Id }, exam);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExamDto>> UpdateAsync(Guid id, [FromBody] ExamUpdateDto dto)
    {
        try
        {
            var exam = await _examService.UpdateAsync(id, dto);
            return Ok(exam);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        try
        {
            await _examService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
