using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShelfSense.Application.DTOs.Auth;
using ShelfSense.Application.Services.Auth;
using ShelfSense.Domain.Identity;
using System.Security.Claims;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtTokenService _jwtService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService jwtService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }

        // ---------------- REGISTER ----------------

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { message = "Email already registered." });

            // Validate role
            if (!await _roleManager.RoleExistsAsync(dto.Role))
                return BadRequest(new { message = $"Role '{dto.Role}' does not exist." });

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.Phone,
                RoleType = dto.Role,   // ✅ take role from DTO
                Name = dto.Name        // ✅ store full name if you added this property
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = "User registration failed.", errors = result.Errors });

            // ✅ assign the chosen role
            await _userManager.AddToRoleAsync(user, dto.Role);

            return Ok(new
            {
                message = $"User registered successfully with role '{dto.Role}'. Please log in to continue."
            });
        }




        // ---------------- LOGIN ----------------
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            // Check if user is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                return Unauthorized(new
                {
                    message = "Account temporarily locked due to multiple failed login attempts. Please try again later."
                });
            }

            // Check password
            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user); // Increment failure count

                int attemptsLeft = 3 - await _userManager.GetAccessFailedCountAsync(user);
                return Unauthorized(new
                {
                    message = $"Invalid credentials. {attemptsLeft} attempt(s) remaining before lockout."
                });
            }

            // Reset failure count on successful login
            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                message = $"Welcome back, {user.UserName}!",
                token,
                refreshToken
            });
        }




        // ---------------- REFRESH TOKEN ----------------
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenRefreshDto dto)
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(dto.AccessToken);
            var email = principal?.Identity?.Name;

            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { error = "Invalid access token" });

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized(new { error = "Invalid or expired refresh token" });

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtService.GenerateToken(user, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Ok(new { token = newAccessToken, refreshToken = newRefreshToken });
        }

        // ---------------- WHOAMI ----------------
        [Authorize]
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            var username = User.Identity?.Name ?? "Unknown";
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new
            {
                message = $"Welcome {username}",
                userId,
                username,
                roles
            });
        }
    }
}
