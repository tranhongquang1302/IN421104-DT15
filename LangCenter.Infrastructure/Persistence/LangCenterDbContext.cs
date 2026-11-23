using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Persistence;

public class LangCenterDbContext(DbContextOptions<LangCenterDbContext> options) : DbContext(options) { }
