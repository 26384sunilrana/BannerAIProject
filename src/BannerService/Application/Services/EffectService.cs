<<<<<<< HEAD
namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;
    using Domain.ValueObjects;
    using DTOs;
=======
namespace BannerService.Application.Services;

using global::BannerService.Domain.Entities;
using global::BannerService.Domain.Interfaces;
using global::BannerService.Domain.Services;
using global::BannerService.Domain.ValueObjects;
using global::BannerService.Application.Dto;
>>>>>>> fdd9d7e4866c32af53c9f511abe3780d5ff25658

public interface IEffectService
{
    Task<EffectDto> ApplyEffectAsync(Guid bannerId, Guid componentId, Guid shopId,
        int effectType, Dictionary<string, object> parameters);
    Task RemoveEffectAsync(Guid bannerId, Guid componentId, Guid effectId, Guid shopId);
    Task<EffectDto> UpdateEffectAsync(Guid bannerId, Guid componentId, Guid effectId,
        Dictionary<string, object> newParameters, Guid shopId);
    Task<List<EffectDto>> GetComponentEffectsAsync(Guid bannerId, Guid componentId, Guid shopId);
}

public class EffectService : IEffectService
{
    private readonly IBannerRepository _bannerRepository;
    private readonly EffectValidator _validator;
    private readonly IUnitOfWork _unitOfWork;

    public EffectService(
        IBannerRepository bannerRepository,
        EffectValidator validator,
        IUnitOfWork unitOfWork)
    {
        _bannerRepository = bannerRepository;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }

    public async Task<EffectDto> ApplyEffectAsync(Guid bannerId, Guid componentId, Guid shopId,
        int effectType, Dictionary<string, object> parameters)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException("Banner not found");

        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new KeyNotFoundException("Component not found");

        var validationResult = ValidateEffectParameters(effectType, parameters);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);

        var effect = new Effect(effectType, parameters);
        component.AddEffect(effect);

        await _bannerRepository.UpdateAsync(banner);
        await _unitOfWork.CommitAsync();

        return MapToDto(effect);
    }

    public async Task RemoveEffectAsync(Guid bannerId, Guid componentId, Guid effectId, Guid shopId)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException("Banner not found");

        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new KeyNotFoundException("Component not found");

        var effect = component.Effects.FirstOrDefault(e => e.Id == effectId);
        if (effect == null)
            throw new KeyNotFoundException("Effect not found");

        component.RemoveEffect(effectId);

        await _bannerRepository.UpdateAsync(banner);
        await _unitOfWork.CommitAsync();
    }

    public async Task<EffectDto> UpdateEffectAsync(Guid bannerId, Guid componentId, Guid effectId,
        Dictionary<string, object> newParameters, Guid shopId)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException("Banner not found");

        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new KeyNotFoundException("Component not found");

        var effect = component.Effects.FirstOrDefault(e => e.Id == effectId);
        if (effect == null)
            throw new KeyNotFoundException("Effect not found");

        var validationResult = ValidateEffectParameters(effect.EffectType, newParameters);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);

        component.RemoveEffect(effectId);
        var newEffect = new Effect(effect.EffectType, newParameters);
        component.AddEffect(newEffect);

        await _bannerRepository.UpdateAsync(banner);
        await _unitOfWork.CommitAsync();

        return MapToDto(newEffect);
    }

    public async Task<List<EffectDto>> GetComponentEffectsAsync(Guid bannerId, Guid componentId, Guid shopId)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException("Banner not found");

        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new KeyNotFoundException("Component not found");

        return component.Effects.Select(MapToDto).ToList();
    }

    private ValidationResult ValidateEffectParameters(int effectType, Dictionary<string, object> parameters)
    {
        try
        {
            return effectType switch
            {
                1 => _validator.ValidateOpacity(
                    Convert.ToDecimal(parameters["opacity"]),
                    Convert.ToInt32(parameters["duration"]),
                    Convert.ToInt32(parameters["delay"])),
                2 => _validator.ValidateRotation(
                    Convert.ToDecimal(parameters["rotation"]),
                    Convert.ToInt32(parameters["duration"]),
                    Convert.ToInt32(parameters["delay"]),
                    parameters["timingCurve"].ToString() ?? ""),
                3 => _validator.ValidateScale(
                    Convert.ToDecimal(parameters["scaleX"]),
                    Convert.ToDecimal(parameters["scaleY"]),
                    Convert.ToInt32(parameters["duration"]),
                    Convert.ToInt32(parameters["delay"]),
                    parameters["origin"].ToString() ?? ""),
                4 => _validator.ValidateBlur(
                    Convert.ToInt32(parameters["blurRadius"]),
                    Convert.ToInt32(parameters["duration"]),
                    Convert.ToInt32(parameters["delay"])),
                5 => _validator.ValidateAnimation(
                    parameters["animationType"].ToString() ?? "",
                    Convert.ToInt32(parameters["duration"]),
                    Convert.ToInt32(parameters["delay"]),
                    Convert.ToInt32(parameters["repeat"]),
                    Convert.ToInt32(parameters["repeatDelay"])),
                _ => ValidationResult.Failure("Invalid effect type")
            };
        }
        catch (KeyNotFoundException ex)
        {
            return ValidationResult.Failure($"Missing parameter: {ex.Message}");
        }
        catch (FormatException ex)
        {
            return ValidationResult.Failure($"Invalid parameter format: {ex.Message}");
        }
    }

    private EffectDto MapToDto(Effect effect) => new()
    {
        Id = effect.Id,
        EffectType = effect.EffectType,
        Parameters = effect.Parameters,
        IsEnabled = effect.IsEnabled,
        CreatedAt = effect.CreatedAt
    };
}
}
