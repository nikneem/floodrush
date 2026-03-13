using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal static class ProjectResourceBuilderExtensions
{
    public static IResourceBuilder<ProjectResource> WithSeedBasicLevelsCommand(
        this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithHttpCommand(
            path: "/api/levels/dev/seed-basic-levels",
            displayName: "Add Basic Levels",
            endpointName: "https",
            commandName: "seed-basic-levels",
            commandOptions: new HttpCommandOptions
            {
                ConfirmationMessage = "Add three basic easy levels to local development storage?"
            });

        return builder;
    }
}
