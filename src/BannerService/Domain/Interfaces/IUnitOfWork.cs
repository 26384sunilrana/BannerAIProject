namespace BannerService.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IBannerRepository BannerRepository { get; }
    IComponentRepository ComponentRepository { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
