using System;
using AutoMapper;
using Play.Application.DTOs;
using Play.Domain.Entities;

namespace Play.Infrastructure.Helpers;

public class RoleProfile : Profile
{
    public RoleProfile()
    {
        CreateMap<Role, RoleDto>();

        // UpdateRequest -> role
        CreateMap<UpdateRoleRequest, Role>()
        .ForAllMembers(x => x.Condition(
            (src, dest, prop) =>
            {
                // ignore both null & empty string properties
                if (prop is null) return false;
                if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;
                return true;
            }
        ));
    }
}
