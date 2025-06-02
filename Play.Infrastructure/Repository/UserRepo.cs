using System;
using System.Data;
using System.Text;
using ClosedXML.Excel;
using Dapper;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
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
    public async Task<User?> Update(User user)
    {
        var sqlBuilder = new StringBuilder("UPDATE users SET ");
        var parameters = new DynamicParameters();
        var fields = new List<string>();
        int actualUpdateCount = 0;

        // Add fields to update if provided
        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            fields.Add("first_name = @FirstName");
            parameters.Add("FirstName", user.FirstName);
            actualUpdateCount++;
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            fields.Add("last_name = @LastName");
            parameters.Add("LastName", user.LastName);
            actualUpdateCount++;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            fields.Add("email = @Email");
            parameters.Add("Email", user.Email);
            actualUpdateCount++;
        }

        if (!string.IsNullOrWhiteSpace(user.RoleId))
        {
            fields.Add("role_id = @RoleId");
            parameters.Add("RoleId", user.RoleId);
            actualUpdateCount++;
        }

        // Always update these system fields
        fields.Add("is_active = @IsActive");
        parameters.Add("IsActive", user.IsActive);
        actualUpdateCount++; // Count IsActive as a meaningful update

        // Always update the timestamp
        fields.Add("updated_at = @UpdatedAt");
        parameters.Add("UpdatedAt", DateTime.Now);

        // Handle deleted_at (can be null for active users)
        fields.Add("deleted_at = @DeletedAt");
        parameters.Add("DeletedAt", user.DeletedAt);

        // Check if any meaningful updates are being made
        if (actualUpdateCount == 0)
            throw new ArgumentException("No fields provided for update.");

        // Build the SQL query
        sqlBuilder.Append(string.Join(", ", fields));
        sqlBuilder.Append(" WHERE id = @Id RETURNING id, role_id, first_name, last_name, email, is_active, updated_at, deleted_at");
        parameters.Add("Id", user.Id);

        try
        {
            return await connection.QuerySingleOrDefaultAsync<User>(sqlBuilder.ToString(), parameters);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            // Handle unique constraint violations
            if (ex.ConstraintName == "unique_email")
                throw new ArgumentException("Email is already in use by another user.");

            // Re-throw if it's a different constraint
            throw;
        }
    }

    public async Task<User?> GetById(string id)
    {
        var sql = """
            SELECT id, role_id, email, first_name, last_name, created_at,updated_at, deleted_at, is_active 
            FROM users WHERE TRIM(id) = TRIM(@Id);
       """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetUsers(PaginationRequest request)
    {
        var whereConditions = new List<string>();
        if (request.IsActive.HasValue)
            whereConditions.Add("is_active = @IsActive");
        if (request.LastCreatedAt.HasValue)
            whereConditions.Add("created_at < @LastCreatedAt");

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var query = $"""
            SELECT id, role_id, email, first_name, last_name, created_at, updated_at, deleted_at, is_active  
            FROM users
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

    public async Task<string> ExportUsersToExcel(bool? isActive = null, int? maxRows = null)
    {
        if (maxRows.HasValue && maxRows <= 0)
        {
            throw new ArgumentException("maxRows must be a positive integer.", nameof(maxRows));
        }

        // Create temporary file
        var tempFile = Path.GetTempFileName();

        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096))
        {
            // Create workbook and worksheet
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users");

            // Add header row
            worksheet.Cell(1, 1).Value = "RoleId";
            worksheet.Cell(1, 2).Value = "FirstName";
            worksheet.Cell(1, 3).Value = "LastName";
            worksheet.Cell(1, 4).Value = "Email";
            worksheet.Cell(1, 5).Value = "CreatedAt";
            worksheet.Cell(1, 6).Value = "UpdatedAt";
            worksheet.Cell(1, 7).Value = "DeletedAt";
            worksheet.Cell(1, 8).Value = "IsActive";

            // Set fixed column widths to avoid AutoFitColumns
            worksheet.Column(1).Width = 36; // RoleId (UUID)
            worksheet.Column(2).Width = 20; // FirstName
            worksheet.Column(3).Width = 20; // LastName
            worksheet.Column(4).Width = 30; // Email
            worksheet.Column(5).Width = 20; // CreatedAt
            worksheet.Column(6).Width = 20; // UpdatedAt
            worksheet.Column(7).Width = 20; // DeletedAt
            worksheet.Column(8).Width = 10; // IsActive

            // Build SQL query
            var sql = new StringBuilder(@"
            SELECT id, role_id, first_name, last_name, email, created_at, updated_at, deleted_at, is_active
            FROM users
        ");
            var parameters = new DynamicParameters();
            var conditions = new List<string>();

            if (isActive.HasValue)
            {
                conditions.Add("is_active = @IsActive");
                parameters.Add("IsActive", isActive.Value);
            }

            if (conditions.Any())
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", conditions));
            }

            sql.Append(" ORDER BY id");

            int rowIndex = 2;
            const int batchSize = 10000; // Process 10,000 rows per batch
            int totalRowsProcessed = 0;
            int offset = 0;

            while (true)
            {
                int currentBatchSize = maxRows.HasValue ? Math.Min(batchSize, maxRows.Value - totalRowsProcessed) : batchSize;
                if (currentBatchSize <= 0)
                    break;

                // Build batch query
                var batchSql = new StringBuilder(sql.ToString());
                batchSql.Append(" OFFSET @Offset LIMIT @BatchSize;");
                parameters.Add("Offset", offset);
                parameters.Add("BatchSize", currentBatchSize);

                var batch = await _connection.QueryAsync<User>(batchSql.ToString(), parameters);
                var batchList = batch.ToList();
                if (batchList.Count == 0)
                    break;

                // Prepare data for InsertData
                var data = batchList.Select(user => new object[]
                {
                user.RoleId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                user.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                user.DeletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                user.IsActive
                });

                // Insert batch data starting at rowIndex
                worksheet.Cell(rowIndex, 1).InsertData(data);
                rowIndex += batchList.Count;
                totalRowsProcessed += batchList.Count;

                if (maxRows.HasValue && totalRowsProcessed >= maxRows.Value)
                    break;

                offset += batchSize;

                // Save periodically to flush to disk
                workbook.SaveAs(fileStream);
            }

            workbook.SaveAs(fileStream);
        }

        return tempFile;
    }
    public async Task<List<User>> ImportUsersFromExcel(IFormFile file)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var users = new List<User>();

        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var result = reader.AsDataSet();
        var table = result.Tables[0];

        for (int i = 1; i < table.Rows.Count; i++) // Skip header
        {
            var row = table.Rows[i];

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                RoleId = row[0].ToString() ?? throw new ArgumentException("RoleId cannot be null"),
                FirstName = row[1].ToString() ?? throw new ArgumentException("FirstName cannot be null"),
                LastName = row[2].ToString() ?? throw new ArgumentException("LastName cannot be null"),
                Email = row[3].ToString() ?? throw new ArgumentException("Email cannot be null"),
                Password = BCrypt.Net.BCrypt.HashPassword(row[4].ToString()), // Hash password
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                DeletedAt = null,
                IsActive = true
            };

            // Check if email already exists
            var checkSql = "SELECT COUNT(*) FROM users WHERE email = @Email";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { user.Email });

            if (exists == 0)
            {
                var insertSql = """
                INSERT INTO users (id, role_id, first_name, last_name, email, password, created_at, updated_at, deleted_at, is_active)
                VALUES (@Id, @RoleId, @FirstName, @LastName, @Email, @Password, @CreatedAt, @UpdatedAt, @DeletedAt, @IsActive)
                """;
                await connection.ExecuteAsync(insertSql, user);
                users.Add(user);
            }
        }

        return users;
    }


    public async Task<bool> CheckEmailUnique(string email, string excludeUserId)
    {
        const string sql = @"
        SELECT COUNT(1) 
        FROM users 
        WHERE LOWER(email) = LOWER(@Email) 
        AND id != @ExcludeUserId 
        AND deleted_at IS NULL";

        var parameters = new DynamicParameters();
        parameters.Add("Email", email.Trim());
        parameters.Add("ExcludeUserId", excludeUserId);

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count == 0;
    }

    public async Task<bool> IsEmailUnique(string email)
    {
        var sql = "SELECT COUNT(1) FROM users WHERE email = @Email";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count == 0;
    }
}
