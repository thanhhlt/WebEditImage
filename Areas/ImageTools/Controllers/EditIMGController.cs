using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace App.Areas.ImageTools.Controllers;

[Area("ImageTools")]
[Route("edit-img/[action]")]
[Authorize("CanUseImgTools")]
public class EditIMGController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuthorizationService _authorizationService;

    public EditIMGController(
        IHttpClientFactory httpClientFactory,
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IWebHostEnvironment environment,
        IAuthorizationService authorizationService
    )
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _userManager = userManager;
        _environment = environment;
        _authorizationService = authorizationService;
    }

    private static ConcurrentDictionary<string, bool> _processingTasks = new();

    [HttpGet("/edit-img")]
    public async Task<IActionResult> Index()
    {
        var tasksAccess = new Dictionary<string, bool>
        {
            { "resolution-enht", (await _authorizationService.AuthorizeAsync(User, "resolution-enht", "AllowFeatureAccess")).Succeeded },
            { "unblur", (await _authorizationService.AuthorizeAsync(User, "unblur", "AllowFeatureAccess")).Succeeded },
            { "object-remove", (await _authorizationService.AuthorizeAsync(User, "object-remove", "AllowFeatureAccess")).Succeeded },
            { "background-blur", (await _authorizationService.AuthorizeAsync(User, "background-blur", "AllowFeatureAccess")).Succeeded },
            { "color-enht", (await _authorizationService.AuthorizeAsync(User, "color-enht", "AllowFeatureAccess")).Succeeded },
            { "denoise", (await _authorizationService.AuthorizeAsync(User, "denoise", "AllowFeatureAccess")).Succeeded }
        };

        ViewBag.TasksAccess = tasksAccess;

        return View();
    }

    [HttpGet("check-save-permission")]
    public async Task<IActionResult> CheckSavePermission()
    {
        var result = await _authorizationService.AuthorizeAsync(User, null, "AllowSaveImage");
        return Ok(new { canSave = result.Succeeded });
    }
    [HttpGet("check-quota")]
    public async Task<IActionResult> CheckQuota()
    {
        var result = await _authorizationService.AuthorizeAsync(User, null, "ImageQuota");
        return Ok(new { canProcess = result.Succeeded });
    }

    [HttpPost]
    public async Task<IActionResult> ProcessEditImageAsync(string task)
    {
        var imageQuota = await _authorizationService.AuthorizeAsync(User, null, "ImageQuota");
        if (!imageQuota.Succeeded)
        {
            return Forbid("Bạn đã đến giới hạn số ảnh được xử lý. Quay lại vào ngày mai hoặc nâng cấp membership.");
        }

        var allowFeatureAccess = await _authorizationService.AuthorizeAsync(User, task, "AllowFeatureAccess");
        if (!allowFeatureAccess.Succeeded)
        {
            return Forbid("Bạn không có quyền sử dụng tính năng này.");
        }

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
        var editedImage = new EditedImagesModel()
        {
            UserId = userId,
            ActionTaken = actionTaken,
            EditedAt = DateTime.Now,
            ImagePath = ""
        };

        // Authorization
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, null, "AllowSaveImage");

        if (authorizationResult.Succeeded)
        {
            // Save Image
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

            //Save Thumbnail
            string thumbnailFolder = Path.Combine(userFolder, "Thumbnails");
            if (!Directory.Exists(thumbnailFolder))
            {
                Directory.CreateDirectory(thumbnailFolder);
            }

            string thumbnailPath = Path.Combine(thumbnailFolder, fileName);
            using (var memoryStream = new MemoryStream(imageBytes))
            using (var image = Image.Load(memoryStream))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(320, 240),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsJpegAsync(thumbnailPath);
            }

            long fileSizeKB = imageBytes.Length / 1024;
    
            editedImage.ImagePath = $"Images/Edited/{userId}/{fileName}";
            editedImage.ImageKBSize = fileSizeKB;
        }

        _dbContext.EditedImages.Add(editedImage);
        await _dbContext.SaveChangesAsync();
        string base64Image = Convert.ToBase64String(imageBytes);
        return Json(new { image = $"data:image/jpeg;base64,{base64Image}" });
    }
}
