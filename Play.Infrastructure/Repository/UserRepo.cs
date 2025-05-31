using System;
using System.Data;
using Dapper;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Repositories;

namespace Play.Infrastructure.Repository;

public class UserRepo(IDbConnection connection) : SimpleCrudRepositories<User, string>(connection), IScoped
{
    public async Task<User?> Create(User role)
    {
        var sql = """
            INSERT INTO users (id, role_id, first_name, last_name, email, password, created_at, updated_at, deleted_at, is_active)
            VALUES (@Id, @RoleId, @FirstName, @LastName, @Email, @Password, @CreatedAt, @UpdatedAt, @DeletedAt, @IsActive)
        """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, role);
    }

    public async Task<UserDto?> GetById(string id)
    {
        var sql = """
            SELECT id, role_id, email, first_name, last_name, created_at, is_active 
            FROM users WHERE id = @Id;
       """;
        return await connection.QuerySingleOrDefaultAsync<UserDto>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAll(PaginationRequest request)
    {
        var whereConditions = new List<string>();
        if (request.IsActive.HasValue)
            whereConditions.Add("is_active = @IsActive");
        if (request.LastCreatedAt.HasValue)
            whereConditions.Add("created_at < @LastCreatedAt");

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var query = $"""
            SELECT * FROM users
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize;
         """;

        var parameters = new
        {
            request.IsActive,
            request.LastCreatedAt,
            request.PageSize
        };

        var users = await connection.QueryAsync<User>(query, parameters);
        return users;
    }
}
