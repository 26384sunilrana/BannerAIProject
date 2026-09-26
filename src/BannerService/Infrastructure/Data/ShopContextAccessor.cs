namespace BannerService.Infrastructure.Data;

public interface IShopContextAccessor
{
    Guid ShopId { get; }
    void SetShopId(Guid shopId);
}

public class ShopContextAccessor : IShopContextAccessor
{
    private Guid _shopId;

    public Guid ShopId
    {
        get
        {
            if (_shopId == Guid.Empty)
                throw new InvalidOperationException("ShopId has not been set. Ensure ShopContextMiddleware is registered.");
            return _shopId;
        }
    }

    public void SetShopId(Guid shopId)
    {
        if (shopId == Guid.Empty)
            throw new ArgumentException("ShopId cannot be empty");
        _shopId = shopId;
    }
}
