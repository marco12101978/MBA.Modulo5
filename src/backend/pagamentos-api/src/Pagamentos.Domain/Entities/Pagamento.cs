using Core.DomainObjects;
using Plataforma.Educacao.Core.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Pagamentos.Domain.Entities
{
    public class Pagamento : Entidade, IRaizAgregacao
    {
        public Guid CobrancaCursoId { get; set; }
        public Guid AlunoId { get; set; }
        public string Status { get; set; }
        public decimal Valor { get; set; }

        private string _numeroCartaoCriptografado;
        private string _numeroCvvCriptografado;

        public string NomeCartao { get; set; }
        public string ExpiracaoCartao { get; set; }

        public Transacao Transacao { get; set; }


        public void DefinirNumeroCartao(string numeroCartao, string encryptionKey)
        {

            if (string.IsNullOrWhiteSpace(numeroCartao))
                throw new DomainException("Número do cartão é obrigatório.");

            var digits = new string(numeroCartao.Where(char.IsDigit).ToArray());

            if (digits.Length < 13 || digits.Length > 19)
                throw new DomainException("Número do cartão inválido.");

            _numeroCartaoCriptografado = Criptografar(digits, encryptionKey);
        }

        public string ObterNumeroCartao(string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(_numeroCartaoCriptografado))
                throw new InvalidOperationException("Cartão não informado.");

            return Descriptografar(_numeroCartaoCriptografado, encryptionKey);
        }

        public void DefinirNumeroCVV(string cvv, string encryptionKey)
        {
            ValidarCvvTransitorio(cvv);
            _numeroCvvCriptografado = Criptografar(cvv, encryptionKey);
        }

        public string ObterNumeroCVV(string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(_numeroCvvCriptografado))
                throw new InvalidOperationException("CVV não informado.");

            return Descriptografar(_numeroCvvCriptografado, encryptionKey);
        }

        public string ObterUltimos4()
        {
            var digits = ObterNumeroCartaoChaveInterna();
            return digits[^4..];
        }

        private string ObterNumeroCartaoChaveInterna()
        {
            return new string(
                Descriptografar(_numeroCartaoCriptografado, "X2pt0")
            );
        }

        public void ValidarCvvTransitorio(string cvv)
        {
            if (string.IsNullOrWhiteSpace(cvv) ||
                !cvv.All(char.IsDigit) ||
                cvv.Length < 3 || cvv.Length > 4)
                throw new DomainException("CVV inválido.");
        }


        private static string Criptografar(string plain, string key)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.IV = new byte[16];

            var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(plain);
            var result = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            return Convert.ToBase64String(result);
        }

        private static string Descriptografar(string cipher, string key)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.IV = new byte[16];

            var decryptor = aes.CreateDecryptor();
            var bytes = Convert.FromBase64String(cipher);
            var result = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            return Encoding.UTF8.GetString(result);
        }


    }
}
