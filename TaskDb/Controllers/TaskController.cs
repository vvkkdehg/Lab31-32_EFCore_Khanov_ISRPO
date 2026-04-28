using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskDb.Data;
using TaskDb.Models;

namespace TaskDb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase {
    private readonly AppDbContext _db;
    
    public TasksController(AppDbContext db) {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetAll(
        [FromQuery] bool? completed = null,
        [FromQuery] string? priority = null) {
        var query = _db.Tasks.AsQueryable();

        if (completed.HasValue)
            query = query.Where(t => t.IsCompleted == completed.Value);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetById(int id) {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null)
            return NotFound(new { Message = $"Задача с id={id} не найдена" });
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create([FromBody] CreateTaskDto dtos) {
        if (string.IsNullOrWhiteSpace(dtos.Title))
            return BadRequest(new { Message = "Поле Title обязательно для заполнения" });

        var task = new TaskItem {
            Title = dtos.Title.Trim(),
            Description = dtos.Description?.Trim() ?? string.Empty,
            Priority = dtos.Priority,
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskItem>> Update(int id, [FromBody] UpdateTaskDto dt) {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null)
            return NotFound(new { Message = $"Задача с id={id} не найдена" });
        if (string.IsNullOrWhiteSpace(dt.Title))
            return BadRequest(new { Message = "Поле Title не может быть пустым" });
        
        task.Title = dt.Title;
        task.Description = dt.Description?.Trim() ?? string.Empty;
        task.IsCompleted = dt.IsCompleted;
        task.Priority = dt.Priority;
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPatch("{id}/complete")]
    public async Task<ActionResult<TaskItem>> Complete(int id) {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null)
            return NotFound(new { Message = $"Задача с id={id} не найдена" });
        task.IsCompleted = !task.IsCompleted;
        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id) {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null)
            return NotFound(new { Message = $"Задача с id={id} не найдена" });
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
