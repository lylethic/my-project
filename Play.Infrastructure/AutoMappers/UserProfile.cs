using System;
using AutoMapper;
using Play.Application.DTOs;
using Play.Domain.Entities;

namespace Play.Infrastructure.AutoMappers;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UpdateUserRequest, User>()
        .ForAllMembers(x => x.Condition(
            (src, dest, prop) =>
            {
                if (prop == null) return false;
                if (prop is string str && string.IsNullOrEmpty(str))
                    return false; return true;
            }));
    }
}
