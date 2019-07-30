using Microsoft.Extensions.DependencyInjection;
using RESTar.ContentTypeProviders;

namespace RESTar.Excel
{
    public static class ExtensionMethods
    {
            public static IServiceCollection AddExcelContentProvider(this IServiceCollection services)
            {
                services.Add(new ServiceDescriptor(typeof(IContentTypeProvider), new ExcelProvider()));
                return services;
            } 
    }
}