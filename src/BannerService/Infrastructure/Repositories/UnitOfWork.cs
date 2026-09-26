namespace BannerService.Infrastructure.Repositories;

using Domain.Interfaces;
using Data;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IShopContextAccessor _shopContextAccessor;
    private IDbContextTransaction? _transaction;
    private IBannerRepository? _bannerRepository;
    private IComponentRepository? _componentRepository;

    public UnitOfWork(ApplicationDbContext context, IShopContextAccessor shopContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _shopContextAccessor = shopContextAccessor ?? throw new ArgumentNullException(nameof(shopContextAccessor));
    }

    public IBannerRepository BannerRepository
    {
        get
        {
            _bannerRepository ??= new BannerRepository(_context, _shopContextAccessor);
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
}
