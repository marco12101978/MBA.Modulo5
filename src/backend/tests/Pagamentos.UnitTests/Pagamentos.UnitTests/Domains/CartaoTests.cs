using Pagamentos.Domain.ValueObjects;

namespace Pagamentos.UnitTests.Domains;
public class CartaoTests
{
    [Fact]
    public void Criar_deve_falhar_quando_nome_impresso_vazio()
    {
        var act = () => Cartao.Criar("", "4111 1111 1111 1111", "VISA");
        act.Should().Throw<ArgumentException>()
           .Where(e => e.ParamName == "nomeImpresso");
    }

    [Fact]
    public void Criar_deve_falhar_quando_numero_cartao_vazio()
    {
        var act = () => Cartao.Criar("MARCO ROQUE", "");
        act.Should().Throw<ArgumentException>()
           .Where(e => e.ParamName == "numeroCartao");
    }

    [Theory]
    [InlineData("123")] // < 13 dígitos
    [InlineData("12345678901234567890")] // > 19 dígitos
    public void Criar_deve_falhar_quando_numero_cartao_tamanho_invalido(string numero)
    {
        var act = () => Cartao.Criar("MARCO ROQUE", numero);
        act.Should().Throw<ArgumentException>()
           .Where(e => e.ParamName == "numeroCartao");
    }

    [Fact]
    public void Criar_deve_retornar_ultimos4_somente_digitos_e_trim()
    {
        var c = Cartao.Criar("  MARCO ROQUE  ", "4111-1111-1111-1234", "  VISA  ");

        c.NomeImpresso.Should().Be("MARCO ROQUE");
        c.Ultimos4.Should().Be("1234");
        c.Bandeira.Should().Be("VISA");
    }

    [Fact]
    public void Criar_deve_aceitar_bandeira_nula_e_retornar_vazia()
    {
        var c = Cartao.Criar("MARCO ROQUE", "4111111111111234", bandeira: null);
        c.Bandeira.Should().Be(string.Empty);
    }
}
