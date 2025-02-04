using App.Models;
using Microsoft.EntityFrameworkCore;

public class MembershipExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MembershipExpirationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndDeleteExpiredMemberships();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckAndDeleteExpiredMemberships()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var expiredMemberships = await dbContext.Memberships
                .Where(m => m.EndTime < DateTime.Now)
                .ToListAsync();

            if (expiredMemberships.Any())
            {
                dbContext.Memberships.RemoveRange(expiredMemberships);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}