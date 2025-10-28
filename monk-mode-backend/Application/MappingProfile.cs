using AutoMapper;
using monk_mode_backend.Domain;
using monk_mode_backend.DTOs;
using monk_mode_backend.Models;

namespace monk_mode_backend.Application
{
    /// <summary>
    /// MappingProfile – hardened & aligned to current DTOs.
    /// - Read maps (Entity -> DTO) liefern vollständige, UI-taugliche Antworten.
    /// - Write maps (DTO -> Entity) ignorieren Id/UserId/CreatedAt/Navigations, um Overposting zu verhindern.
    /// - FriendshipResponseDTO: FriendUsername wird im Service gesetzt (hier bewusst .Ignore()).
    /// - TimeBlockDTO.Tasks wird nicht in die Entity zurückgeschrieben (separate Endpoints steuern Tasks).
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // -----------------------------
            // ApplicationUser <-> UserProfileDTO
            // -----------------------------
            CreateMap<ApplicationUser, UserProfileDTO>()
                .ForMember(d => d.Username, opt => opt.MapFrom(s => s.UserName ?? string.Empty))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email ?? string.Empty));

            // (Kein ReverseMap nötig — UserProfileDTO ist reines Read-Model)

            // -----------------------------
            // TimeBlock <-> TimeBlockDTO
            // -----------------------------
            CreateMap<TimeBlock, TimeBlockDTO>()
                // Optional: leere Liste statt null ausliefern (UI-freundlich)
                .ForMember(d => d.Tasks, opt => opt.MapFrom(s => s.Tasks))
                ;

            CreateMap<TimeBlockDTO, TimeBlock>()
                // Security: Id/UserId serverseitig setzen, nicht vom Client
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                // Tasks werden separat verwaltet (nicht aus DTO in Entity mappen)
                .ForMember(d => d.Tasks, opt => opt.Ignore());

            // -----------------------------
            // UserTask <-> TaskDTO
            // -----------------------------
            CreateMap<UserTask, TaskDTO>();

            CreateMap<TaskDTO, UserTask>()
                // Security: Server kontrolliert Id/UserId/TimeBlockId
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.TimeBlockId, opt => opt.Ignore());

            // CreateTaskDTO -> UserTask (Write-only)
            CreateMap<CreateTaskDTO, UserTask>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.TimeBlockId, opt => opt.Ignore());

            // UpdateTaskDTO -> UserTask (Write-only, Partial Update – Controller setzt gezielt Felder)
            CreateMap<UpdateTaskDTO, UserTask>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.TimeBlockId, opt => opt.Ignore());

            // -----------------------------
            // Template <-> TemplateDTO
            // -----------------------------
            CreateMap<Template, TemplateDTO>()
                .ForMember(d => d.TemplateBlocks, opt => opt.MapFrom(s => s.TemplateBlocks));

            CreateMap<TemplateDTO, Template>()
                // Security: diese Felder nie vom Client übernehmen
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                // TemplateBlocks werden über den TemplateBlockController verwaltet
                .ForMember(d => d.TemplateBlocks, opt => opt.Ignore());

            // -----------------------------
            // TemplateBlock <-> TemplateBlockDTO
            // -----------------------------
            CreateMap<TemplateBlock, TemplateBlockDTO>();

            CreateMap<TemplateBlockDTO, TemplateBlock>()
                // Security: Server kontrolliert Id/TemplateId
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.TemplateId, opt => opt.Ignore())
                // Navigation nicht vom Client überschreiben
                .ForMember(d => d.Template, opt => opt.Ignore());

            // -----------------------------
            // Friendship -> FriendshipDTO / FriendshipResponseDTO
            // -----------------------------
            CreateMap<Friendship, FriendshipDTO>();
            // Response-Variante für UI (enthält FriendUsername, wird im Service gesetzt)
            CreateMap<Friendship, FriendshipResponseDTO>()
                .ForMember(d => d.FriendUsername, opt => opt.Ignore());

            // -----------------------------
            // DailyStatistics <-> DailyStatisticsDTO
            // -----------------------------
            CreateMap<DailyStatistics, DailyStatisticsDTO>();

            CreateMap<DailyStatisticsDTO, DailyStatistics>()
                // Security: Server kontrolliert Id/UserId
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore());

            // -----------------------------
            // LoginDTO / RegisterDTO / TokenDTO / ResponseDTO / UpdateXpRequestDTO
            // keine Entitäts-Mappings nötig (Request/Response-DTOs)
            // -----------------------------
        }
    }
}