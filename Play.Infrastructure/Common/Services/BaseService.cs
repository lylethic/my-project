using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Play.Infrastructure.Common.Services;

public abstract class BaseService(IServiceProvider services)
{
    protected IServiceProvider _services = services;
    protected IMapper _mapper = services.GetRequiredService<IMapper>();
}
