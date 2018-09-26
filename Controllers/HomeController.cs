using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using andead.netcore.jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

[Route("/")]
public class HomeController : Controller
{
    private IPeople people;

    public HomeController(IPeople people)
    {
        this.people = people;
    }

    [HttpGet("token")]
    public IActionResult GetToken(string login, string password)
    {
        try
        {
            Person person = people.GetPeople().FirstOrDefault(u => u.UserName == login && u.Password == password);
            if (person != null)
            {

                return Ok(JsonConvert.SerializeObject(
                    new
                    {
                        access_token = GenerateJwtToken(person),
                        refresh_token = person.RefreshToken
                    }, new JsonSerializerSettings { Formatting = Formatting.Indented }));
            }

            return Unauthorized();
        }
        catch (ApplicationException)
        {
            return Unauthorized();
        }
    }

    private string GetNewRefreshToken()
    {
        return Guid.NewGuid().ToString().Replace("-", "");
    }

    private string GenerateJwtToken(Person person)
    {
        people.SetUserRefreshToken(person.Id, GetNewRefreshToken());

        var claims = new List<Claim>()
        {
            //new Claim(JwtRegisteredClaimNames.Sub, "test@mail.ru"),
            //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimsIdentity.DefaultNameClaimType, person.UserName),
            new Claim(ClaimsIdentity.DefaultRoleClaimType, person.Role)
            //new Claim(ClaimTypes.NameIdentifier, "tester"),
            //new Claim(ClaimTypes.Role, "admin")
        };

        var token = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Authorize, HttpGet("secure")]
    public IActionResult Secret()
    {
        return Ok($"Your name is: {User.Identity.Name}");
    }

    [HttpGet("refresh")]
    public IActionResult Refresh(string refreshToken)
    {
        try
        {
            if (!String.IsNullOrEmpty(refreshToken))
            {
                Person person = people.GetPeople().FirstOrDefault(u => u.RefreshToken == refreshToken);

                if (person != null)
                {
                    people.SetUserRefreshToken(person.Id, GetNewRefreshToken());

                    return Ok(JsonConvert.SerializeObject(
                        new
                        {
                            access_token = GenerateJwtToken(person),
                            refresh_token = person.RefreshToken
                        }, new JsonSerializerSettings { Formatting = Formatting.Indented }));
                }
            }
            
            return Unauthorized();
        }
        catch (ApplicationException)
        {
            return Unauthorized();
        }
    }
}