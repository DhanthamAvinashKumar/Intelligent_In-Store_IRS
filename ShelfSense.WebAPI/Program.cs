using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.Interfaces;
using ShelfSense.Application.Mapping;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Infrastructure.Repositories;
using System;

var builder = WebApplication.CreateBuilder(args);
 
// Add services to the container.


// Register repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IStoreRepository,StoreRepository>();
builder.Services.AddScoped<IShelfRepository, ShelfRepository>();
builder.Services.AddScoped<IProductShelfRepository, ProductShelfRepository>();
builder.Services.AddScoped<IReplenishmentAlert, ReplenishmentAlertRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IRestockTaskRepository, RestockTaskRepository>();
builder.Services.AddScoped<IInventoryReport, InventoryReportRepository>();
builder.Services.AddScoped<IStockRequest,StockRequestRepository>();
builder.Services.AddScoped<ISalesHistory, SalesHistoryRepository>();




builder.Services.AddDbContext<ShelfSenseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



 
  

// 🔄 Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

 

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
