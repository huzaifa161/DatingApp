using API.DTOs;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly ITokenService _tokenService;
        public IMapper _mapper { get; }
        private readonly UserManager<AppUser> _userManager;
        public SignInManager<AppUser> _signinManager { get; }
        public AccountController(UserManager<AppUser> userManager,SignInManager<AppUser> signinManager, ITokenService tokenService,IMapper mapper)
        {
            _signinManager  = signinManager;
            _userManager = userManager;
            _mapper = mapper;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {

            if(await UserExists(registerDto.Username)) return BadRequest("Username is Taken");

            var user = _mapper.Map<AppUser>(registerDto);

            user.UserName = registerDto.Username.ToLower();

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if(!result.Succeeded) return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user,"Member");
            
            if(!roleResult.Succeeded) return BadRequest(roleResult.Errors);

            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.createToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.Users.Include(x => x.Photos).SingleOrDefaultAsync(x =>x.UserName == loginDto.Username.ToLower());
            
            if(user == null) return Unauthorized("Invalid UserName");

            var result = await _signinManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
    
            if(!result.Succeeded) return Unauthorized();

            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.createToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }
        private async Task<bool> UserExists(string Username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == Username.ToLower());
        }
    }
}