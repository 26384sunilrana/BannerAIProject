namespace BannerService.Infrastructure.Security;

using System.Linq.Expressions;
using Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// Encrypts personal and payment values that were saved before field encryption was switched on. Run it once after turning encryption on
/// (<c>dotnet BannerService.dll --encrypt-existing</c>); it is safe to run again, a value already encrypted is left alone. Returns how
/// many values it encrypted.
/// </summary>
public class EncryptionBackfill
{
    private readonly ApplicationDbContext _context;

    public EncryptionBackfill(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> RunAsync()
    {
        if (!FieldEncryption.IsConfigured)
            throw new InvalidOperationException("Field encryption is not switched on, so there is nothing to encrypt with.");

        var count = 0;
        count += await EncryptAsync<User, string>(u => u.PhoneNumber, u => u.Id);
        count += await EncryptAsync<Shop, Guid>(s => s.PhoneNumber, s => s.Id);
        count += await EncryptAsync<Shop, Guid>(s => s.Address, s => s.Id);
        count += await EncryptAsync<Shop, Guid>(s => s.PostalCode, s => s.Id);
        count += await EncryptAsync<Subscription, Guid>(s => s.PaymentMethodId, s => s.Id);
        count += await EncryptAsync<Invoice, Guid>(i => i.PaymentReference, i => i.Id);
        return count;
    }

    private async Task<int> EncryptAsync<TEntity, TKey>(Expression<Func<TEntity, string?>> property, Expression<Func<TEntity, TKey>> key)
        where TEntity : class
    {
        var name = ((MemberExpression)property.Body).Member.Name;
        var keyName = ((MemberExpression)key.Body).Member.Name;
        var entityType = _context.Model.FindEntityType(typeof(TEntity))!;
        var table = entityType.GetTableName();

        List<TEntity> rows;
        if (_context.Database.IsRelational() && table != null)
        {
            // only the rows whose stored value is not already encrypted
            var column = entityType.FindProperty(name)!.GetColumnName(StoreObjectIdentifier.Table(table, entityType.GetSchema()));
            var keyColumn = entityType.FindProperty(keyName)!.GetColumnName(StoreObjectIdentifier.Table(table, entityType.GetSchema()));
            var ids = await _context.Database
                .SqlQueryRaw<string>($"SELECT CAST([{keyColumn}] AS nvarchar(64)) AS [Value] FROM [{table}] WHERE [{column}] IS NOT NULL AND [{column}] <> '' AND [{column}] NOT LIKE '{FieldEncryption.Prefix}%'")
                .ToListAsync();
            if (ids.Count == 0) return 0;

            var wanted = ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var compiledKey = key.Compile();
            rows = (await _context.Set<TEntity>().ToListAsync()).Where(e => wanted.Contains(compiledKey(e)!.ToString()!)).ToList();
        }
        else
        {
            rows = await _context.Set<TEntity>().ToListAsync();
        }

        var changed = 0;
        foreach (var row in rows)
        {
            var entry = _context.Entry(row);
            if (entry.Property(name).CurrentValue is not string { Length: > 0 }) continue;
            entry.Property(name).IsModified = true; // makes the converter write the encrypted form
            changed++;
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return changed;
    }
}
