using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using my_books.Data.Model;
using my_books.Data;
using my_books.Data.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace my_books.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AuthenticationController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IConfiguration configuration,
            TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _tokenValidationParameters = tokenValidationParameters;
        }
        [HttpPost("register-user")]
        public async Task<IActionResult> Register([FromBody] RegisterVM payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please, provide all required fields");
            }

            var userExists = await _userManager.FindByEmailAsync(payload.Email);

            if (userExists != null)
            {
                return BadRequest($"User {payload.Email} already exists");
            }

            ApplicationUser newUser = new ApplicationUser()
            {
                Email = payload.Email,
                UserName = payload.UserName,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(newUser, payload.Password);

            if (!result.Succeeded)
            {
                return BadRequest("User could not be created!");
            }

            switch (payload.Role)
            {
                case "Admin":
                    await _userManager.AddToRoleAsync(newUser, UserRoles.Admin);
                    break;
                case "Publisher":
                    await _userManager.AddToRoleAsync(newUser, UserRoles.Publisher);
                    break;
                case "Author":
                    await _userManager.AddToRoleAsync(newUser, UserRoles.Author);
                    break;
                default:
                    await _userManager.AddToRoleAsync(newUser, UserRoles.User);
                    break;
            }

            return Created(nameof(Register), $"User {payload.Email} created");
        }
        [HttpPost("login-user")]
        public async Task<IActionResult> Login([FromBody] LoginVM payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please, provide all required fields");
            }

            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, payload.Password))
            {
                var tokenValue = await GenerateJwtTokenAsync(user,"");

                return Ok(tokenValue);
            }

            return Unauthorized();
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestVM payload)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please, provide all required fields");
            }

            try
            {
                var result = await VerifyAndGenerateToken(payload);
                if (result == null)
                {
                    return BadRequest("Invalid Token");
                }
                return Ok(result);
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }

            return Unauthorized();
        }

        private async Task<AuthResultVM> VerifyAndGenerateToken(TokenRequestVM payload)
        {
            // check 1-Jwt token format 
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var tokenInVerification = jwtTokenHandler.ValidateToken(payload.Token, _tokenValidationParameters, out var validatedToken);
            // check 2- Encription Algorythim
            if (validatedToken is JwtSecurityToken jwtSecurityToken) 
            {
                var result = string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                if (result == false) return null;
            }
            // check 3- Validate Expiry date
            var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDate=UnixTimeStampToDateTimeInUTC(utcExpiryDate);
            if (expiryDate > DateTime.UtcNow) throw new Exception("Token Has Not Expired yet!");

            // check 4- Refresh Token Exist in DB
            var dbRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync();
            if (dbRefreshToken ==null ) throw new Exception("Token does not exist in the database!");
            else
            {
                // check 5-validate id
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if (dbRefreshToken.JwtId != jti) throw new Exception("Token does not match!");

                if (dbRefreshToken.DateExpire > DateTime.UtcNow) throw new Exception("Your Refresh Token Has Expired yet, Please re-authenticate it.");

                if (dbRefreshToken.IsRevoked) throw new Exception("Refresh Token has revoked!");

                var dbUser= await _userManager.FindByIdAsync(dbRefreshToken.UserId);
                var newTokenResponce = GenerateJwtTokenAsync(dbUser, payload.RefreshToken);

                return await newTokenResponce;
            }
        }

        private DateTime UnixTimeStampToDateTimeInUTC(long unixTimestamp)
        {
            var dateTimeVal=new DateTime(1970,1,1,0,0,0,0,DateTimeKind.Utc);
            return dateTimeVal.AddSeconds(unixTimestamp);
        }
        private async Task<AuthResultVM> GenerateJwtTokenAsync(ApplicationUser user,string existingRefreshToken)
        {
            var authClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            //Add User Roles
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }


            var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.UtcNow.AddMinutes(10), // 5 - 10mins
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = new RefreshToken();
            if (string.IsNullOrEmpty(existingRefreshToken))
            {
                refreshToken = new RefreshToken()
                {
                    JwtId = token.Id,
                    IsRevoked = false,
                    UserId = user.Id,
                    DateAdded = DateTime.UtcNow,
                    DateExpire = DateTime.UtcNow.AddMonths(6),
                    Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString()
                };
                await _context.RefreshTokens.AddAsync(refreshToken);
                await _context.SaveChangesAsync();
            }
            

            var response = new AuthResultVM()
            {
                Token = jwtToken,
                RefreshToken = (string.IsNullOrEmpty(existingRefreshToken))? refreshToken.Token: existingRefreshToken,
                ExpiresAt = token.ValidTo
            };

            return response;
        }
    }
}
