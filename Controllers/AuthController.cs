using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LogiTrack.Controllers;

public class RegisterDto
{
  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  public string Password { get; set; }
}

public class LoginDto
{
  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  public string Password { get; set; }
}

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly SignInManager<ApplicationUser> _signInManager;
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly IConfiguration _configuration;

  public AuthController(UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration
  )
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _roleManager = roleManager;
    _configuration = configuration;
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
  {
    var user = new ApplicationUser { UserName = registerDto.Email, Email = registerDto.Email };
    var result = await _userManager.CreateAsync(user, registerDto.Password);

    if (!result.Succeeded)
      return BadRequest(result.Errors);

    return CreatedAtAction(nameof(Register), new { email = user.Email }, "User registered successfully!");
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
  {
    var user = await _userManager.FindByEmailAsync(loginDto.Email);
    if (user == null)
      return Unauthorized("Email does not exist.");
    var checkPasswordResult = await _userManager.CheckPasswordAsync(user, loginDto.Password);
    if (!checkPasswordResult)
      return Unauthorized("Invalid login attempt");

    var token = GenerateJwtToken(user);

    return Ok(new { token });
  }

  [Authorize]
  [HttpGet("seed-roles")]
  public async Task<IActionResult> SeedRoles()
  {
    var roleMappings = new Dictionary<string, string>
    {
        { "manager@logitrack.com", "Manager" },
        { "staff@logitrack.com", "Staff" }
    };

    foreach (var role in roleMappings.Values.Distinct())
    {
      if (!await _roleManager.RoleExistsAsync(role))
      {
        await _roleManager.CreateAsync(new IdentityRole(role));
      }
    }

    foreach (var kvp in roleMappings)
    {
      var email = kvp.Key;
      var role = kvp.Value;

      var user = await _userManager.FindByEmailAsync(email);
      if (user != null && !await _userManager.IsInRoleAsync(user, role))
      {
        Console.WriteLine($"Assigning role '{role}' to '{email}'");
        await _userManager.AddToRoleAsync(user, role);
      }
    }

    return Ok("Roles seeded.");
  }

  private async Task<string> GenerateJwtToken(ApplicationUser user)
  {
    var roles = await _userManager.GetRolesAsync(user);
    var claims = new List<Claim>
    {
      new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new Claim(ClaimTypes.NameIdentifier, user.Id)
    };
    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: _configuration["Jwt:Issuer"],
      audience: _configuration["Jwt:Audience"],
      claims: claims,
      expires: DateTime.UtcNow.AddHours(1),
      signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
