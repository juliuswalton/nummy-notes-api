using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NummyNotesApi.Models;


namespace NummyNotesApi.Services;

public class UsersService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly PasswordHasher<User> _passwordHasher;

    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public UsersService(IOptions<NummyNotesDatabaseSettings> nummyNotesDatabaseSettings)
    {
        DotNetEnv.Env.Load();

        var mongoClient = new MongoClient(nummyNotesDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(nummyNotesDatabaseSettings.Value.DatabaseName);

        _usersCollection = mongoDatabase.GetCollection<User>(nummyNotesDatabaseSettings.Value.UsersCollectionName);
        _passwordHasher = new PasswordHasher<User>();

        _jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")!;

        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!;

        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;
    }

    public async Task<string> Authenticate(string email, string password)
    {
        DotNetEnv.Env.Load();
        var user = await _usersCollection.Find(x => x.Email == email).FirstOrDefaultAsync();

        if (user is null)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        if (result != PasswordVerificationResult.Success)
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenKey = Encoding.ASCII.GetBytes(_jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Email, email),
            }),

            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience, 

            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(tokenKey),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<List<User>> GetAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<User?> GetAsync(string id) =>
        await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(User newUser)
    {
        newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);
        await _usersCollection.InsertOneAsync(newUser);
    }

    public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);
}