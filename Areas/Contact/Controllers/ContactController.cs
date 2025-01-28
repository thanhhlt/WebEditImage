#nullable disable

using System.Globalization;
using App.Areas.Contact.Models;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Contact.Controllers;

[Area("Contact")]
[Route("contact/[action]")]
// [Authorize(Policy = "CanManageContact")]
public class ContactController : Controller
{
    private readonly ILogger<ContactController> _logger;
    private readonly AppDbContext _dbContext;

    private readonly UserManager<AppUser> _userManager;

    public ContactController(
        ILogger<ContactController> logger,
        AppDbContext dbContext,
        UserManager<AppUser> userManager)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        StatusMessage = string.Empty;
    }

    [TempData]
    public string StatusMessage { get; set; }

    [AllowAnonymous]
    public IActionResult GetStatusMessage()
    {
        return PartialView("_StatusMessage");
    }

    //GET: /contact/manage
    [HttpGet("/contact/manage")]
    public async Task<IActionResult> Index(string slug, [FromQuery(Name = "p")] int currentPage, string searchString)
    {
        ViewBag.SearchString = searchString;
        var model = new IndexViewModel();

        var contacts = _dbContext.Contacts.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {

            var contactsSearch = contacts;
            DateTime searchDate;
            bool isDate = DateTime.TryParseExact(
                searchString,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out searchDate);

            if (isDate)
            {
                var startOfDay = searchDate.Date;
                var endOfDay = searchDate.Date.AddDays(1);

                contactsSearch = contactsSearch.Where(c => c.DateSend >= startOfDay && c.DateSend < endOfDay);
            }
            else
            {
                contactsSearch = contactsSearch.Where(c => c.Title.Contains(searchString) ||
                                                        c.Name.Contains(searchString) ||
                                                        c.Content.Contains(searchString));
            }
            contacts = contactsSearch;
        }

        model.Contacts = await contacts.AsNoTracking()
                                .OrderBy(c => c.Status != 0).ThenBy(c => c.Priority).ThenBy(c => c.DateSend)
                                .Select(c => new ContactView
                                {
                                    Id = c.Id,
                                    Title = c.Title,
                                    Status = c.Status,
                                    DateSend = c.DateSend,
                                    Priority = c.Priority,
                                    UserId = c.UserId,
                                    Name = c.Name
                                }).ToListAsync();

        // Pagination
        if (model.Contacts.Any())
        {
            model.currentPage = Math.Max(currentPage, 1);
            model.totalContacts = model.Contacts.Count();
            model.countPages = (int)Math.Ceiling((double)model.totalContacts / model.ITEMS_PER_PAGE);

            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            model.Contacts = model.Contacts.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                                            .Take(model.ITEMS_PER_PAGE).ToList();
        }

        return View(model);
    }

    public class ListContactId
    {
        public List<int> contactIds { get; set; }
    }
    //POST: /admin/post/DeleteMultiPosts
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMultiContactsAsync([FromBody] ListContactId Ids)
    {
        if (Ids.contactIds == null)
        {
            StatusMessage = "Error Không có liên hệ nào.";
            return Json(new { success = false, redirect = Url.Action("Index") });
        }

        var contacts = _dbContext.Contacts.AsEnumerable()
                            .Where(c => Ids.contactIds.Contains(c.Id));

        if (!contacts.Any())
        {
            StatusMessage = "Error Không tìm thấy liên hệ nào.";
            return Json(new { success = false, redirect = Url.Action("Index") });
        }

        _dbContext.Contacts.RemoveRange(contacts);
        await _dbContext.SaveChangesAsync();

        StatusMessage = "Đã xoá những liên hệ đã chọn.";
        return Json(new { success = true, redirect = Url.Action("Index") });
    }

    //POST: /contact/sendcontact
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendContact([Bind(Prefix = "Contact")]SendContactModel model)
    {
        var message = string.Empty;
        var user = await _userManager.GetUserAsync(User);
        if (user == null && string.IsNullOrWhiteSpace(model.Email))
        {
            ModelState.AddModelError("Email", "Email không được bỏ trống nếu chưa đăng nhập.");
            message = "Gửi liên hệ thất bại.";
            return Json(new { success = false, message = message });
        }

        if (!ModelState.IsValid)
        {
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            message = $"Gửi liên hệ thất bại.<br/> {string.Join("<br/>", errorMessages)}";
            return Json(new { success = false, message = message });
        }

        int priority = await _dbContext.Memberships.AsNoTracking()
                                    .Where(m => m.UserId == user.Id)
                                    .Select(m => m.MembershipDetails.PrioritySupport)
                                    .FirstOrDefaultAsync();

        ContactsModel contact = new ContactsModel()
        {
            Name = model.Name,
            Email = model.Email,
            Title = model.Title,
            Content = model.Content,
            Status = 0,
            DateSend = DateTime.UtcNow,
            Priority = (user == null) ? 4 : (priority == 0 ? 3 : priority),
            UserId = user?.Id
        };

        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync();

        if (user == null)
        {
            message = $"Gửi liên hệ thành công.";
            return Json(new { success = true, message = message });
        }
        var url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("ContactDetail", new { id = contact.Id })}";
        message = $"Gửi liên hệ thành công. Xem chi tiết tại <a href='{url}' target='_blank'>đây</a>";
        return Json(new { success = true, message = message });
    }

    //GET: /contact/{id}
    [AllowAnonymous]
    [HttpGet("/contact/{id}")]
    public async Task<IActionResult> ContactDetail(int? id)
    {
        if (id == null)
        {
            return NotFound("Không tìm thấy liên hệ");
        }
        ContactDetailModel contact = await _dbContext.Contacts.Where(c => c.Id == id)
                                            .Select(c => new ContactDetailModel
                                            {
                                                Id = c.Id,
                                                Name = c.Name,
                                                Email = c.Email,
                                                Title = c.Title,
                                                Content = c.Content,
                                                Response = c.Response,
                                                Status = c.Status,
                                                DateSend = c.DateSend,
                                                Priority = c.Priority,
                                                UserId = c.UserId,
                                                UserName = c.User.UserName,
                                            }).FirstOrDefaultAsync();

        if (contact == null)
        {
            return NotFound("Không tìm thấy liên hệ");
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var isOwner = contact.UserId == currentUser?.Id;

        if (!isOwner)
        {
            return Forbid();
        }

        return View(contact);
    }

    //POST: /contact/DeleteContact
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContactAsync(int? id)
    {
        if (id == null)
        {
            StatusMessage = "Không tìm thấy liên hệ";
            return Json(new { success = false });
        }
        var contact = await _dbContext.Contacts.Where(c => c.Id == id).FirstOrDefaultAsync();
        if (contact == null)
        {
            StatusMessage = "Không tìm thấy liên hệ";
            return Json(new { success = false });
        }

        _dbContext.Contacts.Remove(contact);
        await _dbContext.SaveChangesAsync();

        return Json(new {success = true, redirect = Url.RouteUrl("SendContact")});
    }

    //POST: /contact/ResponseContact
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResponseContactAsync([Bind("Id", "Response")] ContactDetailModel model)
    {
        var contact = await _dbContext.Contacts.FindAsync(model.Id);
        if (contact == null)
        {
            StatusMessage = "Không tìm thấy liên hệ";
            return Json(new { success = false });
        }

        contact.Response = model.Response;
        contact.Status = 1;
        await _dbContext.SaveChangesAsync();
        
        StatusMessage = "Phản hồi đã được cập nhật thành công!";
        return Json(new { success = true });
    }

    //GET: /contact/ListContacts
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListContactsAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Không tìm thấy tài khoản");
        }
        var contacts = await _dbContext.Contacts.AsNoTracking()
                                            .Where(c => c.UserId == user.Id)
                                            .OrderBy(c => c.Status != 0).ThenBy(c => c.DateSend)
                                            .Select(c => new ContactView
                                            {
                                                Id = c.Id,
                                                Title = c.Title,
                                                Status = c.Status,
                                                DateSend = c.DateSend,
                                            }).ToListAsync();

        return View(contacts);  
    }
}