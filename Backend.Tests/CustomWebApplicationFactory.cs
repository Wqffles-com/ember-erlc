using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var contentRoot = GetContentRootPath();
        builder.UseContentRoot(contentRoot);
    }

    private static string GetContentRootPath()
    {
        var assemblyLocation = typeof(Program).Assembly.Location;
        var solutionRoot = Path.GetFullPath(
            Path.Combine(assemblyLocation, "..", "..", "..", "..", ".."));

        var contentRoot = Path.Combine(solutionRoot, "Backend");

        if (!File.Exists(Path.Combine(contentRoot, "Backend.csproj")))
            throw new InvalidOperationException(
                $"Could not locate Backend.csproj. Resolved content root: {contentRoot}");

        return contentRoot;
    }
}
