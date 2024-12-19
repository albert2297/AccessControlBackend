using AccessControl.Application.Dtos.LogEntity;
using AccessControl.Application.Dtos.UserEntity;
using AccessControl.Infrastructure.Persistence.Entities;
using AutoMapper;

namespace AccessControl.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<LogEntity, LogEntityDto>().ReverseMap();
            CreateMap<UserEntity, LogUserDto>().ReverseMap();

            CreateMap<UserEntity, UserEntityDto>().ReverseMap();
            CreateMap<UserEntity, UserRegistrationRequestDto>().ReverseMap();
            CreateMap<UserEntity, UserUpdateRequestDto>().ReverseMap().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
