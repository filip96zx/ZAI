using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Przychodnia.Interfaces;
using Przychodnia.Models;
using Przychodnia.Npgsql;
using Przychodnia.Transfer.PagedList;
using Przychodnia.Transfer.Token;
using Przychodnia.Transfer.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenis.Core;

namespace Przychodnia.Services
{
    public class UserService : IUserService
    {
        private UserManager<User> UserManager { get; set; }
        private RoleManager<Role> RoleManager { get; set; }
        private DatabaseContext DataContext { get; set; }
        private SignInManager<User> SigInManager { get; set; }
        private IEmailSender EmailSender { get; set; }
        private ITokenService TokenService { get; set; }
        private IConfiguration Config;

        public UserService(UserManager<User> userManager,
            RoleManager<Role> roleManager,
            DatabaseContext dataContext,
            SignInManager<User> sigInManager,
            ITokenService tokenService,
            IEmailSender emailSender,
            IConfiguration config)
        {
            UserManager = userManager;
            RoleManager = roleManager;
            DataContext = dataContext;
            SigInManager = sigInManager;
            TokenService = tokenService;
            EmailSender = emailSender;
            Config = config;
        }

        public async Task<Result<User>> GetUserByIdAsync(int id)
        {
            var user = await UserManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return Result.Error<User>($"User with that id {id} doesn't exist");
            }
            return Result.Ok(user);
        }

        public async Task<Result<int>> CreateUserAsync(CreateUserCommand command)
        {
            if (command == null)
            {
                return Result.Error<int>("Create user command can`t be null");
            }

            var user = new User()
            {
                Name = command.Name,
                Surname = command.Surname,
                DateOfBirth = command.DateOfBirth,
                Email = command.Email,
                PhoneNumber = command.PhoneNumber,
                Country = command.Country,
                City = command.City,
                Gender = command.Gender,
                UserName = command.UserName
            };
            var userResult = await UserManager.CreateAsync(user, command.Password);
            if (userResult.Succeeded)
            {
                var userTmp = await UserManager.FindByNameAsync(user.UserName);
                return Result.Ok(userTmp.Id);
            }
            else
            {
                return Result.Error<int>(identityErrors: userResult.Errors);
            }

        }



        public async Task<Result<int>> UpdateUserAsync(int id, UpdateUserCommand command)
        {
            if (command == null)
            {
                return Result.Error<int>("Update user command can`t be null");
            }
            var user = await UserManager.FindByIdAsync(id.ToString());
            user.City = command.City;
            user.Country = command.Country;
            user.DateOfBirth = command.DateOfBirth;
            user.Gender = command.Gender;
            user.Name = command.Name;
            user.Surname = command.Surname;
            var result = await UserManager.UpdateAsync(user);
            DataContext.SaveChanges();
            return !result.Succeeded ? Result.Error<int>(result.Errors) : Result.Ok(user.Id);
        }

        public async Task<Result<TokenDTO>> LoginUserAsync(LoginUserCommand loginUserCommand)
        {
            var loginResult = await SigInManager.PasswordSignInAsync(loginUserCommand.Login, loginUserCommand.Password,
               false, false);

            if (loginResult.Succeeded == false)
            {
                return Result.Error<TokenDTO>($"Data is incorrect");
            }

            var userResult = UserManager.FindByNameAsync(loginUserCommand.Login);

            if (userResult == null)
            {
                return Result.Error<TokenDTO>($"Data is incorrect");
            }

            var roles = UserManager.GetRolesAsync(userResult.Result);
            var token = TokenService.CreateToken(userResult.Result, roles.Result);

            return Result.Ok(token);
        }

        public async Task<Result<User>> DeleteUserAsync(int id)
        {
            var user = await UserManager.FindByIdAsync(id.ToString());
            var deleteResult = await UserManager.DeleteAsync(user);
            if (deleteResult.Succeeded == false)
            {
                return Result.Error<User>(deleteResult.Errors);
            }

            return Result.Ok(user);
        }

        public async Task<Result<PagedListDTO<UserDTO>>> ListUserAsync(ListQuery query)
        {
            var viewModel = new PagedListDTO<UserDTO>()
            {
                PageSize = query.PageSize,
                PageIndex = query.PageIndex,
                TotalCount = DataContext.Users.Count()
            };
            var users = await DataContext.Users.Select(x => new UserDTO
            {
                Id = x.Id,
                Name = x.Name,
                Surname = x.Surname,
                DateOfBirth = x.DateOfBirth,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber
            })
                .Skip(query.PageSize * query.PageIndex)
                .Take(query.PageSize)
                .ToListAsync();

            viewModel.Item = users;
            return Result.Ok(viewModel);
        }

        public async Task<Result<int>> RegisterUserAsync(CreateUserCommand command)
        {
            var createUserResult = await CreateUserAsync(command);
            if (!createUserResult.Success)
            {
                return createUserResult;
            }
            var user = await UserManager.FindByIdAsync(createUserResult.Value.ToString());
            var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
            var codeEncoded = WebEncoders.Base64UrlEncode(tokenGeneratedBytes);

            var url = $"{Config["Email:ConfirmEmail"]}/{createUserResult.Value}/{codeEncoded}";
            var bodyBuilder = new BodyBuilder { HtmlBody = $"<html>Aby potwierdzić rejestracje <a href={url}>kliknij tu!</a>.</html>" };
            var message = new MimeMessage()
            {
                Body = bodyBuilder.ToMessageBody()
            };
            await EmailSender.SendEmailAsync(command.Email, "Potwierdzenie rejestracji", message);

            return Result.Ok(createUserResult.Value);
        }

        public async Task<Result<string>> ConfirmEmailAsync(int userId, string code)
        {
            var userResult = await GetUserByIdAsync(userId);
            if (!userResult.Success)
            {
                return Result.Error<string>(userResult.ErrorMessages.First().Message);
            }
            var codeDecodedBytes = WebEncoders.Base64UrlDecode(code);
            var codeDecoded = Encoding.UTF8.GetString(codeDecodedBytes);

            var result = await UserManager.ConfirmEmailAsync(userResult.Value, codeDecoded);
            if (!result.Succeeded)
            {
                return Result.Error<string>(result.Errors);
            }
            return Result.Ok<string>(null);
        }

        public async Task SignOutUserAsync()
        {
            await SigInManager.SignOutAsync();
        }

        public async Task<Result<int>> SendEmailResetPasswordAsync(ForgotUserPasswordCommand command)
        {
            if (command.UserName == null)
            {
                return Result.Error<int>("user name is null");
            }

            var user = await UserManager.FindByNameAsync(command.UserName);
            var token = await UserManager.GeneratePasswordResetTokenAsync(user);
            var tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
            var codeEncoded = WebEncoders.Base64UrlEncode(tokenGeneratedBytes);

            var url = $"{Config["Email:ResetPassword"]}/{user.Id}/{codeEncoded}";
            var bodyBuilder = new BodyBuilder { HtmlBody = $"<html>Aby zmienić hasło <a href={url}>kliknij tu!</a>.</html>" };
            var message = new MimeMessage()
            {
                Body = bodyBuilder.ToMessageBody()
            };
            await EmailSender.SendEmailAsync(user.Email, "Zmiana hasła", message);
            return Result.Ok(user.Id);
        }

        public async Task<Result<string>> ResetPasswordAsync(ResetUserPasswordCommand command)
        {
            var userResult = await GetUserByIdAsync(command.UserId);
            if (!userResult.Success)
            {
                return Result.Error<string>(userResult.ErrorMessages.First().Message);
            }
            var codeDecodedBytes = WebEncoders.Base64UrlDecode(command.Code);
            var codeDecoded = Encoding.UTF8.GetString(codeDecodedBytes);

            var result = await UserManager.ResetPasswordAsync(userResult.Value, codeDecoded, command.NewPassword);
            if (!result.Succeeded)
            {
                return Result.Error<string>(result.Errors);
            }

            return Result.Ok<string>(null);
        }

        public async Task<Result<IdentityResult>> CreateRoleAsync(CreateRoleCommand command)
        {
            Role role = new Role
            {
                Name = command.RoleName
            };

            var result = await RoleManager.CreateAsync(role);

            return Result.Ok(result);
        }
        public async Task<Result<IdentityResult>> DeleteRoleAsync(CreateRoleCommand command)
        {
            Role role = new Role
            {
                Name = command.RoleName
            };

            var result = await RoleManager.DeleteAsync(role);

            return Result.Ok(result);
        }

        public async Task<Result<IdentityResult>> AddRoleToUserAsync(AddRoleCommand command)
        {
            var user = await UserManager.FindByIdAsync(command.UserId);

            var result = await UserManager.AddToRoleAsync(user, command.RoleName);

            return Result.Ok(result);

        }

        public async Task<Result<IdentityResult>> RemoveRoleFromUserAsync(AddRoleCommand command)
        {
            var user = await UserManager.FindByIdAsync(command.UserId);

            var result = await UserManager.RemoveFromRoleAsync(user, command.RoleName);
            return Result.Ok(result);
        }
    }
}
