using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace censudex.src.Services
{
    /// <summary>
    /// BCrypt password hasher implementation for ASP.NET Identity.
    /// </summary>
    public class BCryptPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        /// <summary>
        /// Hashes the given password using BCrypt.
        /// </summary>
        /// <param name="user">The user for whom the password is being hashed.</param>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The hashed password.</returns>
        public string HashPassword(TUser user, string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        /// <summary>
        /// Verifies the given hashed password against the provided plain text password.
        /// </summary>
        /// <param name="user">The user whose password is being verified.</param>
        /// <param name="hashedPassword">The hashed password stored for the user.</param>
        /// <param name="providedPassword">The plain text password provided for verification.</param>
        /// <returns>A result indicating whether the verification was successful or failed.</returns>
        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            bool isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
            return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }
    }
}