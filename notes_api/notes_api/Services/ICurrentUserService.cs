using System.Security.Claims;

namespace NotesApi.Services
{

    public interface ICurrentUserService
    {
        int? UserSubId { get; }
        ClaimsPrincipal? Principals { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserSubId
        {
            get
            {
                if (int.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var v))
                {
                    return v;
                }
                return null;
            }
        }

        public ClaimsPrincipal? Principals => _httpContextAccessor.HttpContext?.User;
    }
}
