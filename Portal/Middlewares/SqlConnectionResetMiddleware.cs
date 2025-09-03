using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;

namespace Portal.Middlewares
{
    public class SqlConnectionResetMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SqlConnectionResetMiddleware> _logger;

        public SqlConnectionResetMiddleware(RequestDelegate next, ILogger<SqlConnectionResetMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (SqlException ex)
            {
                // Логируем
                _logger.LogError(ex, "SQL ошибка при обработке запроса. Код ошибки: {ErrorCode}", ex.Number);

                // Если ошибка связана с сетью/подключением — сброс пула
                if (IsTransientError(ex))
                {
                    _logger.LogWarning("Временная ошибка SQL. Сбрасываю пул подключений...");
                    SqlConnection.ClearAllPools();
                }

                // Возвращаем 500 пользователю
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Ошибка при обращении к базе данных.");
            }
        }

        private bool IsTransientError(SqlException ex)
        {
            // Частые временные ошибки
            int[] transientErrorNumbers = {
                -2,     // Timeout expired
                53,     // SQL Server not found
                4060,   // Cannot open database
                10054,  // Connection reset by peer
                10053,  // Connection aborted
                18456   // Login failed
            };

            return transientErrorNumbers.Contains(ex.Number);
        }
    }
}
