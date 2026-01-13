using Mapster;
using Pagamentos.Application.Mappings;
using Pagamentos.Application.ViewModels;
using Pagamentos.Domain.Entities;
using Pagamentos.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pagamentos.UnitTests.Mappings;
public class PagamentosMappingConfigTests
{
    [Fact]
    public void Register_deve_configurar_mapeamentos_e_permitir_adapt_de_pagamento_transacao_e_colecoes()
    {

        var config = new TypeAdapterConfig();
        new PagamentosMappingConfig().Register(config);

        var pagamento = new Pagamentos.Domain.Entities.Pagamento
        {
            CobrancaCursoId = Guid.NewGuid(),
            AlunoId = Guid.NewGuid(),
            Status = "Pago",
            Valor = 123.45m
        };

        var transacao = new Transacao
        {
            CobrancaCursoId = pagamento.CobrancaCursoId,
            PagamentoId = pagamento.Id,
            Total = pagamento.Valor,
            StatusTransacao = StatusTransacao.Pago,
            Pagamento = pagamento
        };

        pagamento.Transacao = transacao;


        var vm = pagamento.Adapt<PagamentoViewModel>(config);


        vm.Should().NotBeNull();
        vm.Id.Should().Be(pagamento.Id);
        vm.CobrancaCursoId.Should().Be(pagamento.CobrancaCursoId);
        vm.AlunoId.Should().Be(pagamento.AlunoId);
        vm.Status.Should().Be(pagamento.Status);
        vm.Valor.Should().Be(pagamento.Valor);
        vm.Transacao.Should().NotBeNull();
        vm.Transacao.Id.Should().Be(transacao.Id);


        var pagamentoVolta = vm.Adapt<Pagamentos.Domain.Entities.Pagamento>(config);


        pagamentoVolta.Should().NotBeNull();
        pagamentoVolta.CobrancaCursoId.Should().Be(pagamento.CobrancaCursoId);
        pagamentoVolta.AlunoId.Should().Be(pagamento.AlunoId);
        pagamentoVolta.Status.Should().Be(pagamento.Status);
        pagamentoVolta.Valor.Should().Be(pagamento.Valor);


        var transacaoVm = transacao.Adapt<TransacaoViewModel>(config);


        transacaoVm.Should().NotBeNull();
        transacaoVm.Id.Should().Be(transacao.Id); 


        var listaVm = new[] { pagamento }.Adapt<IEnumerable<PagamentoViewModel>>(config).ToList();

        listaVm.Should().HaveCount(1);
        listaVm[0].Id.Should().Be(pagamento.Id);
    }
}
