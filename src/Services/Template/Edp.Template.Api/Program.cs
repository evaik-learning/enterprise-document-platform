using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Edp.Template.Infrastructure.Persistence;
using Edp.Template.Application.Contracts;
using FluentValidation;
using Edp.Template.Infrastructure.Storage;
using Azure.Storage.Blobs;
using FluentValidation.AspNetCore;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("TemplateDb") ?? "Server=(localdb)\\MSSQLLocalDB;Database=TemplateDb;Trusted_Connection=True;";
builder.Services.AddDbContext<TemplateDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSharedInfrastructure();
builder.Services.AddCurrentUserContext();
builder.Services.AddUnitOfWork<TemplateDbContext>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Blob storage
var blobConn = builder.Configuration.GetConnectionString("BlobStorage") ?? string.Empty;
builder.Services.AddSingleton(sp => new BlobServiceClient(blobConn));
builder.Services.AddScoped<ITemplateStorage, BlobTemplateStorage>();

// Repositories
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<ITemplateVersionRepository, TemplateVersionRepository>();

// Placeholder extractor
builder.Services.AddScoped<IPlaceholderExtractor, Edp.Template.Infrastructure.Document.OpenXmlPlaceholderExtractor>();
builder.Services.AddScoped<IPlaceholderRepository, PlaceholderRepository>();
builder.Services.AddScoped<ITemplateValidator, Edp.Template.Infrastructure.Validation.TemplateValidator>();

var app = builder.Build();

app.UseSharedPlatformMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok("Template API is running"));
app.MapControllers();

app.Run();
