using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;

namespace WebStore.Services.ConcreteServices
{
    public abstract class BaseService
    {
        protected readonly ApplicationDbContext DbContext;
        protected readonly ILogger Logger;
        protected readonly IMapper Mapper;

        public BaseService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
        {
            DbContext = dbContext;
            Logger = logger;
            Mapper = mapper;
        }

        protected static void EnsureRequiredValues(params (object? Value, string Description)[] fields)
        {
            var missingFields = fields.Where(f => f.Value == null).Select(f => f.Description).ToList();
            if (missingFields.Count > 0)
                throw new ArgumentException($"Brak wymaganych danych: {string.Join(", ", missingFields)}.");
        }
    }
}
