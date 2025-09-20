namespace Garius.Caepi.Reader.Api.Infrastructure.Extensions
{
    public static class SecurityHeaderExtensions
    {
        public static IApplicationBuilder AddSecurityHeaders(this IApplicationBuilder app, string[]? corsOrigins)
        {
            app.UseSecurityHeaders(policy =>
            {
                policy.AddDefaultSecurityHeaders();
                policy.RemoveServerHeader();

                policy.AddReferrerPolicyNoReferrer();

                policy.AddPermissionsPolicy(builder =>
                {
                    builder.AddGeolocation().None();
                    builder.AddMicrophone().None();
                    builder.AddCamera().None();
                    builder.AddMagnetometer().None();
                    builder.AddGyroscope().None();
                    builder.AddSpeaker().None();
                });

                policy.AddContentSecurityPolicy(builder =>
                {
                    builder.AddDefaultSrc().Self();
                    builder.AddImgSrc().Self().From("data:");
                    builder.AddFontSrc().Self();

                    var connectSrcBuilder = builder.AddConnectSrc().Self();

                    foreach (var origin in corsOrigins)
                        connectSrcBuilder.From(origin);

                    builder.AddScriptSrc()
                        .Self()
                        .WithNonce()
                        .From("https://*.stripe.com")
                        .From("https://*.paypal.com")
                        .From("https://*.google.com")
                        .From("https://*.gstatic.com")
                        .From("https://challenges.cloudflare.com");

                    builder.AddStyleSrc()
                        .Self()
                        .From("https://*.stripe.com");

                    builder.AddFrameSrc().Self()
                        .From("https://*.stripe.com")
                        .From("https://*.paypal.com")
                        .From("https://*.google.com")
                        .From("https://challenges.cloudflare.com");

                    builder.AddFrameAncestors().Self();
                    builder.AddUpgradeInsecureRequests();
                });

                policy.AddContentSecurityPolicyReportOnly(builder =>
                {
                    builder.AddDefaultSrc().Self();
                    builder.AddScriptSrc().Self().From("https://*.stripe.com").From("https://*.paypal.com");
                    builder.AddStyleSrc().Self();
                    builder.AddImgSrc().Self().From("data:");
                    builder.AddConnectSrc().Self();
                    builder.AddFrameSrc().Self().From("https://*.stripe.com").From("https://*.paypal.com");

                    // Endpoint para receber relatórios
                    builder.AddReportTo("csp-endpoint");
                });

                policy.AddContentTypeOptionsNoSniff();
                policy.AddXssProtectionBlock();
            });

            return app;
        }
    }
}
