using ICFAccounting.Data.Authentication;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace ICFAccounting.Api.Authentication;

/// <summary>Provisions credentials out of band without exposing registration or passwords over HTTP.</summary>
public static class UserAccountCommand
{
    public static async Task<int> RunAsync(string operation, IServiceProvider services)
    {
        if (Console.IsInputRedirected || operation is not ("create" or "set-password"))
        {
            Console.Error.WriteLine("Use an interactive terminal: users create | users set-password. Passwords must not be passed as arguments.");
            return 1;
        }

        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
            !MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            Console.Error.WriteLine("A valid email of at most 256 characters is required.");
            return 1;
        }

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
        UserAccount account;
        if (operation == "create")
        {
            Console.Write("Display name (optional): ");
            var name = Console.ReadLine()?.Trim();
            if (name?.Length > 200)
            {
                Console.Error.WriteLine("Display name must not exceed 200 characters.");
                return 1;
            }
            Console.Write("Profile image HTTPS URL (optional): ");
            var imageUrl = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(imageUrl) &&
                (imageUrl.Length > 2048 || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var image) ||
                 image.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(image.UserInfo)))
            {
                Console.Error.WriteLine("Profile image must be an HTTPS URL without embedded credentials and at most 2048 characters.");
                return 1;
            }
            account = new UserAccount
            {
                UserName = email, Email = email, DisplayName = string.IsNullOrEmpty(name) ? null : name,
                ProfileImageUrl = string.IsNullOrEmpty(imageUrl) ? null : imageUrl
            };
        }
        else
        {
            var existing = await users.FindByEmailAsync(email);
            if (existing is null)
            {
                Console.Error.WriteLine("Account not found.");
                return 1;
            }
            account = existing;
        }

        var password = ReadPassword("Password (12+ characters, upper/lowercase, digit, symbol): ");
        if (password is null)
        {
            Console.Error.WriteLine("Password entry cancelled.");
            return 1;
        }
        var confirmation = ReadPassword("Confirm password: ");
        if (password != confirmation)
        {
            Console.Error.WriteLine("Password entry cancelled or passwords do not match.");
            return 1;
        }

        var result = operation == "create"
            ? await users.CreateAsync(account, password)
            : await users.ResetPasswordAsync(account, await users.GeneratePasswordResetTokenAsync(account), password);
        if (!result.Succeeded)
        {
            // Identity error codes describe the failure without printing account data.
            Console.Error.WriteLine($"Account operation failed: {string.Join(", ", result.Errors.Select(error => error.Code))}");
            return 1;
        }
        Console.WriteLine("Account operation completed.");
        return 0;
    }

    private static string? ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null;
            }
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                if (password.Length >= 1024)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("Password must not exceed 1024 characters.");
                    return null;
                }
                password.Append(key.KeyChar);
            }
        }
    }
}
