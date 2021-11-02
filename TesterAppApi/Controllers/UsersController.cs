using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TesterAppApi.Models;
using TesterAppApi.Models.Entitles;
using Org.BouncyCastle.Crypto.Generators;

namespace TesterApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly TesterDbContext _testerDbContext;
        public UsersController(TesterDbContext context)
        {
            _testerDbContext = context;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Post([FromBody] User model)
        {
            if (model.Password != null)
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

            _testerDbContext.users.Add(model).ToString().ToLower();

            await _testerDbContext.SaveChangesAsync();

            return Ok(model);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Get()
        {
            var response = await Task.Run(() => _testerDbContext.users.ToList());

            return Ok(response);
        }

        [HttpGet]
        [Route("{userId}")]
        public async Task<ActionResult> GetById(int userId)
        {
            var response = await Task.Run(() => _testerDbContext.users.Where(x => x.Id == userId).FirstOrDefault());

            if (response == null)
                return NotFound();

            return Ok(response);
        }

        [HttpPut]
        public async Task<ActionResult> Put([FromBody] User model)
        {
            _testerDbContext.users.Update(model);

            await _testerDbContext.SaveChangesAsync();

            return Ok(model);
        }

        [HttpDelete]
        public async Task<ActionResult> Delete([FromBody] User model)
        {
            _testerDbContext.users.Remove(model);

            await _testerDbContext.SaveChangesAsync();

            return Ok(model);
        }

        [EnableCors()]
        [HttpGet]
        [Authorize]
        [Route("token")]
        public async Task<ActionResult> Token()
        {
            string errorText;
            if (_testerDbContext.users.FirstOrDefault(x => x.Email == User.Identity.Name) == null)
                return BadRequest(new { errorText = "Invalid token" });
            var response = await Task.Run(() => new
            {
                Id = _testerDbContext.users.FirstOrDefault(j => j.Email == User.Identity.Name).Id,
                Email = User.Identity.Name,
            });
            return Ok(response);
        }


        [EnableCors()]
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user == null)
                return BadRequest(new { errorText = "No data has been sent!" });

            var identity = GetIdentity(user.Email, user.Password);

            if (identity == null)
                return BadRequest(new { errorText = "Invalid Email or Password!" });

            // Создание JWT-токена
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: DateTime.Now,
                    claims: identity.Claims,
                    expires: DateTime.Now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = await Task.Run(() => new
            {
                Token = encodedJwt,
                UserId = _testerDbContext.users.FirstOrDefault(x => x.Email == user.Email).Id,
                Email = identity.Name
            });

            _testerDbContext.users.FirstOrDefault(x => x.Email == user.Email).Created = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            await _testerDbContext.SaveChangesAsync();

            return Ok(response);
        }
        private ClaimsIdentity GetIdentity(string email, string password)
        {
            // Проверка на наличие пользователя с такой почтой
            var user = _testerDbContext.users.FirstOrDefault(x => x.Email == email);

            if (user == null)
                return null;

            if (BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, _testerDbContext.Roles.FirstOrDefault(x => x.Id == user.RoleId).Name)
                };
                ClaimsIdentity claimsIdentity =
                new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);

                return claimsIdentity;
            }

            return null;
        }

    }
}
