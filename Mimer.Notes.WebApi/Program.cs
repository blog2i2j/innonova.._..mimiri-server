using Mimer.Framework;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;

var builder = WebApplication.CreateBuilder(args);

//var aes = Aes.Create();
//aes.GenerateKey();

//var rsa = new RSACryptoServiceProvider(4096);

//JsonObject json = new JsonObject();
//json.String("aesKey", Convert.ToBase64String(aes.Key));
//json.String("privateKey", rsa.ExportPkcs8PrivateKeyPem());
//json.String("publicKey", rsa.ExportSubjectPublicKeyInfoPem());

//Console.WriteLine(json.ToString());

MimerServer.CertPath = builder.Configuration.GetValue<string>("CertPath");
MimerServer.WebsocketUrl = builder.Configuration.GetValue<string>("WebsocketUrl");
MimerServer.NotificationsUrl = builder.Configuration.GetValue<string>("NotificationsUrl");
Dev.SetDebugPath(builder.Configuration.GetValue<string>("LogPath")!);
Dev.Log("Start");
MimerServer.DefaultPostgresConnectionString = builder.Configuration.GetConnectionString("Default") ?? "";
MimerServer.AesKey = Convert.FromBase64String(builder.Configuration.GetValue<string>("AesKey") ?? "");
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
Dev.Log("Allowed Origins:");
foreach (var allowedOrigin in allowedOrigins) {
	Dev.Log(allowedOrigin);
}



// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<DevExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<MimerServer>();

builder.Services.AddControllers(options => {
	options.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
});

builder.Services.AddCors(options => {
	options.AddPolicy("Main",
			policy => {
				policy.WithOrigins(allowedOrigins)
					.AllowAnyMethod()
					.AllowAnyHeader()
					.SetPreflightMaxAge(TimeSpan.FromMinutes(4))
					.AllowCredentials();
			});
});

var app = builder.Build();

// Configure global exception handling for all environments
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("Main");
app.MapControllers();

app.Run();
