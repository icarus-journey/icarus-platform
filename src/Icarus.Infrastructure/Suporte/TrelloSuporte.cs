using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Icarus.Application.Suporte;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Icarus.Infrastructure.Suporte;

public sealed class TrelloSuporteOptions
{
    public const string Secao = "SuporteTrello";
    public string Chave { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string QuadroId { get; set; } = string.Empty;
    public string NovosId { get; set; } = string.Empty;
    public string EmAnaliseId { get; set; } = string.Empty;
    public string ResolvidosId { get; set; } = string.Empty;
}

public sealed record CartaoSuporte(string Id, string IdList, string? IdBoard, string? Desc);
public sealed record MudancaLista(string Id, DateTimeOffset Date, DadosMudanca? Data);
public sealed record DadosMudanca(ListaSuporte? ListAfter);
public sealed record ListaSuporte(string Id);

public sealed partial class TrelloSuporte(HttpClient http, IOptions<TrelloSuporteOptions> opcoes, ILogger<TrelloSuporte> logger)
{
    private readonly TrelloSuporteOptions _opcoes = opcoes.Value;

    public async Task<CartaoSuporte[]> ListarAsync(bool incluirDescricao, CancellationToken cancelamento)
    {
        var campos = incluirDescricao ? "id,idList,desc" : "id,idList";
        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"boards/{_opcoes.QuadroId}/cards/all?fields={campos}");
        return (await EnviarAsync<CartaoSuporte[]>(pedido, cancelamento))!;
    }

    public async Task<CartaoSuporte> CriarAsync(string titulo, string descricao, CancellationToken cancelamento)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "cards")
        {
            Content = JsonContent.Create(new { name = titulo, desc = descricao, idList = _opcoes.NovosId })
        };
        var cartao = (await EnviarAsync<CartaoSuporte>(pedido, cancelamento))!;
        if (string.IsNullOrWhiteSpace(cartao.Id) || cartao.Id.Length > 24)
            throw new SuporteIndisponivelException();
        return cartao;
    }

    public async Task<CartaoSuporte?> ConsultarAsync(string cartaoId, CancellationToken cancelamento)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"cards/{Uri.EscapeDataString(cartaoId)}?fields=id,idList,idBoard");
        var cartao = await EnviarAsync<CartaoSuporte>(pedido, cancelamento, aceitarAusente: true);
        return cartao?.IdBoard == _opcoes.QuadroId ? cartao : null;
    }

    public async Task<IReadOnlyList<MudancaLista>> HistoricoAsync(string cartaoId, CancellationToken cancelamento)
    {
        const int tamanhoPagina = 100;
        var mudancas = new List<MudancaLista>();
        var antes = string.Empty;
        while (true)
        {
            using var pedido = new HttpRequestMessage(HttpMethod.Get,
                $"cards/{Uri.EscapeDataString(cartaoId)}/actions?filter=updateCard:idList&fields=id,date,data&limit={tamanhoPagina}{antes}");
            var pagina = (await EnviarAsync<MudancaLista[]>(pedido, cancelamento))!;
            mudancas.AddRange(pagina);
            if (pagina.Length < tamanhoPagina) return mudancas;
            var proxima = $"&before={Uri.EscapeDataString(pagina[^1].Id)}";
            if (antes == proxima) throw new SuporteIndisponivelException();
            antes = proxima;
        }
    }

    public StatusRelato Status(string? listaId)
    {
        if (string.IsNullOrWhiteSpace(listaId)) return StatusRelato.Indisponivel;
        if (listaId == _opcoes.NovosId) return StatusRelato.Recebido;
        if (listaId == _opcoes.EmAnaliseId) return StatusRelato.EmAnalise;
        if (listaId == _opcoes.ResolvidosId) return StatusRelato.Resolvido;
        return StatusRelato.Indisponivel;
    }

    private async Task<T?> EnviarAsync<T>(HttpRequestMessage pedido, CancellationToken cancelamento, bool aceitarAusente = false)
        where T : class
    {
        if (new[] { _opcoes.Chave, _opcoes.Token, _opcoes.QuadroId, _opcoes.NovosId, _opcoes.EmAnaliseId, _opcoes.ResolvidosId }
            .Any(string.IsNullOrWhiteSpace))
            throw new SuporteIndisponivelException();

        pedido.Headers.Authorization = new AuthenticationHeaderValue("OAuth",
            $"oauth_consumer_key=\"{_opcoes.Chave}\", oauth_token=\"{_opcoes.Token}\"");
        try
        {
            using var resposta = await http.SendAsync(pedido, cancelamento);
            if (aceitarAusente && resposta.StatusCode == HttpStatusCode.NotFound) return null;
            if (!resposta.IsSuccessStatusCode)
            {
                RespostaInvalida(logger, (int)resposta.StatusCode);
                throw new SuporteIndisponivelException();
            }
            return await resposta.Content.ReadFromJsonAsync<T>(cancelamento)
                ?? throw new SuporteIndisponivelException();
        }
        catch (Exception falha) when (falha is HttpRequestException or JsonException
            || falha is OperationCanceledException && !cancelamento.IsCancellationRequested)
        {
            FalhaNaComunicacao(logger, falha.GetType().Name);
            throw new SuporteIndisponivelException();
        }
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "A API do Trello respondeu com HTTP {Status} para o suporte.")]
    private static partial void RespostaInvalida(ILogger logger, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível ler a resposta do Trello ({Tipo}).")]
    private static partial void FalhaNaComunicacao(ILogger logger, string tipo);
}
