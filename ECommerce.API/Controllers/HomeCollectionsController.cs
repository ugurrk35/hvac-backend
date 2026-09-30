using System.Text.Json;
using ECommerce.Repository.Data;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
public class HomeCollectionsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;
    public HomeCollectionsController(IWebHostEnvironment environment, ApplicationDbContext db) { _environment = environment; _db = db; }
    public class Collection { public string Id { get; set; } = Guid.NewGuid().ToString("N"); public string Title { get; set; } = "Yeni vitrin"; public string? Eyebrow { get; set; } public bool IsEnabled { get; set; } = true; public List<int> ProductIds { get; set; } = new(); public int MaxItems { get; set; } = 4; public int GridColumns { get; set; } = 4; }
    public class PublicCollection { public string Id { get; set; } = ""; public string Title { get; set; } = ""; public string? Eyebrow { get; set; } public int MaxItems { get; set; } public int GridColumns { get; set; } public IReadOnlyList<ProductListDto> Items { get; set; } = Array.Empty<ProductListDto>(); }
    private string FilePath() => Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "home-collections.json");
    private async Task<List<Collection>> Read() { try { var path=FilePath(); if(System.IO.File.Exists(path)) return JsonSerializer.Deserialize<List<Collection>>(await System.IO.File.ReadAllTextAsync(path)) ?? new(); } catch { } return new(); }
    [HttpGet("api/admin/home/collections")][Authorize(Roles="Admin")] public async Task<ActionResult> GetAdmin() => Ok(await Read());
    [HttpPost("api/admin/home/collections")][Authorize(Roles="Admin")] public async Task<ActionResult> Save([FromBody] List<Collection>? collections) { var valid=(collections??new()).Take(12).Select(x=>new Collection { Id=string.IsNullOrWhiteSpace(x.Id)?Guid.NewGuid().ToString("N"):x.Id, Title=string.IsNullOrWhiteSpace(x.Title)?"Yeni vitrin":x.Title.Trim(), Eyebrow=x.Eyebrow?.Trim(), IsEnabled=x.IsEnabled, ProductIds=x.ProductIds.Where(i=>i>0).Distinct().Take(8).ToList(), MaxItems=Math.Clamp(x.MaxItems,1,8), GridColumns=Math.Clamp(x.GridColumns,2,4) }).ToList(); var path=FilePath(); Directory.CreateDirectory(Path.GetDirectoryName(path)!); await System.IO.File.WriteAllTextAsync(path,JsonSerializer.Serialize(valid,new JsonSerializerOptions{WriteIndented=true})); return Ok(valid); }
    [HttpGet("api/home/collections")][AllowAnonymous] public async Task<ActionResult> GetPublic() { var collections=await Read(); var ids=collections.Where(x=>x.IsEnabled).SelectMany(x=>x.ProductIds).Distinct().ToList(); var products=await _db.Products.Include(x=>x.ProductImages).ThenInclude(x=>x.Image).Include(x=>x.Category).Where(x=>ids.Contains(x.Id)&&x.IsPublished&&!x.IsDeleted).ToListAsync(); var result=collections.Where(x=>x.IsEnabled).Select(x=>new PublicCollection { Id=x.Id,Title=x.Title,Eyebrow=x.Eyebrow,MaxItems=x.MaxItems,GridColumns=x.GridColumns,Items=products.Where(p=>x.ProductIds.Contains(p.Id)).OrderBy(p=>x.ProductIds.IndexOf(p.Id)).Take(x.MaxItems).Select(p=>p.ToListDto()).ToList() }).Where(x=>x.Items.Count>0).ToList(); return Ok(DataResponse<List<PublicCollection>>.CreateSuccess(result)); }
}
