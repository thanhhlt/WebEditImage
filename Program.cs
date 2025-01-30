using System.Globalization;
using App.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddErrorDescriber<AppIdentityErrorDescriber>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
builder.Logging.AddConsole();

// Connect database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    string connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS") ?? "";
    options.UseMySql(connectionString, MySqlServerVersion.AutoDetect(connectionString));
});

// Send Email
builder.Services.AddSingleton<IEmailSender>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<SendMailService>>();
    string sendgridApiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY") ?? "";
    return new SendMailService(logger, sendgridApiKey);
});

// Add HttpClient
builder.Services.AddHttpClient("FlaskAPI", client =>
{
    client.Timeout = TimeSpan.FromMinutes(20);
});

builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IDeleteUserService, DeleteUserService>();

//IdentityOptions
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    //Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.AllowedForNewUsers = true;

    //User.
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    //Login.
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});

// External Login

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "";
        options.ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "";
        options.CallbackPath = new PathString("/signin-google");
        // https://localhost:5000/signin-google
        options.AccessDeniedPath = new PathString("/externalloginfail");
    })
    .AddFacebook(options =>
    {
        options.AppId = Environment.GetEnvironmentVariable("APP_ID") ?? "";
        options.AppSecret = Environment.GetEnvironmentVariable("APP_SECRET") ?? "";
        options.CallbackPath = new PathString("/sign-facebook");
        // https://localhost:5000/signin-facebook
        options.AccessDeniedPath = new PathString("/externalloginfail");
    });

// Config Format Time
var cultureInfo = new CultureInfo("vi-VN");
cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
cultureInfo.DateTimeFormat.LongDatePattern = "dd/MM/yyyy";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // options.ValidationInterval = TimeSpan.FromMinutes(5);
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseWebSockets();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Images")
    ),
    RequestPath = "/imgs"
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws-process-image")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await new ImageProcessWebSocketHandler().Handle(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});
app.Run();
