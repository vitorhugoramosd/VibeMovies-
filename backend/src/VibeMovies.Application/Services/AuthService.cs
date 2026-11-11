using System.Security.Cryptography;
using System.Text;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly Func<ApplicationUser, string> _jwtGenerator;

    public AuthService(IUserRepository userRepository, Func<ApplicationUser, string> jwtGenerator)
    {
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var existing = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException("Já existe uma conta cadastrada com este e-mail.");
        }

        var user = new ApplicationUser
        {
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(dto.Password),
            PreferredRegion = string.IsNullOrWhiteSpace(dto.PreferredRegion) ? "BR" : dto.PreferredRegion.Trim().ToUpperInvariant(),
            DefaultCity = string.IsNullOrWhiteSpace(dto.DefaultCity) ? "Joinville/SC" : dto.DefaultCity.Trim()
        };

        await _userRepository.CreateAsync(user, ct);
        var token = _jwtGenerator(user);

        return new AuthResponseDto(
            token,
            user.Id,
            user.Name,
            user.Email,
            user.PreferredRegion,
            user.DefaultCity
        );
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
        if (user is null || !VerifyPassword(dto.Password, user.PasswordHash))
        {
            return null;
        }

        var token = _jwtGenerator(user);
        return new AuthResponseDto(
            token,
            user.Id,
            user.Name,
            user.Email,
            user.PreferredRegion,
            user.DefaultCity
        );
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 50_000,
            HashAlgorithmName.SHA256,
            outputLength: 32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash)) return false;

        var parts = passwordHash.Split('.');
        if (parts.Length != 2) return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expectedHash = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false; // hash corrompido/malformado: trata como senha inválida em vez de estourar 500
        }

        if (salt.Length == 0 || expectedHash.Length != 32) return false;

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 50_000,
            HashAlgorithmName.SHA256,
            outputLength: 32);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
