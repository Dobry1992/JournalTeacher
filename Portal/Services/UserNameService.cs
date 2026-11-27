using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.DirectoryServices.AccountManagement;
using Portal.Repository;

namespace Portal.Services
{
    public class UserNameService
    {
        // Кэш логин -> DisplayName из AD. Обращение к контроллеру домена дорогое
        // (создание PrincipalContext + LDAP-запрос на каждый вызов), а ФИО сотрудника
        // меняется редко. Успешные результаты кешируются навсегда (до перезапуска),
        // неудачные - на 10 минут, чтобы при недоступном AD не платить таймаутом за каждый чих.
        private static readonly ConcurrentDictionary<string, CacheEntry> Cache =
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan NegativeCacheLifetime = TimeSpan.FromMinutes(10);

        public static void ClearCache() => Cache.Clear();

        private sealed class CacheEntry
        {
            public string DisplayName;
            public DateTime CachedAt;
            public bool FromAd;
        }

        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserNameService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetDisplayName()
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return "Unknown";

            if (Cache.TryGetValue(username, out var entry) &&
                (entry.FromAd || DateTime.Now - entry.CachedAt < NegativeCacheLifetime))
            {
                return entry.DisplayName;
            }

            string displayName;
            bool fromAd = false;

            try
            {
                // Как в коде выставления отметки: полное имя (DisplayName) из AD,
                // при любой неудаче - исходный логин.
                using (var context = new PrincipalContext(ContextType.Domain, AD.root))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null && !string.IsNullOrWhiteSpace(user.DisplayName))
                    {
                        displayName = user.DisplayName;
                        fromAd = true;
                    }
                    else
                    {
                        displayName = username;
                    }
                }
            }
            catch
            {
                displayName = username;
            }

            Cache[username] = new CacheEntry
            {
                DisplayName = displayName,
                CachedAt = DateTime.Now,
                FromAd = fromAd
            };

            return displayName;
        }
    }
}
