using AutoMapper;
using GroupListNet.Core.Extensions;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.ParseSchedule;
using Microsoft.AspNetCore.Identity;

namespace GroupListNet.Core.src.Context
{
    public static class AutoMappingProfile
    {
        public static void ConfigurateAutoMapper(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ParsedScheduleItem, Schedule>()
                .ForMember(dist => dist.StartTime, opt => opt.MapFrom(x => x.StartTime.ToTimeSpan()))
                .ForMember(dist => dist.ClassType, opt => opt.MapFrom(x => ClassTypeParser.GetClassType(x.Type)));
        }
    }
}
