using DistributedRendering.AME.Frontend.Components;
using DistributedRendering.AME.Frontend.Lib.Configuration;
using DistributedRendering.AME.Frontend.Services;

namespace DistributedRendering.AME.Frontend;

public class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.Services
			.AddOptions<HubCommunicationSettings>()
			.Bind(builder.Configuration.GetSection(nameof(HubCommunicationSettings)));

		// Add services to the container.
		builder.Services.AddRazorComponents()
			.AddInteractiveServerComponents();

		builder.Services
			.AddSingleton<IConfigurationService, ConfigurationService>()
			.AddTransient<IHubCommunicationService, HubCommunicationService>();

		WebApplication app = builder.Build();

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
		app.UseHttpsRedirection();

		app.UseAntiforgery();

		app.MapStaticAssets();
		app.MapRazorComponents<App>()
			.AddInteractiveServerRenderMode();

		app.Run();
	}
}