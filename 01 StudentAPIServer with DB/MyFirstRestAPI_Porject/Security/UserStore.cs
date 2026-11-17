using BCrypt.Net;
using System.Security.Cryptography;

namespace StudentApi.Security;

public sealed class AppUser
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string Role { get; init; } = "Student";
    public int? StudentId { get; init; }
}

public sealed class RefreshSession
{
    public required string Token { get; init; }
    public required int UserId { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public bool Revoked { get; set; }
}

public sealed class UserStore
{
    private readonly object _lock = new();
    private readonly List<AppUser> _users =
    [
        new AppUser
        {
            Id = 1,
            Name = "Student API Admin",
            Email = "admin@studentapi.local",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Admin123!", workFactor: 12),
            Role = "Admin",
            StudentId = null
        },
        new AppUser
        {
            Id = 2,
            Name = "Alia Maher",
            Email = "alia.maher@student.com",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Student123!", workFactor: 12),
            Role = "Student",
            StudentId = 1
        }
    ];

    private readonly Dictionary<string, RefreshSession> _refreshSessions = new(StringComparer.Ordinal);

    public IReadOnlyList<AppUser> Users
    {
        get
        {
            lock (_lock)
            {
                return _users.ToList();
            }
        }
    }

    public AppUser? FindByEmail(string email)
    {
        lock (_lock)
        {
            return _users.FirstOrDefault(user =>
                user.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public AppUser? FindById(int id)
    {
        lock (_lock)
        {
            return _users.FirstOrDefault(user => user.Id == id);
        }
    }

    public AppUser? Register(string name, string email, string password)
    {
        lock (_lock)
        {
            if (_users.Any(user => user.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var user = new AppUser
            {
                Id = _users.Count == 0 ? 1 : _users.Max(item => item.Id) + 1,
                Name = name.Trim(),
                Email = email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12),
                Role = "Student",
                StudentId = null
            };

            _users.Add(user);
            return user;
        }
    }

    public string CreateRefreshSession(int userId, TimeSpan lifetime)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var session = new RefreshSession
        {
            Token = token,
            UserId = userId,
            ExpiresAtUtc = DateTime.UtcNow.Add(lifetime)
        };

        lock (_lock)
        {
            _refreshSessions[token] = session;
        }

        return token;
    }

    public RefreshSession? RotateRefreshSession(string token)
    {
        lock (_lock)
        {
            if (!_refreshSessions.TryGetValue(token, out var session))
            {
                return null;
            }

            if (session.Revoked || session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return null;
            }

            session.Revoked = true;
            return session;
        }
    }

    public void RevokeRefreshSession(string token)
    {
        lock (_lock)
        {
            if (_refreshSessions.TryGetValue(token, out var session))
            {
                session.Revoked = true;
            }
        }
    }
}
