using System.Reflection;
using System.Text.Json;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.PersistirResultadoOnboarding;
using Icarus.Domain.Entities;
using Icarus.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Icarus.Infrastructure.UnitTests.Messaging;

public sealed class OnboardingResultConsumerTests
{
    [Theory]
    [InlineData("j-onb")]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task IdentificadorInvalidoVaiParaDlqSemBloquearResultadoSeguinte(string identificador)
    {
        var repositorio = new TrabalhosSimulados();
        using var servicos = CriarServicos(repositorio);
        await using var conexoes = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions()));
        using var consumidor = CriarConsumidor(conexoes, servicos);
        var canal = DispatchProxy.Create<IChannel, CanalSimulado>();
        var simulacao = (CanalSimulado)canal;
        var carga = CriarCarga(identificador);

        await ProcessarAsync(consumidor, canal, carga, 1);
        Assert.Equal(0, repositorio.Consultas);
        Assert.Collection(simulacao.Operacoes,
            operacao => Assert.Equal("publicar:icarus.output.onboarding.dlq", operacao),
            operacao => Assert.Equal("confirmar:1", operacao));
        Assert.Equal(carga, Assert.Single(simulacao.CargasPublicadas));

        // Um resultado válido para trabalho ausente deve ser reconhecido normalmente.
        await ProcessarAsync(consumidor, canal, CriarCarga(Guid.NewGuid().ToString()), 2);
        Assert.Equal(1, repositorio.Consultas);
        Assert.Equal("confirmar:2", simulacao.Operacoes.Last());
        Assert.Single(simulacao.CargasPublicadas);
    }

    [Fact]
    public async Task FalhaAoPublicarNaDlqPreservaEntregaSemConfirmacao()
    {
        using var servicos = CriarServicos(new TrabalhosSimulados());
        await using var conexoes = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions()));
        using var consumidor = CriarConsumidor(conexoes, servicos);
        var canal = DispatchProxy.Create<IChannel, CanalSimulado>();
        var simulacao = (CanalSimulado)canal;
        simulacao.FalharPublicacao = true;

        await Assert.ThrowsAsync<IOException>(() => ProcessarAsync(consumidor, canal, CriarCarga("j-onb"), 1));

        Assert.Equal("publicar:icarus.output.onboarding.dlq", Assert.Single(simulacao.Operacoes));
    }

    private static ServiceProvider CriarServicos(TrabalhosSimulados repositorio)
        => new ServiceCollection().AddSingleton(new PersistirResultadoOnboardingUseCase(
            repositorio, null!, null!, NullLogger<PersistirResultadoOnboardingUseCase>.Instance)).BuildServiceProvider();

    private static OnboardingResultConsumer CriarConsumidor(RabbitMqConnectionProvider conexoes, ServiceProvider servicos)
        => new(conexoes, Options.Create(new RabbitMqOptions { MaxTentativas = 0 }),
            servicos.GetRequiredService<IServiceScopeFactory>(), NullLogger<OnboardingResultConsumer>.Instance);

    private static Task ProcessarAsync(OnboardingResultConsumer consumidor, IChannel canal, byte[] carga, ulong entrega)
    {
        var entrada = new BasicDeliverEventArgs("teste", entrega, false, "icarus.output", "onboarding",
            new BasicProperties(), carga, CancellationToken.None);
        var metodo = typeof(OnboardingResultConsumer).GetMethod("ProcessarMensagemAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)metodo.Invoke(consumidor, [canal, entrada, CancellationToken.None])!;
    }

    private static byte[] CriarCarga(string identificador)
    {
        var planejamento = new
        {
            titulo = "Planejamento de teste",
            area_da_vida = "ESTUDOS_E_APRENDIZADO",
            dificuldade = "FACIL",
            prazo_inicio = "2026-11-09"
        };
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            job_id = identificador,
            user_id = Guid.NewGuid().ToString(),
            epic = planejamento,
            campaign = planejamento,
            missions = Array.Empty<object>()
        });
    }

    public class CanalSimulado : DispatchProxy
    {
        public List<string> Operacoes { get; } = [];
        public List<byte[]> CargasPublicadas { get; } = [];
        public bool FalharPublicacao { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IChannel.BasicPublishAsync))
            {
                Operacoes.Add($"publicar:{args![1]}");
                if (FalharPublicacao) return new ValueTask(Task.FromException(new IOException("Confirmação do broker indisponível.")));
                CargasPublicadas.Add(((ReadOnlyMemory<byte>)args[4]!).ToArray());
                return ValueTask.CompletedTask;
            }
            if (targetMethod?.Name == nameof(IChannel.BasicAckAsync))
            {
                Operacoes.Add($"confirmar:{args![0]}");
                return ValueTask.CompletedTask;
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed class TrabalhosSimulados : ITrabalhoOnboardingRepository
    {
        public int Consultas { get; private set; }

        public Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Consultas++;
            return Task.FromResult<TrabalhoOnboarding?>(null);
        }

        public Task AdicionarAsync(TrabalhoOnboarding trabalho, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
