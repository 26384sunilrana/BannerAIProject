using System.Security.Cryptography;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Security;
using BannerService.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BannerService.Application.Tests.Services;

// FieldEncryption is process-wide. These tests switch it on and leave it on: values written before
// that moment are plain and still readable, and later writes round-trip, so other tests are unaffected.
public class FieldEncryptionTests
{
    public FieldEncryptionTests()
    {
        FieldEncryption.Configure(new EphemeralDataProtectionProvider());
    }

    [Fact]
    public void Encrypt_ProducesPrefixedCiphertext_ThatDecryptsBack()
    {
        var stored = FieldEncryption.Encrypt("+91 98765 43210");

        Assert.StartsWith(FieldEncryption.Prefix, stored);
        Assert.DoesNotContain("98765", stored);
        Assert.Equal("+91 98765 43210", FieldEncryption.Decrypt(stored));
    }

    [Fact]
    public void Encrypt_IsNotDeterministic_AndNeverDoubleEncrypts()
    {
        var first = FieldEncryption.Encrypt("same");
        var second = FieldEncryption.Encrypt("same");

        Assert.NotEqual(first, second);
        Assert.Equal(first, FieldEncryption.Encrypt(first));
    }

    [Fact]
    public void Decrypt_PlainLegacyValue_IsReturnedAsIs()
    {
        Assert.Equal("0123456789", FieldEncryption.Decrypt("0123456789"));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var stored = FieldEncryption.Encrypt("secret");
        var tampered = stored[..^4] + "AAAA";

        Assert.ThrowsAny<CryptographicException>(() => FieldEncryption.Decrypt(tampered));
    }

    [Fact]
    public async Task DbContext_EncryptsConfiguredFields_AndReadsThemBack()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var shopId = Guid.NewGuid();

        await using (var write = new ApplicationDbContext(options))
        {
            write.Shops.Add(new Shop { Id = shopId, Name = "Olive Mart", PhoneNumber = "9876543210", Address = "12 MG Road" });
            await write.SaveChangesAsync();
        }

        await using (var read = new ApplicationDbContext(options))
        {
            // What the provider (the database) holds is not the plain text
            var entityType = read.Model.FindEntityType(typeof(Shop))!;
            var converter = entityType.FindProperty(nameof(Shop.PhoneNumber))!.GetValueConverter()!;
            var stored = (string)converter.ConvertToProvider("9876543210")!;
            Assert.StartsWith(FieldEncryption.Prefix, stored);

            var shop = await read.Shops.SingleAsync(s => s.Id == shopId);
            Assert.Equal("9876543210", shop.PhoneNumber);
            Assert.Equal("12 MG Road", shop.Address);
        }
    }
}
