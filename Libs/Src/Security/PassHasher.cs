using System;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;

namespace Nox.Security;

public class PassHasher
{
    private static readonly int _iterCount = 100000;

    public static string HashPassword(string password, string salt)
        => HashPassword(password, Convert.FromBase64String(salt));

    public static string HashPassword(string password, byte[] salt)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password));
        }

        if (salt == null || salt.Length < 128 / 8)
        {
            throw new ArgumentException("Salt must be at least 128 bits (16 bytes).", nameof(salt));
        }

        byte[] subkey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA512, _iterCount, 256 / 8);
        return Convert.ToBase64String(subkey);
    }

    public static bool VerifyHashedPassword(string hashedPassword, string providedPassword, string salt)
        => VerifyHashedPassword(hashedPassword, providedPassword, Convert.FromBase64String(salt));

    public static bool VerifyHashedPassword(string hashedPassword, string providedPassword, byte[] salt)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword) || salt == null)
        {
            return false;
        }

        byte[] decodedHashedPassword = Convert.FromBase64String(hashedPassword);
        byte[] actualSubkey = KeyDerivation.Pbkdf2(providedPassword, salt, KeyDerivationPrf.HMACSHA512, _iterCount, decodedHashedPassword.Length);

        return ByteArraysEqual(actualSubkey, decodedHashedPassword);
    }

    public static byte[] GenerateSalt(int saltSize = 128 / 8)
    {
        if (saltSize < 128 / 8)
        {
            throw new ArgumentException("Salt size must be at least 128 bits (16 bytes).", nameof(saltSize));
        }

        byte[] salt = new byte[saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        return salt;
    }

    private static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a == null && b == null)
        {
            return true;
        }
        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }
        var areSame = true;
        for (var i = 0; i < a.Length; i++)
        {
            areSame &= (a[i] == b[i]);
        }
        return areSame;
    }
}