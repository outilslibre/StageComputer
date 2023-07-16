using StageComputer.Utils;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using OpenCloseDays;
using FluentValidation;
using StageComputer.Pages;
using Polly;
using Polly.Extensions.Http;
using Winton.AspNetCore.Seo;
using Winton.AspNetCore.Seo.Sitemaps;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
/* TODO
 * explication markdown
 * edit explication with basic auth
 * matomo config
 * fluentvalidation
 * privacy : https://matomo.org/privacy-policy/
 */
#if DEBUG
services.AddRazorPages().AddRazorRuntimeCompilation();
#else
services.AddRazorPages();
#endif
services.AddHttpClient();
services.AddSingleton<PublicHolidaysService>();
services.AddSingleton<OpenCloseDaysService>();
services.AddSingleton<ExplainationManager>();
services.AddLocalization();
services.AddScoped<IValidator<StageComputerModel>, StageComputerModelValidator>();
services.AddScoped<PlafondSecuFetcher>();
services.AddHttpClient("fetcher").AddPolicyHandler(
	HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(6,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
services.AddMemoryCache();
services.AddSeoWithDefaultRobots(
	o =>
	{
		o.Urls = new[]{
			new SitemapUrlOptions()
			{
				Priority = 1,
				RelativeUrl = "/"
			},
			new SitemapUrlOptions()
			{
				Priority = (decimal)0.5,
				RelativeUrl = "/Privacy"
			},
		};
	});
services.AddProgressiveWebApp();
var app = builder.Build();

const string supportedLanguageCode = "fr-FR";
var supportedCultures = new[]{
	new CultureInfo(supportedLanguageCode)
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
	DefaultRequestCulture = new RequestCulture(supportedLanguageCode),
	SupportedCultures = supportedCultures,
	FallBackToParentCultures = false
});
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture(supportedLanguageCode);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
