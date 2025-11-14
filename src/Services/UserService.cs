using censudex.src.models;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using UserProto;

namespace Censudex.Services
{
    /// <summary>
    /// Service for managing users.
    /// </summary>
    public class UserService : UserProto.UserService.UserServiceBase
    {
        /// <summary>
        /// User manager for handling user-related operations.
        /// </summary>
        private readonly UserManager<AppUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager instance.</param>
        public UserService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Retrieves users based on the provided filters.
        /// </summary>
        /// <param name="request">The request containing filter criteria.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A response containing the filtered users.</returns>
        public override Task<UserProto.GetUserResponse> GetUser(UserProto.GetUserRequest request, ServerCallContext context)
        {
            var response = new UserProto.GetUserResponse();
            var users = _userManager.Users.ToList();

            if (!string.IsNullOrEmpty(request.Namefilter))
            {
                users = users.Where(u => u.FullName.Contains(request.Namefilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(request.Emailfilter))
            {
                users = users.Where(u => u.Email!.Contains(request.Emailfilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(request.Isactivefilter))
            {
                if (!bool.TryParse(request.Isactivefilter, out bool isActive))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "IsActiveFilter must be 'true' or 'false'."));
                }
                users = users.Where(u => u.IsActive == isActive).ToList();
            }

            if (!string.IsNullOrEmpty(request.Usernamefilter))
            {
                users = users.Where(u => u.UserName!.Contains(request.Usernamefilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var u in users)
            {
                response.Users.Add(new UserProto.User
                {
                    Id = u.Id.ToString(),
                    Fullname = u.FullName,
                    Email = u.Email ?? "",
                    Username = u.UserName ?? "",
                    IsActive = u.IsActive,
                    Birthdate = u.DateOfBirth.ToString("o"),
                    Address = u.Address ?? "",
                    Phone = u.PhoneNumber ?? "",
                    CreatedAt = u.CreatedAt.ToString("o")

                });
            }
            return Task.FromResult(response);
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception> <summary>

        public override Task<UserProto.CreateUserResponse> CreateUser(UserProto.CreateUserRequest request, ServerCallContext context)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Lastnames) || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Birthdate) || string.IsNullOrEmpty(request.Phone) || string.IsNullOrEmpty(request.Address))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Some required fields are missing."));
            }

            if (!request.Email.EndsWith("@censudex.cl"))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email must belong to the domain censudex.cl."));
            }

            if (!DateOnly.TryParse(request.Birthdate, out var dateOfBirth))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Birthdate is not a valid date."));
            }

            if (_userManager.Users.Any(u => u.Email == request.Email))
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "A user with the provided email already exists."));
            }

            var age = DateTime.UtcNow.Year - dateOfBirth.Year;
            if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-age)) age--;
            if (age < 18)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "User must be at least 18 years old."));
            }

            var phone = request.Phone;
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+569\d{8}$"))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Phone number must be a valid Chilean number starting with +56 and containing 9 digits."));
            }

            var user = new AppUser
            {
                FullName = $"{request.Name} {request.Lastnames}",
                Email = request.Email,
                UserName = request.Username,
                IsActive = true,
                DateOfBirth = dateOfBirth,
                Address = request.Address,
                PhoneNumber = request.Phone,
                CreatedAt = DateTime.UtcNow
            };

            var result = _userManager.CreateAsync(user, request.Password).Result;

            if (result.Succeeded)
            {
                return Task.FromResult(new UserProto.CreateUserResponse { Message = "User created successfully." });
            }
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description))));
        }

        /// <summary>
        /// Deletes a user by setting their status to inactive.
        /// </summary>
        /// <param name="request">The request containing the user ID to delete.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A response indicating the result of the delete operation.</returns>
        public override Task<UserProto.DeleteUserResponse> DeleteUser(UserProto.DeleteUserRequest request, ServerCallContext context)
        {
            var user = _userManager.FindByIdAsync(request.Id).Result;
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
            }
            user.IsActive = false;

            var result = _userManager.UpdateAsync(user).Result;
            if (result.Succeeded)
            {
                return Task.FromResult(new UserProto.DeleteUserResponse { Message = "User deleted successfully." });
            }
            throw new RpcException(new Status(StatusCode.Internal, "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description))));
        }
        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="request">The request containing the user ID.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A response containing the user details.</returns>

        public override Task<UserProto.GetUserIdResponse> GetUserById(UserProto.GetUserIdRequest request, ServerCallContext context)
        {
            var user = _userManager.FindByIdAsync(request.Id).Result;
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
            }

            var response = new UserProto.GetUserIdResponse
            {
                User = new UserProto.User
                {
                    Id = user.Id.ToString(),
                    Fullname = user.FullName,
                    Email = user.Email ?? "",
                    Username = user.UserName ?? "",
                    IsActive = user.IsActive,
                    Birthdate = user.DateOfBirth.ToString("o"),
                    Address = user.Address ?? "",
                    Phone = user.PhoneNumber ?? "",
                    CreatedAt = user.CreatedAt.ToString("o")
                }
            };

            return Task.FromResult(response);
        }

        /// <summary>
        /// Updates an existing user's details.
        /// </summary>
        /// <param name="request">The request containing updated user information.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A response indicating the result of the update operation.</returns>
        /// <exception cref="RpcException">Thrown when validation fails or update operation encounters an error.</exception>
        public override async Task<UserProto.UpdateUserResponse> UpdateUser(UserProto.UpdateUserRequest request, ServerCallContext context)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Id) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Lastnames) || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Birthdate) || string.IsNullOrEmpty(request.Phone) || string.IsNullOrEmpty(request.Address) || string.IsNullOrEmpty(request.Password))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Some required fields are missing."));
            }
            if (!request.Email.EndsWith("@censudex.cl"))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email must belong to the domain censudex.cl."));
            }

            if (!DateOnly.TryParse(request.Birthdate, out var dateOfBirth))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Birthdate is not a valid date."));
            }

            var phone = request.Phone;
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+569\d{8}$"))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Phone number must be a valid Chilean number starting with +56 and containing 9 digits."));
            }

            var user = _userManager.FindByIdAsync(request.Id).Result;
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
            }
            if (_userManager.Users.Any(u => u.Email == request.Email && request.Email != user.Email))
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "A user with the provided email already exists."));
            }
            var age = DateTime.UtcNow.Year - dateOfBirth.Year;
            if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-age)) age--;
            if (age < 18)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "User must be at least 18 years old."));
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passwordResult.Succeeded)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Failed to update password: " + string.Join(", ", passwordResult.Errors.Select(e => e.Description))));
                }
            }

            user.FullName = $"{request.Name} {request.Lastnames}";
            user.Email = request.Email;
            user.UserName = request.Username;
            user.DateOfBirth = dateOfBirth;
            user.Address = request.Address;
            user.PhoneNumber = request.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return new UserProto.UpdateUserResponse { Message = "User updated successfully." };
            }
            throw new RpcException(new Status(StatusCode.Internal, "Failed to update user: " + string.Join(", ", result.Errors.Select(e => e.Description))));

        }

        /// <summary>
        /// Authenticates a user using their email or username and password.
        /// </summary>
        /// <param name="request">The login request containing email/username and password.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A response containing the user's ID and role if authentication is successful.</returns>

        public override async Task<UserProto.LoginResponse> LoginUser(UserProto.LoginRequest request, ServerCallContext context)
        {
            var result = new UserProto.LoginResponse();
            var user = _userManager.Users.FirstOrDefault(u => u.Email == request.EmailOrUsername || u.UserName == request.EmailOrUsername);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
            }
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid password."));
            }
            result.Id = user.Id.ToString();
            result.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";
            return result;
        }

        
    }
}