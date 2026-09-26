namespace BannerService.Infrastructure.Repositories;

using Domain.Interfaces;
using Data;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private IBannerRepository? _bannerRepository;
    private IComponentRepository? _componentRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IBannerRepository BannerRepository
    {
        get
        {
            _bannerRepository ??= new BannerRepository(_context, GetShopContextAccessor());
            return _bannerRepository;
        }
    }

    public IComponentRepository ComponentRepository
    {
        get
        {
            _componentRepository ??= new ComponentRepository(_context);
            return _componentRepository;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
                await _transaction.CommitAsync();
        }
        catch
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
        finally
        {
            _transaction?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.DisposeAsync();

        await _context.DisposeAsync();
    }

    private IShopContextAccessor GetShopContextAccessor()
    {
        // This would be injected in real implementation
        throw new NotImplementedException("ShopContextAccessor must be injected via DI");
    }
}
