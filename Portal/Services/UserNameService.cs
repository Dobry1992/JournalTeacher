using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Portal.Repository;
using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;

namespace Portal.Services
{
    public class UserNameService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly bool _isDevelopment;

        public UserNameService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _isDevelopment = configuration.GetValue<bool>("AD:IsDevelopment");
        }

        public string GetDisplayName()
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return "Unknown";

            // Если в разработке - возвращаем username без обращения к AD
            if (_isDevelopment)
                return username;

            try
            {
                using var context = new PrincipalContext(ContextType.Domain, AD.root);
                var user = UserPrincipal.FindByIdentity(context, username);
                return user?.DisplayName ?? username;
            }
            catch (LdapException ex) when (ex.Message.Contains("unavailable") || ex.Message.Contains("connect"))
            {
                // Логирование ошибки
                return username;
            }
            catch (PrincipalServerDownException)
            {
                // Логирование ошибки
                return username;
            }
            catch (Exception)
            {
                // Логирование ошибки
                return username;
            }
        }
    }
}