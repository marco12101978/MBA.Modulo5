namespace Pagamentos.Domain.ValueObjects;
public sealed class Cartao
{
    public string NomeImpresso { get; }
    public string Ultimos4 { get; }
    public string Bandeira { get; } // opcional

    private Cartao(string nomeImpresso, string ultimos4, string bandeira)
    {
        NomeImpresso = nomeImpresso;
        Ultimos4 = ultimos4;
        Bandeira = bandeira;
    }

    public static Cartao Criar(string nomeImpresso, string numeroCartao, string? bandeira = null)
    {
        if (string.IsNullOrWhiteSpace(nomeImpresso))
            throw new ArgumentException("Nome do cartão é obrigatório.", nameof(nomeImpresso));

        if (string.IsNullOrWhiteSpace(numeroCartao))
            throw new ArgumentException("Número do cartão é obrigatório.", nameof(numeroCartao));

        var digits = new string(numeroCartao.Where(char.IsDigit).ToArray());
        if (digits.Length < 13 || digits.Length > 19)
            throw new ArgumentException("Número do cartão inválido.", nameof(numeroCartao));

        var ultimos4 = digits.Length >= 4 ? digits[^4..] : digits;

        return new Cartao(nomeImpresso.Trim(), ultimos4, (bandeira ?? string.Empty).Trim());
    }
}
