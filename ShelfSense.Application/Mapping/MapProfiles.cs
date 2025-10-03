using AutoMapper;
using ShelfSense.Application.DTOs;
using ShelfSense.Domain.Entities;
//using ShelfSense.Application.DTOs;

namespace ShelfSense.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Category mappings
            CreateMap<CategoryDto.CategoryCreateRequest, Category>();
            CreateMap<Category, CategoryDto.CategoryResponse>();

            // Product mappings
            CreateMap<ProductDto.ProductCreateRequest, Product>();
            CreateMap<Product, ProductDto.ProductResponse>();

            //Store mappings
            CreateMap<StoreCreateRequest, Store>();
            CreateMap<Store, StoreResponse>();

            // Shelf mappings
            CreateMap<ShelfCreateRequest, Shelf>();
            CreateMap<Shelf, ShelfResponse>();

            //ProductShelf mappings

            CreateMap<ProductShelfCreateRequest, ProductShelf>();
            CreateMap<ProductShelf, ProductShelfResponse>();

            //ReplinishmentAlert mappings
            CreateMap<ReplenishmentAlertCreateRequest, ReplenishmentAlert>();
            CreateMap<ReplenishmentAlert, ReplenishmentAlertResponse>();

            //Staff mappings
            CreateMap<StaffCreateRequest, Staff>();
            CreateMap<Staff, StaffResponse>();

            //RestockTask mappings
            CreateMap<RestockTaskCreateRequest, RestockTask>();
            CreateMap<RestockTask, RestockTaskResponse>();

            //InventoryRecord mappings
            CreateMap<InventoryReportCreateRequest, InventoryReport>();
            CreateMap<InventoryReport, InventoryReportResponse>();

            //StockRequest mappings
            CreateMap<StockRequestCreateRequest, StockRequest>();
            CreateMap<StockRequest, StockRequestResponse>();

            //SalesHistory mappings
            CreateMap<SalesHistoryCreateRequest, SalesHistory>();
            CreateMap<SalesHistory, SalesHistoryResponse>();

            // 🌟 NEW MAPPING for Archived Alerts 🌟
            CreateMap<ClosedReplenishmentAlert, ClosedAlertResponse>();


        }
    }
}
