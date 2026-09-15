using System.Net;
using System.Net.Sockets;

namespace isgDotnet.Helpers;

public static class ClientIpHelper
{
    public static string? Resolve(HttpContext? http)
    {
        if (http is null)
        {
            return null;
        }

        // Gerçek bir reverse proxy (IIS/Nginx/vs) yok, uygulama direkt Kestrel ile
        // aynı ağdaki client'lara açık. Bu yüzden X-Forwarded-For gibi header'lara
        // güvenmiyoruz - istemci tarafından kolayca spoof edilebilir ve önceki
        // "local IP'ler trusted proxy'dir" mantığı, aynı ağdaki tüm cihazların
        // aynı (yanlış) IP olarak görünmesine sebep oluyordu.
        return Format(http.Connection.RemoteIpAddress);
    }

    private static IPAddress? Normalize(IPAddress? address)
    {
        if (address is null)
        {
            return null;
        }

        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private static string? Format(IPAddress? address)
    {
        return Normalize(address)?.ToString();
    }
}