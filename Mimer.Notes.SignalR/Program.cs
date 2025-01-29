using Mimer.Framework;
using Mimer.Notes.SignalR;
using Mimer.Notes.SignalR.Authentication;
using Mimer.Notes.SignalR.Base;
using Mimer.Notes.SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

NotificationServer.CertPath = builder.Configuration.GetValue<string>("CertPath");
Dev.SetDebugPath(builder.Configuration.GetValue<string>("LogPath")!);
Dev.Log("Start");
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<NotificationServer>();
builder.Services.AddAuthentication()
		.AddScheme<MimerAuthenticationSchemeOptions, MimerAuthenticationHandler>(
				"MimerAuth",
				opts => { }
		);


builder.Services.AddCors(options => {
	options.AddPolicy("Main",
			policy => {
				policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
			});
});

builder.Services.AddControllers(options => {
	options.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
});

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
	app.UseExceptionHandler(_ => { });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("Main");
app.MapControllers();
app.MapHub<NotificationsHub>("/notifications");

app.Run();
