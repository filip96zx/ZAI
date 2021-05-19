using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;


        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("CreateRole")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateRole(CreateRoleCommand command)
        {
            var result = await _userService.CreateRoleAsync(command);
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
        [HttpPost("DeleteRole")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteRole(CreateRoleCommand command)
        {
            var result = await _userService.DeleteRoleAsync(command);
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
        [HttpPost("AddRole")]
        [AllowAnonymous]
        public async Task<IActionResult> AddRole(AddRoleCommand command)
        {
            var result = await _userService.AddRoleToUserAsync(command);
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
        [HttpPost("RemoveRole")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveRole(AddRoleCommand command)
        {
            var result = await _userService.RemoveRoleFromUserAsync(command);
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



    }
}
