using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Areas.ImageTools.Controllers;

[Area("ImageTools")]
[Route("edit-img/[action]")]
public class EditIMGController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public EditIMGController(
        IHttpClientFactory httpClientFactory,
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _userManager = userManager;
        _environment = environment;
    }

    private static ConcurrentDictionary<string, bool> _processingTasks = new();

    [HttpGet("/edit-img")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ProcessEditImageAsync(string task)
    {
        var file = Request.Form.Files["image"];
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var httpClient = _httpClientFactory.CreateClient("FlaskAPI");

        using (var content = new MultipartFormDataContent())
        using (var fileStream = file.OpenReadStream())
        using (var fileContent = new StreamContent(fileStream))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);
            content.Add(new StringContent(task), "task");

            var response = await httpClient.PostAsync("http://localhost:8000/process", content);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Error processing image.");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!responseJson.TryGetProperty("task_id", out var taskIdElement) || taskIdElement.ValueKind == JsonValueKind.Null)
            {
                return BadRequest("Error processing image: response does not contain task_id.");
            }

            string taskId = taskIdElement.GetString()!;
            _processingTasks[taskId] = true;
            return Ok(new { taskId });
        }
    }

    public class CancelRequest
    {
        public string? TaskId { get; set; }
    }
    [HttpPost]
    public async Task<IActionResult> CancelEditImageAsync([FromBody] CancelRequest request)
    {
        if (string.IsNullOrEmpty(request.TaskId) || !_processingTasks.ContainsKey(request.TaskId))
        {
            return BadRequest("Task not found or already completed.");
        }

        var httpClient = _httpClientFactory.CreateClient("FlaskAPI");
        var response = await httpClient.DeleteAsync($"http://localhost:8000/cancel/{request.TaskId}");

        _processingTasks.TryRemove(request.TaskId, out _);

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest("Error canceling image processing.");
        }

        return Ok(new { message = "Task canceled." });
    }

    [HttpGet]
    public async Task<IActionResult> CheckTaskStatus(string taskId)
    {
        var httpClient = _httpClientFactory.CreateClient("FlaskAPI");
        var response = await httpClient.GetAsync($"http://localhost:8000/status/{taskId}");

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest("Error checking task status.");
        }

        var statusJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Json(statusJson);
    }

    [HttpGet]
    public async Task<IActionResult> GetProcessedImage(string taskId, string task)
    {
        var httpClient = _httpClientFactory.CreateClient("FlaskAPI");
        var imageResponse = await httpClient.GetAsync($"http://localhost:8000/result/{taskId}");

        if (!imageResponse.IsSuccessStatusCode)
        {
            return BadRequest("Error retrieving image.");
        }

        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("Không có tài khoản.");
        }

        string userId = user.Id;
        string userFolder = Path.Combine(_environment.ContentRootPath, "Images", "Edited", userId);
        if (!Directory.Exists(userFolder))
        {
            Directory.CreateDirectory(userFolder);
        }

        string fileName = $"{Guid.NewGuid()}.jpg";
        string filePath = Path.Combine(userFolder, fileName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        long fileSizeKB = imageBytes.Length / 1024;
        ActionEdit actionTaken = task.ToLower() switch
        {
            "resolution-enht" => ActionEdit.ResolutionEnht,
            "unblur" => ActionEdit.Unblur, 
            "object-remove" => ActionEdit.ObjectRemoval,
            "background-blur" => ActionEdit.BackgroundBlur,
            "denoise" => ActionEdit.Denoise,
            "color-enht" => ActionEdit.ColorEnht,
            _ => ActionEdit.None
        };
        var editedImage = new EditedImagesModel
        {
            ImagePath = $"Images/Edited/{userId}/{fileName}",
            ActionTaken = actionTaken,
            EditedAt = DateTime.Now,
            ImageKBSize = fileSizeKB,
            UserId = userId
        };

        _dbContext.EditedImages.Add(editedImage);
        await _dbContext.SaveChangesAsync();
        string base64Image = Convert.ToBase64String(imageBytes);
        return Json(new { image = $"data:image/jpeg;base64,{base64Image}" });
    }
}
