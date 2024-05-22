using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Project01.Context;
using Project01.DTOs;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Project01.Clients.AuthClients
{
    public class AuthClientService
    {

        private readonly string _connectionString;
        public AuthClientService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlConnection");
        }

        public async Task<AppResponse> AuthenticateToken(string? token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var tokenId = jwtToken.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;
            var userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            var response = new AppResponse();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("ValidateToken", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.Add("@Token", SqlDbType.NVarChar, 500).Value = tokenId;
                    command.Parameters.Add("@UserId", SqlDbType.NVarChar, 500).Value = userId;

                    // Add output parameter for the result
                    command.Parameters.Add("@IsTokenValid", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    // Execute the command
                    await command.ExecuteNonQueryAsync();

                    // Read the output parameter value
                    var isTokenValid = (bool)command.Parameters["@IsTokenValid"].Value;

                    if (isTokenValid)
                    {
                        response.ResCode = 1;
                        response.ResMsg = "This token has been revoked.";
                        response.ResBody = null;
                    }
                    else
                    {
                        response.ResCode = 100;
                        response.ResMsg = "Success";
                        response.ResBody = null;
                    }
                }
            }

            return response;
        }


    }
}
