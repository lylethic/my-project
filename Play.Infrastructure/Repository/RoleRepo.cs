using System;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Databases;
using Play.Infrastructure.Common.Repositories;

namespace Play.Infrastructure.Repository;

public class RoleRepo(IDbConnection connection) : SimpleCrudRepositories<Role, string>(connection), IScoped
{
    public async Task<Role?> GetRoleById(string id)
    {
        var param = new { Id = id };
        var sql = SqlCommandHelper.GetSelectSqlWithCondition<Role>(new { Id = id });
        return await GetOneByConditionAsync(sql, param);
    }

    public async Task<Role?> GetById(string id)
    {
        var sql = """
            SELECT * FROM roles WHERE id = @Id;
       """;
        return await connection.QuerySingleOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<List<Role>> GetAllRoles()
    {
        var sql = SqlCommandHelper.GetSelectSql<Role>();
        var result = await connection.QueryAsync<Role>(sql);
        return [.. result];
    }
    public async Task<Role?> Create(Role role)
    {
        var sql = """
            INSERT INTO roles(id, name, created_at, updated_at, deleted_at, is_active)
            VALUES (@Id, @Name, @CreatedAt, @UpdatedAt, @DeletedAt, @IsActive)
        """;
        return await connection.QuerySingleOrDefaultAsync<Role>(sql, role);
    }
    public async Task<Role?> Update(Role role)
    {
        var sql = @"UPDATE roles
                SET name = @Name,
                    updated_at = @UpdatedAt,
                    deleted_at = @DeletedAt,
                    is_active = @IsActive
                WHERE id = @Id
                RETURNING *";
        return await connection.QuerySingleOrDefaultAsync<Role>(sql, role);
    }

    public async Task Delete(Role role)
    {
        await DeleteAsync(role);
    }

    public async Task<IEnumerable<Role>> GetRolesWithPagination(PaginationRequest request)
    {
        var whereConditions = new List<string>();
        if (request.IsActive.HasValue)
            whereConditions.Add("is_active = @IsActive");
        if (request.LastCreatedAt.HasValue)
            whereConditions.Add("created_at < @LastCreatedAt");

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var query = $"""
            SELECT * FROM roles
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

        var roles = await connection.QueryAsync<Role>(query, parameters);
        return roles;
    }

}
