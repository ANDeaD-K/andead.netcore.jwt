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
using Newtonsoft.Json.Linq;

[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("1.1")]
[Route("api/[controller]/v{version:apiVersion}")]
public class IdentityController : Controller
{
    private IPeople people;

    public IdentityController(IPeople people)
    {
        this.people = people;
    }

    [HttpGet("token")]
    [MapToApiVersion("1.1")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
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

    /// <summary>
    /// Return GUID token without dash
    /// </summary>
    /// <returns>New Refresh token</returns>
    private string GetNewRefreshToken()
    {
        return Guid.NewGuid().ToString().Replace("-", "");
    }

    private string GenerateJwtToken(Person person)
    {
        people.SetUserRefreshToken(person.Id, GetNewRefreshToken());

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, person.Id.ToString()),
            new Claim(ClaimsIdentity.DefaultNameClaimType, person.UserName),
            new Claim(ClaimsIdentity.DefaultRoleClaimType, person.Role)
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
    [MapToApiVersion("1.1")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Secret()
    {
        return Ok($"Your name is: {User.Identity.Name}");
    }

    [HttpGet("test")]
    [MapToApiVersion("1.0")]
    public IActionResult Test()
    {
        return Ok($"This is test message");
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="data">JSON object with Refresh token</param>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /refresh
    ///     {
    ///        "refresh_token": "965044bd7f4a4972919516eaa76725d4"
    ///     }
    /// 
    /// </remarks>
    /// <returns>HTTP Response</returns>
    /// <response code="200">Returns JSON object with Access token and Refresh token</response>
    /// <response code="401">Invalid Refresh token</response>
    [HttpPost("refresh")]
    [MapToApiVersion("1.1")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Refresh([FromBody] JObject data)
    {
        try
        {
            string refreshToken = data.GetValue("refresh_token").ToString();

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