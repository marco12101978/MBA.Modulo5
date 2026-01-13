using Core.Identidade;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class JwtConfigTests
{
    [Fact]
    public void AddJwtConfiguration_deve_lancar_quando_JwksUrl_nao_configurada()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:Secret"] = "x",
                ["AppSettings:Emissor"] = "x",
                ["AppSettings:ValidoEm"] = "x"
            })
            .Build();

        var act = () => services.AddJwtConfiguration(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*AutenticacaoJwksUrl*");
    }

    [Fact]
    public void AddJwtConfiguration_deve_registrar_autenticacao_quando_config_ok()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutenticacaoJwksUrl"] = "https://example/jwks.json",
                ["AppSettings:Secret"] = "secret",
                ["AppSettings:Emissor"] = "issuer",
                ["AppSettings:ValidoEm"] = "audience"
            })
            .Build();

        services.AddJwtConfiguration(config);
        var provider = services.BuildServiceProvider();

        provider.GetService<IAuthenticationService>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddJwtConfiguration_deve_lancar_quando_secao_AppSettings_ausente()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Action act = () => services.AddJwtConfiguration(config);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddJwtConfiguration_deve_lancar_quando_JwksUrl_nula_ou_vazia(string? url)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutenticacaoJwksUrl"] = url,
                ["AppSettings:Secret"] = "secret",
                ["AppSettings:Emissor"] = "issuer",
                ["AppSettings:ValidoEm"] = "aud"
            })
            .Build();

        Action act = () => services.AddJwtConfiguration(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AutenticacaoJwksUrl não foi configurado corretamente*");
    }

    [Fact]
    public void AddJwtConfiguration_deve_registrar_scheme_provider_quando_config_ok()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutenticacaoJwksUrl"] = "https://example/jwks.json",
                ["AppSettings:Secret"] = "secret",
                ["AppSettings:Emissor"] = "issuer",
                ["AppSettings:ValidoEm"] = "aud"
            })
            .Build();

        services.AddJwtConfiguration(config);
        var provider = services.BuildServiceProvider();

        provider.GetService<IAuthenticationSchemeProvider>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddJwtConfiguration_nao_deve_lancar_quando_JwksUrl_whitespace_pois_IsNullOrEmpty_nao_pega()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutenticacaoJwksUrl"] = "   ",
                ["AppSettings:Secret"] = "secret",
                ["AppSettings:Emissor"] = "issuer",
                ["AppSettings:ValidoEm"] = "aud"
            })
            .Build();

        Action act = () => services.AddJwtConfiguration(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddJwtConfiguration_deve_lancar_quando_AppSettings_nao_bindar()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:"] = ""
            })
            .Build();

        Action act = () => services.AddJwtConfiguration(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AutenticacaoJwksUrl não foi configurado corretamente*");
    }

    [Fact]
    public async Task AddJwtConfiguration_deve_registrar_JwtBearer_scheme_quando_config_ok()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutenticacaoJwksUrl"] = "https://example/jwks.json",
                ["AppSettings:Secret"] = "secret",
                ["AppSettings:Emissor"] = "issuer",
                ["AppSettings:ValidoEm"] = "aud"
            })
            .Build();

        services.AddJwtConfiguration(config);

        var provider = services.BuildServiceProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme);

        scheme.Should().NotBeNull();
        scheme!.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void AddJwtConfiguration_deve_configurar_JwtBearerOptions_com_JwksOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutenticacaoJwksUrl"] = "https://example/jwks.json",
                ["AppSettings:Secret"] = "secret",
                ["AppSettings:Emissor"] = "issuer",
                ["AppSettings:ValidoEm"] = "aud"
            })
            .Build();

        services.AddJwtConfiguration(config);
        var sp = services.BuildServiceProvider();

        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var opt = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        opt.RequireHttpsMetadata.Should().BeFalse();
        opt.SaveToken.Should().BeTrue();
        opt.BackchannelHttpHandler.Should().NotBeNull();
    }
}
