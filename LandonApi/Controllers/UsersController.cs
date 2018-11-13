using LandonApi.Models;
using LandonApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandonApi.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly PagingOptions _defaultPagingOptions;
        private readonly IAuthorizationService _authzService;

        public UsersController(
            IUserService userService,
            IOptions<PagingOptions> defaultPagingOptions,
            IAuthorizationService authorizationService)
        {
            _userService = userService;
            _defaultPagingOptions = defaultPagingOptions.Value;
            _authzService = authorizationService;
        }

        [HttpGet(Name = nameof(GetVisibleUsers))]
        public async Task<ActionResult<PagedCollection<User>>> GetVisibleUsers(
            [FromQuery] PagingOptions pagingOptions,
            [FromQuery] SortOptions<User, UserEntity> sortOptions,
            [FromQuery] SearchOptions<User, UserEntity> searchOptions)
        {
            pagingOptions.Offset = pagingOptions.Offset ?? _defaultPagingOptions.Offset;
            pagingOptions.Limit = pagingOptions.Limit ?? _defaultPagingOptions.Limit;

            var users = new PagedResults<User>();
            if(User.Identity.IsAuthenticated)
            {
                var canSeeEveryone  = await _authzService.AuthorizeAsync(User, "ViewAllUsersPolicy");
                if(canSeeEveryone.Succeeded)
                {
                    //Admin, can view all users
                    users = await _userService.GetUsersAsync(pagingOptions, sortOptions, searchOptions);
                }
                else
                {
                    var myself = await _userService.GetUserAsync(User);
                    users.Items = new[] { myself };
                    users.TotalSize = 1;
                }
            }

            var collection = PagedCollection<User>.Create(
                Link.ToCollection(nameof(GetVisibleUsers)),
                users.Items.ToArray(),
                users.TotalSize,
                pagingOptions);

            return collection;

            throw new NotImplementedException();
        }

        [Authorize]
        [ProducesResponseType(401)]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public Task<IActionResult> GetUserById(Guid userId)
        {
            // TODO is userId the current user's ID?
            // If so, return myself.
            // If not, only Admin roles should be able to view arbitrary users.
            throw new NotImplementedException();
        }

        [HttpPost(Name = nameof(RegisterUser))]
        [ProducesResponseType(400)]
        [ProducesResponseType(201)]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterForm registerForm)
        {
            var (succeded, message) = await _userService.CreateUserAsync(registerForm);
            if(succeded)
            {
                return Created(Url.Link(nameof(UserinfoController.Userinfo), null), null);
            }

            return BadRequest(new ApiError
            {
                Message = "Registration failed",
                Detail = message
            });
        }
    }
}
