using Microsoft.AspNetCore.Identity;
using OCRS.DTOs.SOA;
using OCRS.Models;

namespace OCRS.Services;

public interface IUserService
{
    Task<CurrentUserDto?> GetAsync(string userId);
    Task<(bool Succeeded, string Message, CurrentUserDto? User)> UpdateAsync(string userId, UpdateCurrentUserDto input);
}

public sealed class UserService(UserManager<ApplicationUser> users) : IUserService
{
    public async Task<CurrentUserDto?> GetAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return null;
        var roles = await users.GetRolesAsync(user);
        return Map(user, roles);
    }

    public async Task<(bool Succeeded, string Message, CurrentUserDto? User)> UpdateAsync(string userId, UpdateCurrentUserDto input)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return (false, "Không tìm thấy người dùng.", null);
        user.FullName = input.FullName.Trim();
        user.PhoneNumber = input.PhoneNumber?.Trim();
        user.DateOfBirth = input.DateOfBirth;
        user.Address = input.Address?.Trim();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return (false, string.Join("; ", result.Errors.Select(x => x.Description)), null);
        return (true, "Cập nhật hồ sơ thành công.", Map(user, await users.GetRolesAsync(user)));
    }

    private static CurrentUserDto Map(ApplicationUser user, IList<string> roles) =>
        new(user.Id, user.FullName, user.Email, user.PhoneNumber, user.DateOfBirth, user.Address, roles.ToArray());
}
