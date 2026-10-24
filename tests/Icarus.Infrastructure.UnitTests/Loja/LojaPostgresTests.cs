using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarRecompensa;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Application.UseCases.CriarRecompensa;
using Icarus.Application.UseCases.DesfazerResgate;
using Icarus.Application.UseCases.ExcluirRecompensa;
using Icarus.Application.UseCases.ResgatarRecompensa;
using Icarus.Application.UseCases.UtilizarRecompensa;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Icarus.Infrastructure.UnitTests.Loja;

/// <summary>Ative com ICARUS_TESTES_POSTGRES. Cria e remove um banco exclusivo, sem tocar nos dados do projeto.</summary>
public sealed class FatoPostgresAttribute : FactAttribute
{
    public FatoPostgresAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICARUS_TESTES_POSTGRES")))
            Skip = "Defina ICARUS_TESTES_POSTGRES para executar a integração em banco isolado.";
    }
}

public sealed class BancoLoja : IAsyncLifetime
{
    private readonly string _nome = "icarus_loja_testes_" + Guid.NewGuid().ToString("N");
    private string? _administracao;
    public string Conexao { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configuracao = Environment.GetEnvironmentVariable("ICARUS_TESTES_POSTGRES");
        if (string.IsNullOrEmpty(configuracao)) return;
        var conexao = new NpgsqlConnectionStringBuilder(configuracao) { Database = "postgres", Pooling = false };
        _administracao = conexao.ConnectionString;
        await using var administrador = new NpgsqlConnection(_administracao);
        await administrador.OpenAsync();
        await using var criar = new NpgsqlCommand($"CREATE DATABASE \"{_nome}\"", administrador);
        await criar.ExecuteNonQueryAsync();
        conexao.Database = _nome;
        Conexao = conexao.ConnectionString;
        await using var contexto = NovoContexto();
        await contexto.Database.MigrateAsync();
    }

    public IcarusDbContext NovoContexto() => new(new DbContextOptionsBuilder<IcarusDbContext>()
        .UseNpgsql(Conexao).UseSnakeCaseNamingConvention().Options);

    public async Task DisposeAsync()
    {
        if (_administracao is null) return;
        await using var administrador = new NpgsqlConnection(_administracao);
        await administrador.OpenAsync();
        await using var excluir = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_nome}\" WITH (FORCE)", administrador);
        await excluir.ExecuteNonQueryAsync();
    }

    public async Task<Guid> CriarUsuarioAsync(long saldo)
    {
        await using var contexto = NovoContexto();
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Pessoa de teste",
            Email = $"{Guid.NewGuid():N}@teste.local",
            SenhaHash = "hash-apenas-de-teste",
            DataNascimento = new DateOnly(1995, 1, 1),
            SaldoPontos = saldo
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }
}

public sealed class LojaPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static ConsultarLojaUseCase Consulta(IcarusDbContext contexto, int custo = 500)
        => new(new LojaRepository(contexto), new LojaOptions { CustoProtecao = custo });

    private static ResgatarRecompensaUseCase Resgate(IcarusDbContext contexto, int custo = 500)
        => new(new LojaRepository(contexto), contexto, Consulta(contexto, custo), new LojaOptions { CustoProtecao = custo },
            NullLogger<ResgatarRecompensaUseCase>.Instance);

    private async Task<long> CriarAsync(Guid usuarioId, int custo)
    {
        await using var contexto = banco.NovoContexto();
        var caso = new CriarRecompensaUseCase(new LojaRepository(contexto), contexto, Consulta(contexto), NullLogger<CriarRecompensaUseCase>.Instance);
        var loja = await caso.ExecuteAsync(new(usuarioId, usuarioId, "Pausa sem culpa", "Descanso", custo), default);
        return loja.Recompensas[0].Id;
    }

    [FatoPostgres]
    public async Task IsolaContasERejeitaSaldoInsuficienteSemAlterarDados()
    {
        var titular = await banco.CriarUsuarioAsync(50);
        var outro = await banco.CriarUsuarioAsync(300);
        var id = await CriarAsync(titular, 120);
        await using var contexto = banco.NovoContexto();
        var lojaOutro = await Consulta(contexto).ExecuteAsync(new(outro, outro), default);
        Assert.Empty(lojaOutro.Recompensas);
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Consulta(contexto).ExecuteAsync(new(outro, titular), default));
        var invisivel = await Assert.ThrowsAsync<OperacaoLojaException>(() => Resgate(contexto).ExecuteAsync(new(outro, outro, id, Guid.NewGuid()), default));
        Assert.True(invisivel.NaoEncontrado);
        var erro = await Assert.ThrowsAsync<OperacaoLojaException>(() => Resgate(contexto).ExecuteAsync(new(titular, titular, id, Guid.NewGuid()), default));
        Assert.Equal("saldo_insuficiente", erro.Codigo);
        var loja = await Consulta(contexto).ExecuteAsync(new(titular, titular), default);
        Assert.Equal(50, loja.SaldoPontos);
        Assert.Empty(loja.Resgates);
        Assert.Equal(EstadoRecompensa.Disponivel, loja.Recompensas[0].Estado);
        Assert.Empty(await contexto.MovimentacoesPontos.Where(m => m.UsuarioId == titular).ToListAsync());
    }

    [FatoPostgres]
    public async Task ResgataUmaVezEstornaCustoHistoricoEMantemTextosImutaveis()
    {
        var usuario = await banco.CriarUsuarioAsync(300);
        var id = await CriarAsync(usuario, 120);
        var operacao = Guid.NewGuid();
        await using var contexto = banco.NovoContexto();
        var resgate = Resgate(contexto);
        await resgate.ExecuteAsync(new(usuario, usuario, id, operacao), default);
        var repeticao = await resgate.ExecuteAsync(new(usuario, usuario, id, operacao), default);
        Assert.Equal(180, repeticao.SaldoPontos);
        var unidade = Assert.Single(repeticao.Resgates);
        var desfazer = new DesfazerResgateUseCase(new LojaRepository(contexto), contexto, Consulta(contexto), NullLogger<DesfazerResgateUseCase>.Instance);
        await desfazer.ExecuteAsync(new(usuario, usuario, unidade.Id), default);
        var estornoRepetido = await desfazer.ExecuteAsync(new(usuario, usuario, unidade.Id), default);
        Assert.Equal(300, estornoRepetido.SaldoPontos);
        Assert.Equal(EstadoRecompensa.Disponivel, estornoRepetido.Recompensas[0].Estado);
        var editar = new AtualizarRecompensaUseCase(new LojaRepository(contexto), contexto, Consulta(contexto), NullLogger<AtualizarRecompensaUseCase>.Instance);
        var erro = await Assert.ThrowsAsync<OperacaoLojaException>(() => editar.ExecuteAsync(new(usuario, usuario, id, "Outro nome", null, 1), default));
        Assert.Equal("recompensa_imutavel", erro.Codigo);
        Assert.Equal(120, Assert.Single(estornoRepetido.Resgates).Custo);
        Assert.Equal(2, await contexto.MovimentacoesPontos.CountAsync(m => m.UsuarioId == usuario));
        Assert.Equal(0, await contexto.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).SumAsync(m => m.Quantidade));
    }

    [FatoPostgres]
    public async Task ConcorrenciaNaoReutilizaSaldoNemDuplicaResgate()
    {
        var usuario = await banco.CriarUsuarioAsync(150);
        var primeiro = await CriarAsync(usuario, 100);
        var segundo = await CriarAsync(usuario, 100);
        async Task<bool> Tentar(long id)
        {
            await using var contexto = banco.NovoContexto();
            try { await Resgate(contexto).ExecuteAsync(new(usuario, usuario, id, Guid.NewGuid()), default); return true; }
            catch (OperacaoLojaException erro) when (erro.Codigo == "saldo_insuficiente") { return false; }
        }
        var resultados = await Task.WhenAll(Tentar(primeiro), Tentar(segundo));
        Assert.Single(resultados, sucesso => sucesso);
        await using var leitura = banco.NovoContexto();
        var loja = await Consulta(leitura).ExecuteAsync(new(usuario, usuario), default);
        Assert.Equal(50, loja.SaldoPontos);
        Assert.Single(loja.Resgates);
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());

        var outro = await banco.CriarUsuarioAsync(500);
        var item = await CriarAsync(outro, 100);
        var operacao = Guid.NewGuid();
        async Task Repetir()
        {
            await using var contexto = banco.NovoContexto();
            await Resgate(contexto).ExecuteAsync(new(outro, outro, item, operacao), default);
        }
        await Task.WhenAll(Repetir(), Repetir());
        Assert.Equal(400, (await Consulta(leitura).ExecuteAsync(new(outro, outro), default)).SaldoPontos);
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == outro).ToListAsync());
    }

    [FatoPostgres]
    public async Task LimitaProtecoesConcorrentesEMudancaDePrecoNaoAlteraEstorno()
    {
        var usuario = await banco.CriarUsuarioAsync(5000);
        async Task<bool> Comprar()
        {
            await using var contexto = banco.NovoContexto();
            try { await Resgate(contexto).ExecuteAsync(new(usuario, usuario, null, Guid.NewGuid()), default); return true; }
            catch (OperacaoLojaException erro) when (erro.Codigo == "limite_protecoes") { return false; }
        }
        var compras = await Task.WhenAll(Comprar(), Comprar(), Comprar(), Comprar());
        Assert.Equal(3, compras.Count(sucesso => sucesso));
        await using var contexto = banco.NovoContexto();
        var antes = await Consulta(contexto, 900).ExecuteAsync(new(usuario, usuario), default);
        Assert.Equal(3500, antes.SaldoPontos);
        Assert.Equal(3, antes.Protecao.Quantidade);
        Assert.Equal(900, antes.Protecao.Custo);
        Assert.All(antes.Resgates, r => Assert.Equal(500, r.Custo));
        var desfazer = new DesfazerResgateUseCase(new LojaRepository(contexto), contexto, Consulta(contexto, 900), NullLogger<DesfazerResgateUseCase>.Instance);
        var depois = await desfazer.ExecuteAsync(new(usuario, usuario, antes.Resgates[0].Id), default);
        Assert.Equal(4000, depois.SaldoPontos);
        Assert.Equal(2, depois.Protecao.Quantidade);
        Assert.Equal(1, await contexto.Itens.CountAsync(i => i.UsuarioId == usuario && i.ControlaApp == ControlaApp.Sim));
    }

    [FatoPostgres]
    public async Task UsoEIrreversivelEExclusaoPreservaHistorico()
    {
        var usuario = await banco.CriarUsuarioAsync(300);
        var id = await CriarAsync(usuario, 120);
        await using var contexto = banco.NovoContexto();
        var compra = await Resgate(contexto).ExecuteAsync(new(usuario, usuario, id, Guid.NewGuid()), default);
        var resgate = Assert.Single(compra.Resgates);
        var usar = new UtilizarRecompensaUseCase(new LojaRepository(contexto), contexto, Consulta(contexto), NullLogger<UtilizarRecompensaUseCase>.Instance);
        await usar.ExecuteAsync(new(usuario, usuario, resgate.Id), default);
        var repetido = await usar.ExecuteAsync(new(usuario, usuario, resgate.Id), default);
        Assert.Equal(180, repetido.SaldoPontos);
        Assert.Equal(EstadoRecompensa.Utilizada, repetido.Recompensas[0].Estado);
        var desfazer = new DesfazerResgateUseCase(new LojaRepository(contexto), contexto, Consulta(contexto), NullLogger<DesfazerResgateUseCase>.Instance);
        await Assert.ThrowsAsync<OperacaoLojaException>(() => desfazer.ExecuteAsync(new(usuario, usuario, resgate.Id), default));
        var excluir = new ExcluirRecompensaUseCase(new LojaRepository(contexto), contexto, Consulta(contexto), NullLogger<ExcluirRecompensaUseCase>.Instance);
        await Assert.ThrowsAsync<OperacaoLojaException>(() => excluir.ExecuteAsync(new(usuario, usuario, id), default));
        var descartavel = await CriarAsync(usuario, 1);
        var atual = await excluir.ExecuteAsync(new(usuario, usuario, descartavel), default);
        Assert.DoesNotContain(atual.Recompensas, r => r.Id == descartavel);
        Assert.Equal(EstadoRecompensa.Excluida, (await contexto.Itens.SingleAsync(i => i.Id == descartavel)).Estado);
        Assert.Single(atual.Resgates);
    }

    [FatoPostgres]
    public async Task FalhaDePersistenciaReverteDebitoEstadoEResgate()
    {
        var usuario = await banco.CriarUsuarioAsync(300);
        var id = await CriarAsync(usuario, 120);
        await using var contexto = banco.NovoContexto();
        var caso = new ResgatarRecompensaUseCase(new LojaRepository(contexto), new UnidadeComFalha(contexto), Consulta(contexto),
            new LojaOptions(), NullLogger<ResgatarRecompensaUseCase>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.ExecuteAsync(new(usuario, usuario, id, Guid.NewGuid()), default));
        var loja = await Consulta(contexto).ExecuteAsync(new(usuario, usuario), default);
        Assert.Equal(300, loja.SaldoPontos);
        Assert.Equal(EstadoRecompensa.Disponivel, loja.Recompensas[0].Estado);
        Assert.Empty(loja.Resgates);
        Assert.Empty(await contexto.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task CreditoComSaldoDesatualizadoNaoSobrescreveResgate()
    {
        var usuario = await banco.CriarUsuarioAsync(300);
        var id = await CriarAsync(usuario, 120);
        await using var credito = banco.NovoContexto();
        var pessoa = await credito.Usuarios.SingleAsync(u => u.Id == usuario);
        await using var resgate = banco.NovoContexto();
        await Resgate(resgate).ExecuteAsync(new(usuario, usuario, id, Guid.NewGuid()), default);
        pessoa.SaldoPontos += 20;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => credito.SaveChangesAsync());
        Assert.Equal(180, (await Consulta(resgate).ExecuteAsync(new(usuario, usuario), default)).SaldoPontos);
    }

    private sealed class UnidadeComFalha(IcarusDbContext contexto) : IUnitOfWork
    {
        public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
        {
            await contexto.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Falha simulada após gravar, antes do commit.");
        }
    }
}
