using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Icarus.Application.Suporte;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Suporte;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Icarus.Infrastructure.UnitTests.Suporte;

public sealed class RelatosPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    [FatoPostgres]
    public async Task CriaUmaVezMesmoComRepeticaoConcorrenteEConsultaSoParaOTitular()
    {
        var titular = await banco.CriarUsuarioAsync(0);
        var outro = await banco.CriarUsuarioAsync(0);
        using var trello = new TrelloSimulado();
        using var http = Cliente(trello);
        using var servicos = Montar(http);

        var envios = await Task.WhenAll(Enviar(titular, "primeiro"), Enviar(titular, "primeiro"));
        Assert.Equal(envios[0], envios[1]);
        Assert.Equal(1, trello.Criacoes);
        Assert.Equal(StatusRelato.Recebido, envios[0].Status);

        var maisRecente = await Enviar(titular, "segundo");
        var detalheId = envios[0].Id;
        var dataMovimento = new DateTimeOffset(2026, 11, 23, 14, 0, 0, TimeSpan.Zero);
        trello.MoverPrimeiro(dataMovimento);

        using var leitura = servicos.CreateScope();
        var api = leitura.ServiceProvider.GetRequiredService<ISuporte>();
        Assert.Empty(await api.ListarAsync(outro, default));
        Assert.Null(await api.ConsultarAsync(outro, detalheId, default));
        var lista = await api.ListarAsync(titular, default);
        Assert.Equal([maisRecente.Id, detalheId], lista.Select(r => r.Id));
        Assert.Equal(StatusRelato.EmAnalise, lista[1].Status);
        var detalhe = Assert.IsType<DetalheRelato>(await api.ConsultarAsync(titular, detalheId, default));
        Assert.Equal(new EtapaRelato(StatusRelato.EmAnalise, dataMovimento), detalhe.Historico[1]);
        Assert.Equal(StatusRelato.EmAnalise, detalhe.Status);
        await Assert.ThrowsAsync<ChaveEnvioEmUsoException>(() =>
            api.EnviarAsync(titular, "primeiro", "Outro assunto", "Descrição", default));

        async Task<ResumoRelato> Enviar(Guid usuario, string chave)
        {
            using var escopo = servicos.CreateScope();
            return await escopo.ServiceProvider.GetRequiredService<ISuporte>()
                .EnviarAsync(usuario, chave, "Diário não abre", "Tela ficou carregando.", default);
        }
    }

    [FatoPostgres]
    public async Task RecuperaCartaoGravadoPeloTrelloAntesDeUmaRespostaPerdida()
    {
        var titular = await banco.CriarUsuarioAsync(0);
        using var trello = new TrelloSimulado { PerderRespostaDaCriacao = true };
        using var http = Cliente(trello);
        using var servicos = Montar(http);
        using (var escopo = servicos.CreateScope())
        {
            var api = escopo.ServiceProvider.GetRequiredService<ISuporte>();
            await Assert.ThrowsAsync<SuporteIndisponivelException>(() =>
                api.EnviarAsync(titular, "recuperar", "Relato", "Descrição", default));
            Assert.Empty(await api.ListarAsync(titular, default));
        }

        using var outroEscopo = servicos.CreateScope();
        var recuperada = outroEscopo.ServiceProvider.GetRequiredService<ISuporte>();
        var envio = await recuperada.EnviarAsync(titular, "recuperar", "Relato", "Descrição", default);
        Assert.Equal(1, trello.Criacoes);
        Assert.Equal(envio, Assert.Single(await recuperada.ListarAsync(titular, default)));
    }

    private static HttpClient Cliente(HttpMessageHandler transporte) => new(transporte, disposeHandler: false)
    {
        BaseAddress = new Uri("https://api.trello.com/1/")
    };

    private ServiceProvider Montar(HttpClient http)
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = banco.Conexao }).Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddInfrastructure(configuracao);
        services.AddScoped<IcarusDbContext>(_ => banco.NovoContexto());
        services.AddSingleton(new TrelloSuporte(http, Options.Create(new TrelloSuporteOptions
        {
            Chave = "teste", Token = "teste", QuadroId = "quadro", NovosId = "novos",
            EmAnaliseId = "analise", ResolvidosId = "resolvidos"
        }), NullLogger<TrelloSuporte>.Instance));
        return services.BuildServiceProvider();
    }

    private sealed class TrelloSimulado : HttpMessageHandler
    {
        private readonly List<CartaoSuporte> _cartoes = [];
        private readonly List<MudancaLista> _mudancas = [];

        public bool PerderRespostaDaCriacao { get; init; }
        public int Criacoes => _cartoes.Count;

        public void MoverPrimeiro(DateTimeOffset data)
        {
            _cartoes[0] = _cartoes[0] with { IdList = "analise" };
            _mudancas.Add(new("movimento", data, new(new("analise"))));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("OAuth", request.Headers.Authorization?.Scheme);
            var caminho = request.RequestUri!.AbsolutePath;
            if (caminho == "/1/cards" && request.Method == HttpMethod.Post)
            {
                var corpo = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
                Assert.Equal("novos", corpo.GetProperty("idList").GetString());
                lock (_cartoes)
                {
                    var cartao = new CartaoSuporte(Guid.NewGuid().ToString("N")[..24], "novos", "quadro",
                        corpo.GetProperty("desc").GetString());
                    _cartoes.Add(cartao);
                    return PerderRespostaDaCriacao ? new(HttpStatusCode.ServiceUnavailable) : Json(cartao);
                }
            }

            lock (_cartoes)
            {
                if (caminho == "/1/boards/quadro/cards/all") return Json(_cartoes.ToArray());
                var id = caminho.Split('/')[3];
                if (caminho.EndsWith("/actions", StringComparison.Ordinal)) return Json(_mudancas.ToArray());
                return Json(_cartoes.Single(c => c.Id == id));
            }
        }

        private static HttpResponseMessage Json<T>(T valor) => new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(valor)
        };
    }
}
