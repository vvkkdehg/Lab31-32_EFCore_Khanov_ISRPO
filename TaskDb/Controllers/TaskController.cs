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
    public async Task<ActionResult<TaskItem>> Create([FromBody] CreateTaskDto dto) {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { Message = "Поле Title обязательно для заполнения" });

        var task = new TaskItem {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Priority = dto.Priority,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            DueDate = dto.DueDate
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskItem>> Update(int id, [FromBody] UpdateTaskDto dto) {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null)
            return NotFound(new { Message = $"Задача с id={id} не найдена" });
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { Message = "Поле Title не может быть пустым" });
        
        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim() ?? string.Empty;
        task.IsCompleted = dto.IsCompleted;
        task.Priority = dto.Priority;
        task.DueDate = dto.DueDate;
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

    [HttpPatch("complete-all")]
    public async Task<ActionResult> CompleteAll() {
        var tasks = await _db.Tasks.Where(t => !t.IsCompleted).ToListAsync();
        foreach (var task in tasks) {
            task.IsCompleted = true;
        }
        await _db.SaveChangesAsync();
        return Ok(new { Updated = tasks.Count });
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TaskItem>>> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? priority = null,
        [FromQuery] bool? completed = null) {
        var q = _db.Tasks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(t => t.Title.Contains(query) || t.Description.Contains(query));
        if (!string.IsNullOrWhiteSpace(priority))
            q = q.Where(t => t.Priority == priority);
        if (completed.HasValue)
            q = q.Where(t => t.IsCompleted == completed.Value);
        var results = await q
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return Ok(results);
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats() {
        var total = await _db.Tasks.CountAsync();
        var completed = await _db.Tasks.CountAsync(t => t.IsCompleted);
        var pending = total - completed;
        var byPriority = await _db.Tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();
        var recentDate = DateTime.UtcNow.AddDays(-7);
        var recentCount = await _db.Tasks
            .CountAsync(t => t.CreatedAt >= recentDate);
        return Ok(new {
            Total = total,
            Completed = completed,
            Pending = pending,
            CompletionPct = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            ByPriority = byPriority,
            CreatedLastWeek = recentCount
        });
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5) {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        if (pageSize > 50) pageSize = 50;
        var totalCount = await _db.Tasks.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var tasks = await _db.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return Ok(new {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPrev = page > 1,
            HasNext = page < totalPages,
            Items = tasks
        });
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetOverdue() {
        var now = DateTime.UtcNow;
        var overdue = await _db.Tasks
            .Where(t => t.DueDate != null
                && t.DueDate < now
                && !t.IsCompleted)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        return Ok(overdue);
    }

    [HttpDelete("completed")]
    public async Task<ActionResult> DeleteCompleted() {
        var tasks = await _db.Tasks.Where(t => t.IsCompleted).ToListAsync();
        _db.Tasks.RemoveRange(tasks);
        await _db.SaveChangesAsync();
        return Ok(new { Deleted = tasks.Count });
    }
}