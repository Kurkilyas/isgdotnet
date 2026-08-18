using System.Net;
using System.Text.Json;
using InvoiceTrackingSystemBackend.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sunucuda bir hata oluştu: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = (int)HttpStatusCode.InternalServerError;
        string message = "Sunucu kaynaklı bir hata oluştu.";
        string? detailedError = null;

        switch (exception)
        {
            // 1. KAYIT BULUNAMADI (404) - servisin bilerek fırlattığı durum
            case NotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = exception.Message;
                break;

            case UnauthorizedException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;

            // 2. GEÇERSİZ İSTEK / İHLAL EDİLEN İŞ KURALI (400)
            case BadRequestException:
            case ArgumentException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = exception.Message;
                break;

            // 3. ÇAKIŞMA (409) - örn. ileride "bu fatura zaten onaylanmış"
            case ConflictException:
                statusCode = (int)HttpStatusCode.Conflict;
                message = exception.Message;
                break;

            // 4. HARİCİ SİSTEM ERİŞİLEMİYOR (503) - Vega/ERP gibi
            case ExternalServiceException:
                statusCode = (int)HttpStatusCode.ServiceUnavailable;
                message = exception.Message;

                if (_env.IsDevelopment() && exception.InnerException != null)
                {
                    detailedError = exception.InnerException.Message;
                }
                break;

            // 5. DOĞRUDAN SQL BAĞLANTI/SORGU HATASI (503) - Vega gibi salt-okunur
            //    dış veritabanlarına erişilemediğinde servis katmanı özel bir tür
            //    fırlatmamış olsa bile burada yakalanır.
            case SqlException:
            case TimeoutException:
                statusCode = (int)HttpStatusCode.ServiceUnavailable;
                message = "Veritabanına/harici sisteme şu anda ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz.";

                if (_env.IsDevelopment())
                {
                    detailedError = exception.Message;
                }
                break;

            // 6. VERİTABANI GÜNCELLEME HATASI (409) - EF Core yazma işlemleri
            case DbUpdateException:
                statusCode = (int)HttpStatusCode.Conflict;
                message = "Veritabanı işlemi sırasında hata oluştu.";

                if (_env.IsDevelopment() && exception.InnerException != null)
                {
                    detailedError = exception.InnerException.Message;
                }
                break;

            // 7. DİĞER TÜM HATALAR (500)
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;

                if (_env.IsDevelopment())
                {
                    message = exception.Message;
                    if (exception.InnerException != null)
                    {
                        detailedError = exception.InnerException.Message;
                    }
                }
                else
                {
                    message = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
                }
                break;
        }

        context.Response.StatusCode = statusCode;

        var response = new
        {
            success = false,
            statusCode,
            error = message,
            debugInfo = _env.IsDevelopment()
                ? new { detailedError, stackTrace = exception.StackTrace }
                : null
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
