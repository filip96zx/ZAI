using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Przychodnia.Interfaces;
using Przychodnia.Transfer.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Przychodnia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser(ListQuery query)
        {
            var result = await _userService.ListUserAsync(query);
            return Ok(result.Value);
        }

        [HttpGet("{id}"), Authorize]
        [Produces(typeof(UserDTO))]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (result.Success == false)
            {
                foreach (var error in result.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }
            var viewModel = new UserDTO()
            {
                Id = result.Value.Id,
                Name = result.Value.Name,
                Surname = result.Value.Surname,
                DateOfBirth = result.Value.DateOfBirth,
                Email = result.Value.Email,
                PhoneNumber = result.Value.PhoneNumber
            };
            return Ok(viewModel);
        }

        [HttpPost]
        [Produces(typeof(UserDTO))]
        public async Task<IActionResult> Create(CreateUserCommand command)
        {
            var userIdResult = await _userService.CreateUserAsync(command);
            if (!userIdResult.Success)
            {
                foreach (var error in userIdResult.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }
            var userResult = await _userService.GetUserByIdAsync(userIdResult.Value);
            if (!userResult.Success)
            {
                foreach (var error in userResult.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }
            var userDTO = new UserDTO()
            {
                Id = userResult.Value.Id,
                Name = userResult.Value.Name,
                Surname = userResult.Value.Surname,
                DateOfBirth = userResult.Value.DateOfBirth,
                Email = userResult.Value.Email,
                PhoneNumber = userResult.Value.PhoneNumber
            };

            return CreatedAtAction(nameof(GetById),
                new { id = userDTO.Id }, userDTO);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(CreateUserCommand command)
        {
            var userIdResult = await _userService.RegisterUserAsync(command);
            if (!userIdResult.Success)
            {
                foreach (var error in userIdResult.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }

            return Ok();
        }


        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, UpdateUserCommand command)
        {

            var updateResult = await _userService.UpdateUserAsync(id, command);
            if (updateResult.Success == false)
            {
                foreach (var error in updateResult.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleteResult = await _userService.DeleteUserAsync(id);
            if (deleteResult.Success == false)
            {
                foreach (var error in deleteResult.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }

                return BadRequest(ModelState);
            }

            return NoContent();
        }

        [HttpGet("ConfirmEmail/{id}/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(int id, string code)
        {
            var result = await _userService.ConfirmEmailAsync(id, code);
            if (result.Success == false)
            {
                foreach (var error in result.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }

            return Ok();

        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _userService.SignOutUserAsync();
            return Ok();
        }

        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotUserPasswordCommand command)
        {
            var result = await _userService.SendEmailResetPasswordAsync(command);
            if (!result.Success)
            {
                foreach (var error in result.ErrorMessages)
                {
                    ModelState.AddModelError(error.PropertyName, error.Message);
                }
                return BadRequest(ModelState);
            }

            return Ok();
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        [Produces(typeof(ResetUserPasswordCommand))]
        public async Task<IActionResult> ResetPassword(ResetUserPasswordCommand command)
        {
            var result = await _userService.ResetPasswordAsync(command);
            if (result.Success != false) return Ok();
            foreach (var error in result.ErrorMessages)
            {
                ModelState.AddModelError(error.PropertyName, error.Message);
            }
            return BadRequest(ModelState);

        }


    }
}
