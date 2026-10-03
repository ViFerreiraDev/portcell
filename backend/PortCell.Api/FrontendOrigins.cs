namespace PortCell.Api;

public static class FrontendOrigins
{
    public static string[] Allowed(IConfiguration configuration)
    {
        var origins = new[] { configuration["Frontend:Origin"] ?? "http://localhost:5173" }
            .Concat(configuration.GetSection("Frontend:AllowedOrigins").GetChildren().Select(item => item.Value!));
        return origins.Select(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https") || uri.AbsolutePath != "/" ||
                uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
                throw new InvalidOperationException("Configure Frontend:Origin e Frontend:AllowedOrigins com origens HTTP/HTTPS exatas.");
            return uri.GetLeftPart(UriPartial.Authority);
        }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
