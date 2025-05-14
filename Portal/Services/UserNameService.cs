using Microsoft.AspNetCore.Http;
using Portal.Repository;
using System.DirectoryServices.AccountManagement;

namespace Portal.Services
{
    public class UserNameService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserNameService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetDisplayName()
        {
            string displayName = "";
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return "Unknown";

            using var context = new PrincipalContext(ContextType.Domain, AD.root);
            try
            {
                var user = UserPrincipal.FindByIdentity(context, username);
                displayName = user?.DisplayName ?? username;
            }
            catch
            {
                displayName = username;
            }

            return displayName;
        }
    }
}
