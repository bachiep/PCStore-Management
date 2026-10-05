using System.Text.RegularExpressions;

namespace ComputerStore.BLL;

/// <summary>Lỗi nghiệp vụ, thông điệp hiển thị trực tiếp cho người dùng.</summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

/// <summary>Các hàm kiểm tra dữ liệu dùng chung.</summary>
public static partial class Validator
{
    [GeneratedRegex(@"^0\d{9,10}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_.]{3,50}$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9\-_/.]{3,50}$")]
    private static partial Regex SerialRegex();

    public static bool IsPhone(string? s) => !string.IsNullOrWhiteSpace(s) && PhoneRegex().IsMatch(s.Trim());
    public static bool IsEmail(string? s) => !string.IsNullOrWhiteSpace(s) && EmailRegex().IsMatch(s.Trim());
    public static bool IsUsername(string? s) => !string.IsNullOrWhiteSpace(s) && UsernameRegex().IsMatch(s);
    public static bool IsSerial(string? s) => !string.IsNullOrWhiteSpace(s) && SerialRegex().IsMatch(s.Trim());

    public static void Require(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessException($"{fieldName} không được để trống.");
    }

    public static void OptionalPhone(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !IsPhone(value))
            throw new BusinessException("Số điện thoại không hợp lệ (bắt đầu bằng 0, 10-11 chữ số).");
    }

    public static void OptionalEmail(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !IsEmail(value))
            throw new BusinessException("Email không hợp lệ.");
    }

    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
