using BlogSite.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = 
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<BlogPostLoader>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DiscordNotificationService>();

var app = builder.Build();

app.UseForwardedHeaders(); 

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.Use(async (context, next) => {
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request Scheme: {Scheme}, Remote IP: {RemoteIp}", 
        context.Request.Scheme, context.Connection.RemoteIpAddress);
    await next();
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
