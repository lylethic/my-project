using System;
using System.Data;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Repositories;

namespace Play.Infrastructure.Repository;

public class CateRepo(IDbConnection connection) : SimpleCrudRepositories<Category, string>(connection), IScoped
{

}
