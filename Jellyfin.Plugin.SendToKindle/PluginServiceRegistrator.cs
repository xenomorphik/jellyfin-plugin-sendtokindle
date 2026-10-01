using Jellyfin.Plugin.SendToKindle.Services;
using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller;

namespace Jellyfin.Plugin.SendToKindle
{
    /// <summary>
    /// Registers services into the Jellyfin Dependency Injection container.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHostedService<WebInjectorService>();
            serviceCollection.AddTransient<IKindleExtractionService, KindleExtractionService>();
            serviceCollection.AddTransient<ISmtpDeliveryService, SmtpDeliveryService>();
        }
    }
}
