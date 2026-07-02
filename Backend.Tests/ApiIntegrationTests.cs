using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private string UniqueName(string prefix) => $"{prefix}_{Guid.NewGuid():N}"[..20];

    // --- Auth Endpoints ---

    [Fact]
    public async Task Register_ReturnsOk_WithTokens()
    {
        var name = UniqueName("reg");
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = name,
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.Equal("OK", body.Message);
        Assert.True(body.Data.TryGetProperty("AccessToken", out _));
        Assert.True(body.Data.TryGetProperty("RefreshToken", out _));
    }

    [Fact]
    public async Task Register_Duplicate_ReturnsBadRequest()
    {
        var name = UniqueName("dup");
        await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = name,
            Password = "Test123!"
        });

        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = name,
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        Assert.Equal(400, body!.StatusCode);
        Assert.Equal("Bad Request", body.Message);
        Assert.Equal("Username already taken.", body.Hint);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var name = UniqueName("login");
        await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = name,
            Password = "Test123!"
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            UserName = name,
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.True(body.Data.TryGetProperty("AccessToken", out var token));
        Assert.NotEmpty(token.GetString()!);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var name = UniqueName("badpwd");
        await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = name,
            Password = "Test123!"
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            UserName = name,
            Password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        Assert.Equal(401, body!.StatusCode);
        Assert.Equal("Invalid credentials.", body.Hint);
    }

    [Fact]
    public async Task Login_NonexistentUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            UserName = UniqueName("nobody"),
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        Assert.Equal(401, body!.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var name = UniqueName("refresh");
        var (_, refreshToken) = await RegisterAndGetTokens(name);

        var response = await _client.PostAsJsonAsync("/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.True(body.Data.TryGetProperty("AccessToken", out _));
        Assert.True(body.Data.TryGetProperty("RefreshToken", out _));
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/refresh", new
        {
            RefreshToken = "invalid-token-value"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        Assert.Equal(401, body!.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesToken()
    {
        var name = UniqueName("logout");
        var (_, refreshToken) = await RegisterAndGetTokens(name);

        await _client.PostAsJsonAsync("/auth/logout", new
        {
            RefreshToken = refreshToken
        });

        var response = await _client.PostAsJsonAsync("/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- ApplicationUsers Endpoints ---

    [Fact]
    public async Task GetUsers_WithAuth_ReturnsOk()
    {
        var token = await GetAccessToken(UniqueName("listuser"));

        var response = await GetAuthenticatedAsync("/users", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
    }

    [Fact]
    public async Task GetUserById_NotFound_Returns404()
    {
        var token = await GetAccessToken(UniqueName("getbyid"));

        var response = await GetAuthenticatedAsync(
            "/users/00000000-0000-0000-0000-000000000000", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        Assert.Equal(404, body!.StatusCode);
        Assert.Equal("User not found.", body.Hint);
    }

    [Fact]
    public async Task CreateUser_WithAuth_ReturnsCreated()
    {
        var token = await GetAccessToken(UniqueName("createby"));

        var response = await PostAuthenticatedAsync("/users", token, new
        {
            UserName = UniqueName("newuser"),
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(201, body!.StatusCode);
        Assert.Equal("Created", body.Message);
    }

    [Fact]
    public async Task DeleteUser_ReturnsOk()
    {
        var token = await GetAccessToken(UniqueName("deltest"));

        var response = await DeleteAuthenticatedAsync("/users", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        Assert.Equal(204, body!.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WithAuth_ReturnsOk()
    {
        var token = await GetAccessToken(UniqueName("updateuser"));

        var newName = UniqueName("newname");
        var response = await PutAuthenticatedAsync("/users", token, new
        {
            UserName = newName
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.Equal(newName, body.Data.GetProperty("UserName").GetString());
    }

    // --- Helpers ---

    private async Task<string> GetAccessToken(string userName)
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = userName,
            Password = "Test123!"
        });

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            response = await _client.PostAsJsonAsync("/auth/login", new
            {
                UserName = userName,
                Password = "Test123!"
            });
        }

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        return body!.Data.GetProperty("AccessToken").GetString()!;
    }

    private async Task<(string accessToken, string refreshToken)> RegisterAndGetTokens(string userName)
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            UserName = userName,
            Password = "Test123!"
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        return (
            body!.Data.GetProperty("AccessToken").GetString()!,
            body.Data.GetProperty("RefreshToken").GetString()!
        );
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostAuthenticatedAsync(string url, string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAuthenticatedAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAuthenticatedAsync(string url, string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new("Bearer", token);
        return await _client.SendAsync(request);
    }

    // --- Servers Endpoints ---

    [Fact]
    public async Task CreateServer_ReturnsCreated()
    {
        var token = await GetAccessToken(UniqueName("srvcreate"));
        var name = UniqueName("server");

        var response = await PostAuthenticatedAsync("/servers", token, new
        {
            Name = name,
            Description = "A test server"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(201, body!.StatusCode);
        Assert.Equal(name, body.Data.GetProperty("Name").GetString());
        Assert.True(body.Data.TryGetProperty("Id", out _));
    }

    [Fact]
    public async Task GetServer_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvget"));

        var response = await GetAuthenticatedAsync($"/servers/{serverId}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.Equal(serverId, body.Data.GetProperty("Id").GetString());
    }

    [Fact]
    public async Task GetServer_NotFound_Returns404()
    {
        var token = await GetAccessToken(UniqueName("srv404"));
        var response = await GetAuthenticatedAsync("/servers/00000000-0000-0000-0000-000000000000", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListServers_ReturnsUserServers()
    {
        var token = await GetAccessToken(UniqueName("srvlist"));
        await CreateServerAndGetId(UniqueName("server1"), token);
        await CreateServerAndGetId(UniqueName("server2"), token);

        var response = await GetAuthenticatedAsync("/servers", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.Equal(JsonValueKind.Array, body.Data.ValueKind);
        Assert.True(body.Data.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task UpdateServer_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvupd"));
        var newName = UniqueName("updated");

        var response = await PutAuthenticatedAsync($"/servers/{serverId}", token, new
        {
            Name = newName,
            Description = "Updated"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(newName, body!.Data.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task DeleteServer_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvdel"));

        var response = await DeleteAuthenticatedAsync($"/servers/{serverId}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await GetAuthenticatedAsync($"/servers/{serverId}", token);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetMembers_ServerCreated_OwnerIsMember()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvmem"));

        var response = await GetAuthenticatedAsync($"/servers/{serverId}/members", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
        Assert.Equal(1, body.Data.GetArrayLength());
    }

    [Fact]
    public async Task JoinAndAccept_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvjoin"));
        var otherToken = await GetAccessToken(UniqueName("joiner"));

        var serverResp = await GetAuthenticatedAsync($"/servers/{serverId}", token);
        var serverBody = await serverResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var joinCode = serverBody!.Data.GetProperty("JoinCode").GetString()!;

        var joinResp = await PostAuthenticatedAsync($"/servers/join/{joinCode}", otherToken, new { });
        Assert.Equal(HttpStatusCode.OK, joinResp.StatusCode);
        var joinBody = await joinResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, joinBody!.StatusCode);

        var requestId = joinBody.Data.GetProperty("Id").GetString()!;

        var acceptResp = await PostAuthenticatedAsync($"/servers/{serverId}/join-requests/{requestId}/accept", token, new { });
        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);
        var acceptBody = await acceptResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, acceptBody!.StatusCode);

        var otherUserId = GetUserIdFromToken(otherToken);
        Assert.Equal(otherUserId, acceptBody.Data.GetProperty("UserId").GetString());
    }

    [Fact]
    public async Task RemoveMember_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvremm"));
        var otherToken = await GetAccessToken(UniqueName("removeme"));

        var otherUserId = GetUserIdFromToken(otherToken);
        await AddMemberViaJoinAsync(token, serverId, otherToken);

        var response = await DeleteAuthenticatedAsync($"/servers/{serverId}/members/{otherUserId}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Roles Endpoints ---

    [Fact]
    public async Task ServerCreated_AutoSeedsOwnerAndMemberRoles()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvroles"));

        var response = await GetAuthenticatedAsync($"/servers/{serverId}/roles", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.True(body!.Data.GetArrayLength() >= 2, "Should have at least Owner and Member roles");
    }

    [Fact]
    public async Task CreateRole_ReturnsCreated()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvcr"));

        var response = await PostAuthenticatedAsync($"/servers/{serverId}/roles", token, new
        {
            Name = "Moderator",
            Permissions = (long)(Permission.KickMembers | Permission.BanMembers | Permission.MuteMembers),
            Position = 50,
            Color = "#00FF00"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(201, body!.StatusCode);
        Assert.Equal("Moderator", body.Data.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task UpdateRole_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvur"));
        var rolesResp = await GetAuthenticatedAsync($"/servers/{serverId}/roles", token);
        var rolesBody = await rolesResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var memberRoleId = FindRoleIdByName(rolesBody!, "Member");

        var response = await PutAuthenticatedAsync($"/servers/{serverId}/roles/{memberRoleId}", token, new
        {
            Name = "Peasant",
            Position = 0
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal("Peasant", body!.Data.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task DeleteRole_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvdr"));
        var rolesResp = await GetAuthenticatedAsync($"/servers/{serverId}/roles", token);
        var rolesBody = await rolesResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var roleId = rolesBody!.Data[0].GetProperty("Id").GetString()!;

        var response = await DeleteAuthenticatedAsync($"/servers/{serverId}/roles/{roleId}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Role Assignment ---

    [Fact]
    public async Task AssignRoleToMember_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvarm"));
        var otherToken = await GetAccessToken(UniqueName("assignee"));

        var memberId = await AddMemberViaJoinAsync(token, serverId, otherToken);

        var rolesResp = await GetAuthenticatedAsync($"/servers/{serverId}/roles", token);
        var rolesBody = await rolesResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var roleId = rolesBody!.Data[0].GetProperty("Id").GetString()!;

        var response = await PostAuthenticatedAsync($"/servers/{serverId}/members/{memberId}/roles", token, new
        {
            RoleId = roleId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var memberRolesResp = await GetAuthenticatedAsync($"/servers/{serverId}/members/{memberId}/roles", token);
        var memberRolesBody = await memberRolesResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.True(memberRolesBody!.Data.GetArrayLength() > 0);
    }

    [Fact]
    public async Task RemoveRoleFromMember_ReturnsOk()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvrrm"));
        var otherToken = await GetAccessToken(UniqueName("remrole"));

        var memberId = await AddMemberViaJoinAsync(token, serverId, otherToken);

        var rolesResp = await GetAuthenticatedAsync($"/servers/{serverId}/roles", token);
        var rolesBody = await rolesResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var roleId = rolesBody!.Data[0].GetProperty("Id").GetString()!;

        await PostAuthenticatedAsync($"/servers/{serverId}/members/{memberId}/roles", token, new { RoleId = roleId });
        var response = await DeleteAuthenticatedAsync($"/servers/{serverId}/members/{memberId}/roles/{roleId}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEffectivePermissions_AggregatesRoles()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("srvperm"));

        var membersResp = await GetAuthenticatedAsync($"/servers/{serverId}/members", token);
        var membersBody = await membersResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var memberId = membersBody!.Data[0].GetProperty("Id").GetString()!;

        var response = await GetAuthenticatedAsync($"/servers/{serverId}/members/{memberId}/permissions", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        Assert.Equal(200, body!.StatusCode);
    }

    // --- Permission Checks ---

    [Fact]
    public async Task NonOwner_MissingPermission_ReturnsForbidden()
    {
        var (ownerToken, serverId) = await CreateServerAndGetId(UniqueName("permdeny"));
        var memberToken = await GetAccessToken(UniqueName("normiem"));

        await AddMemberViaJoinAsync(ownerToken, serverId, memberToken);

        var response = await PutAuthenticatedAsync($"/servers/{serverId}", memberToken, new
        {
            Name = "Hacked"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonOwner_MissingRoleManage_ReturnsForbidden()
    {
        var (ownerToken, serverId) = await CreateServerAndGetId(UniqueName("permdn2"));
        var memberToken = await GetAccessToken(UniqueName("normie2"));

        await AddMemberViaJoinAsync(ownerToken, serverId, memberToken);

        var response = await PostAuthenticatedAsync($"/servers/{serverId}/roles", memberToken, new
        {
            Name = "Unauthorized",
            Permissions = 0
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_Administrator_CanDoEverything()
    {
        var (token, serverId) = await CreateServerAndGetId(UniqueName("adminbyp"));

        var roleResp = await PostAuthenticatedAsync($"/servers/{serverId}/roles", token, new
        {
            Name = "AdminRole",
            Permissions = (long)Permission.Administrator,
            Position = 100
        });
        Assert.Equal(HttpStatusCode.Created, roleResp.StatusCode);

        var response = await PutAuthenticatedAsync($"/servers/{serverId}", token, new
        {
            Name = "AdminUpdated"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- OpenAPI / Swagger ---

    [Fact]
    public async Task SwaggerJson_ReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3.0.4", body.GetProperty("openapi").GetString());
        Assert.Equal("Ember API", body.GetProperty("info").GetProperty("title").GetString());
    }

    [Fact]
    public async Task SwaggerUI_ReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- New Helpers ---

    private string GetUserIdFromToken(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var nameIdClaim = jwt.Claims.FirstOrDefault(c =>
            c.Type == JwtService.NameIdentifierClaimType);

        return nameIdClaim?.Value ?? throw new InvalidOperationException("Could not find user ID in token");
    }

    private async Task<(string token, string serverId)> CreateServerAndGetId(string serverName, string? existingToken = null)
    {
        var token = existingToken ?? await GetAccessToken(UniqueName(serverName));

        var response = await PostAuthenticatedAsync("/servers", token, new
        {
            Name = serverName,
            Description = "Test server"
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        return (token, body!.Data.GetProperty("Id").GetString()!);
    }

    private static string FindRoleIdByName(ApiResponse<JsonElement> body, string name)
    {
        foreach (var role in body!.Data.EnumerateArray())
        {
            if (role.GetProperty("Name").GetString() == name)
                return role.GetProperty("Id").GetString()!;
        }

        throw new InvalidOperationException($"Role '{name}' not found");
    }

    private async Task<string> AddMemberViaJoinAsync(string ownerToken, string serverId, string memberToken)
    {
        var serverResp = await GetAuthenticatedAsync($"/servers/{serverId}", ownerToken);
        var serverBody = await serverResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var joinCode = serverBody!.Data.GetProperty("JoinCode").GetString()!;

        var joinResp = await PostAuthenticatedAsync($"/servers/join/{joinCode}", memberToken, new { });
        var joinBody = await joinResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        var requestId = joinBody!.Data.GetProperty("Id").GetString()!;

        var acceptResp = await PostAuthenticatedAsync($"/servers/{serverId}/join-requests/{requestId}/accept", ownerToken, new { });
        var acceptBody = await acceptResp.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(JsonOptions);
        return acceptBody!.Data.GetProperty("Id").GetString()!;
    }
}
