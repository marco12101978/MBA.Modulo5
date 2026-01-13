using Conteudo.Domain.Entities;
using Conteudo.Domain.ValueObjects;
using Conteudo.Infrastructure.Data;
using Conteudo.Infrastructure.Repositories;
using Conteudo.UnitTests.Repositories.Infra;
using Core.Communication.Filters;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace Conteudo.UnitTests.Repositories;

public class CursoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ConteudoDbContext _ctx;
    private readonly CursoRepository _repo;

    public CursoRepositoryTests()
    {
        var (options, conn) = EfSqliteInMemoryFactory.Create();
        _conn = conn;
        _ctx = new ConteudoDbContext(options);
        _repo = new CursoRepository(_ctx);
    }

    private static ConteudoProgramatico VO() =>
        new("r", "d", "o", "pr", "pa", "m", "r", "a", "b");

    private Curso NovoCurso(string nome = "Curso A", Guid? cat = null)
        => new(nome, 100, VO(), 10, "Básico", "Instrutor", 20, "", null, cat);

    private static Curso NovoCurso(string nome, bool ativo)
    {
        // Ajuste para o teu factory/ctor real
        var curso = new Curso(
            nome: nome,
            valor: 100m,
            conteudoProgramatico: new ConteudoProgramatico("Resumo é obrigatório", "Descrição é obrigatória", "Objetivos são obrigatórios", "","","","","",""),
            duracaoHoras: 10,
            nivel: "Básico",
            instrutor: "Instrutor",
            vagasMaximas: 10,
            imagemUrl: "http://img",
            validoAte: null,
            categoriaId: null
        );

        SetAtivo(curso, ativo);
        return curso;
    }

    [Fact]
    public async Task Adicionar_ObterPorId_includeAulas_e_Deletar_devem_funcionar()
    {
        var c = NovoCurso("Curso A");
        _ctx.Cursos.Add(c); await _ctx.SaveChangesAsync();

        // + 1 aula para testar include
        var aula = new Aula(c.Id, "A1", "d", 1, 30, "url", "Vídeo", true, "");
        _ctx.Aulas.Add(aula); await _ctx.SaveChangesAsync();

        var comAulas = await _repo.ObterPorIdAsync(c.Id, includeAulas: true);
        comAulas!.Aulas.Should().HaveCount(1);

        c.AtualizarInformacoes("Curso A+", 150, VO(), 12, "Interm", "Instrutor", 25);
        await _repo.Atualizar(c);
        await _repo.UnitOfWork.Commit();

        (await _repo.ObterPorIdAsync(c.Id))!.Nome.Should().Be("Curso A+");

        await _repo.Deletar(c);
        await _repo.UnitOfWork.Commit();
        (await _repo.ObterPorIdAsync(c.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ObterTodos_e_ObterPorCategoriaId_devem_respeitar_flags_e_incluir_categoria()
    {
        var cat = new Categoria("Arquitetura", "d", "#123456", "", 0);
        _ctx.Categorias.Add(cat); await _ctx.SaveChangesAsync();

        var c1 = NovoCurso("C1", cat.Id);
        var c2 = NovoCurso("C2", cat.Id);
        var c3 = NovoCurso("C3", null);
        _ctx.Cursos.AddRange(c1, c2, c3); await _ctx.SaveChangesAsync();

        var todos = (await _repo.ObterTodosAsync()).ToList();
        todos.Should().HaveCount(3);

        var porCat = (await _repo.ObterPorCategoriaIdAsync(cat.Id)).ToList();
        porCat.Select(x => x.Nome).Should().BeEquivalentTo(new[] { "C1", "C2" });
        porCat.Should().OnlyContain(x => x.Categoria != null); // Include(Categoria)
    }

    [Fact]
    public async Task ExistePorNome_deve_considerar_excludeId()
    {
        var c1 = NovoCurso("Nome Único A"); var c2 = NovoCurso("Nome Único B");
        _ctx.Cursos.AddRange(c1, c2); await _ctx.SaveChangesAsync();

        (await _repo.ExistePorNomeAsync("Nome Único A")).Should().BeTrue();
        (await _repo.ExistePorNomeAsync("Nome Único A", excludeId: c1.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ObterTodosAsync_filter_deve_filtrar_por_query_e_ativos()
    {
        var ativoMatch = NovoCurso(GerarNomeCursoUnico("Curso Clean") , ativo: true);
        var ativoNoMatch = NovoCurso(GerarNomeCursoUnico("Outro Nome"), ativo: true);
        var inativoMatch = NovoCurso(GerarNomeCursoUnico("Clean Architecture 2"), ativo: false);

        _ctx.Cursos.AddRange(ativoMatch, ativoNoMatch, inativoMatch);
        await _ctx.SaveChangesAsync();

        var filter = new CursoFilter
        {
            PageIndex = 1,
            PageSize = 10,
            Query = "Clean",
            Ativos = true,
            IncludeAulas = false
        };

        var res = await _repo.ObterTodosAsync(filter);

        res.Items.Should().ContainSingle();
        res.Items.Single().Id.Should().Be(ativoMatch.Id);
    }

    [Fact]
    public async Task ObterTodosAsync_filter_sem_query_deve_retornar_todos_quando_ativos_false()
    {
        _ctx.Cursos.AddRange(NovoCurso("A", ativo: true), NovoCurso("B", ativo: false));
        await _ctx.SaveChangesAsync();

        var filter = new CursoFilter
        {
            PageIndex = 1,
            PageSize = 10,
            Query = null,
            Ativos = false,
            IncludeAulas = false
        };

        var res = await _repo.ObterTodosAsync(filter);

        res.Items.Should().HaveCount(2);
    }

    private static void SetAtivo(Curso curso, bool ativo)
    {
        var prop = typeof(Curso).GetProperty(nameof(Curso.Ativo), BindingFlags.Instance | BindingFlags.Public);
        var setter = prop!.GetSetMethod(nonPublic: true);
        setter!.Invoke(curso, new object[] { ativo });
    }

    private static string GerarNomeCursoUnico(string? prefixo = null)
    {
        prefixo ??= "Curso";

        return $"{prefixo} - {Guid.NewGuid():N}".Substring(0, 40);
    }


    public void Dispose() => _conn.Dispose();
}
