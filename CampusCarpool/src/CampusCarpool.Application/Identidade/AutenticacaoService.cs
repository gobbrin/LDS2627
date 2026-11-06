using System.Security.Cryptography;
using System.Text;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Application.Identidade;

/// <summary>Implementação de <see cref="IAutenticacaoService"/>.</summary>
/// <remarks>
/// Os tokens de confirmação são 32 bytes aleatórios; na BD só fica o SHA-256 (ver <see cref="ConfirmacaoConta"/>).
/// </remarks>
public sealed class AutenticacaoService(
    IRepositorioUtilizadores utilizadores,
    IRepositorioConfirmacoesConta confirmacoes,
    IRepositorioTokensRevogados tokensRevogados,
    IRepositorioDispositivos dispositivos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IServicoEmail servicoEmail,
    IHashPalavraPasse hashPalavraPasse,
    IGeradorTokens geradorTokens,
    ValidadorEmailAcademico validadorEmail,
    ILogger<AutenticacaoService> logger) : IAutenticacaoService
{
    private static readonly Action<ILogger, Exception?> LogFalhaEmail = LoggerMessage.Define(
        LogLevel.Error, new EventId(3001, nameof(LogFalhaEmail)), "Falha no envio do email de confirmação");

    // P-02: validade da ligação de confirmação.
    private const int HorasDeValidadeConfirmacao = 24;

    /// <summary>Tempo que uma revogação fica depois de o token expirar: tem de exceder o <c>ClockSkew</c> de 30 s do JwtBearer.</summary>
    private static readonly TimeSpan MargemDepoisDeExpirar = TimeSpan.FromMinutes(1);

    // P-08 / RF-F001-05: mínimo de 8 caracteres, com pelo menos uma letra e um dígito.
    private static bool PalavraPasseValida(string? palavraPasse) =>
        palavraPasse is { Length: >= 8 } && palavraPasse.Any(char.IsLetter) && palavraPasse.Any(char.IsDigit);

    // Emails comparados sempre em minúsculas e sem espaços.
    private static string Normalizar(string email) => email.Trim().ToLowerInvariant();

    private static string Sha256(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string NovoToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <inheritdoc />
    public async Task<Resultado> RegistarAsync(string nome, string email, string palavraPasse, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 100 ||
            string.IsNullOrWhiteSpace(email) || !PalavraPasseValida(palavraPasse))
        {
            return new Erro(TipoErro.DadosInvalidos, "Dados de registo inválidos.");
        }

        var normalizado = Normalizar(email);

        if (!validadorEmail.Permitido(normalizado) || normalizado.Length > 255)
            return new Erro(TipoErro.DadosInvalidos, "É necessário um email académico permitido.");

        // RF-F001-04 pede este erro de conflito, apesar de revelar que o email já tem conta.
        if (await utilizadores.ExisteEmailAsync(normalizado, ct))
            return new Erro(TipoErro.Conflito, "Email já registado.");

        var utilizador = Utilizador.Registar(nome.Trim(), normalizado);
        utilizador.PasswordHash = hashPalavraPasse.Gerar(utilizador, palavraPasse);
        utilizadores.Adicionar(utilizador);

        var token = NovoToken();

        confirmacoes.Adicionar(new ConfirmacaoConta
        {
            UtilizadorId = utilizador.Id,
            TokenHash = Sha256(token),
            ExpiraEm = DateTime.UtcNow.AddHours(HorasDeValidadeConfirmacao)
        });

        // A conta fica gravada antes do envio: se o email falhar, o utilizador pode pedir o reenvio.
        if (await unidadeDeTrabalho.TentarConfirmarAsync(ct) is { } recusa)
        {
            // Dois registos simultâneos passam ambos a verificação acima; o índice único recusa o segundo.
            unidadeDeTrabalho.DescartarAlteracoes();

            if (await utilizadores.ExisteEmailAsync(normalizado, ct))
                return new Erro(TipoErro.Conflito, "Email já registado.");

            throw new InvalidOperationException("A base de dados recusou o registo.", recusa);
        }

        try
        {
            await servicoEmail.EnviarConfirmacaoAsync(normalizado, token, ct);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or InvalidOperationException ||
            (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Serviço de Email recusou, excedeu o tempo ou está por configurar. A conta fica por confirmar.
            LogFalhaEmail(logger, ex);

            // O reenvio só é aceite um minuto depois da última ligação.
            return new Erro(
                TipoErro.ServicoIndisponivel,
                "Conta criada, mas o envio do email falhou. Aguarda um minuto e pede o reenvio da ligação de confirmação.");
        }

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> ReenviarConfirmacaoAsync(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255)
            return new Erro(TipoErro.DadosInvalidos, "Email inválido.");

        var normalizado = Normalizar(email);

        if (!validadorEmail.Permitido(normalizado))
            return Resultado.Sucesso;

        var utilizador = await utilizadores.ObterPorEmailAsync(normalizado, ct);

        if (utilizador is null || utilizador.EmailConfirmado)
            return Resultado.Sucesso;

        // Limita reenvios a um por minuto, aproveitando o instante de criação do token.
        // Não há data de criação (DO-10): como a validade é sempre 24 h, ExpiraEm - 24 h é a criação.
        var agora = DateTime.UtcNow;
        var ultimo = await confirmacoes.ObterUltimaExpiracaoAsync(utilizador.Id, ct);

        if (ultimo.HasValue && ultimo.Value > agora.AddHours(HorasDeValidadeConfirmacao).AddMinutes(-1))
            return Resultado.Sucesso;

        var token = NovoToken();
        var tokenHash = Sha256(token);

        confirmacoes.Adicionar(new ConfirmacaoConta
        {
            UtilizadorId = utilizador.Id,
            TokenHash = tokenHash,
            ExpiraEm = agora.AddHours(HorasDeValidadeConfirmacao)
        });

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        try
        {
            await servicoEmail.EnviarConfirmacaoAsync(normalizado, token, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // A falha não é revelada ao cliente (resposta neutra) e a ligação anterior continua a servir.
            LogFalhaEmail(logger, ex);
            return Resultado.Sucesso;
        }

        // Só a ligação mais recente serve; as anteriores só deixam de valer depois de a nova seguir.
        await confirmacoes.InvalidarAnterioresAsync(utilizador.Id, tokenHash, agora, ct);
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> ConfirmarContaAsync(string token, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var confirmacao = await confirmacoes.ObterPorHashAsync(Sha256(token), ct);

        // RF-F001-08: o utilizador fica a saber o motivo, para saber se deve iniciar sessão ou pedir nova ligação.
        if (confirmacao is null)
            return new Erro(TipoErro.DadosInvalidos, "Ligação de confirmação inválida. Pede uma nova ligação.");

        if (confirmacao.ConsumidoEm is not null)
            return new Erro(TipoErro.DadosInvalidos, "Esta ligação já foi usada. Se a conta está confirmada, inicia sessão.");

        if (!confirmacao.EstaValida(agora))
            return new Erro(TipoErro.DadosInvalidos, "Esta ligação expirou. Pede uma nova ligação de confirmação.");

        var utilizador = await utilizadores.ObterParaAlterarAsync(confirmacao.UtilizadorId, ct);

        if (utilizador is null)
            return new Erro(TipoErro.NaoEncontrado, "A conta desta ligação já não existe.");

        utilizador.Confirmar();
        confirmacao.Utilizar(agora);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado<TokenAcesso>> IniciarSessaoAsync(string email, string palavraPasse, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterPorEmailAsync(Normalizar(email), ct);

        if (utilizador is null)
        {
            // O mesmo custo de verificar um hash: o tempo de resposta não revela se o email tem conta.
            hashPalavraPasse.Gerar(new Utilizador(), palavraPasse);
            return new Erro(TipoErro.NaoAutenticado, "Credenciais inválidas.");
        }

        if (!hashPalavraPasse.Verificar(utilizador, palavraPasse))
            return new Erro(TipoErro.NaoAutenticado, "Credenciais inválidas.");

        if (!utilizador.EmailConfirmado)
            return new Erro(TipoErro.Proibido, "Conta por confirmar.");

        return geradorTokens.Gerar(utilizador.Id);
    }

    /// <inheritdoc />
    public async Task TerminarSessaoAsync(
        string jti,
        DateTime? expiraEm,
        Guid? utilizadorId,
        string? tokenDispositivo,
        CancellationToken ct)
    {
        var agora = DateTime.UtcNow;

        // Limpeza: passada a margem, um token expirado já é recusado pela validação da data.
        await tokensRevogados.RemoverExpiradosAsync(agora - MargemDepoisDeExpirar, ct);

        if (!string.IsNullOrWhiteSpace(tokenDispositivo) && utilizadorId is Guid id)
            await dispositivos.DesativarAsync(id, tokenDispositivo.Trim(), ct);

        tokensRevogados.Adicionar(new TokenRevogado { Jti = jti, ExpiraEm = expiraEm ?? agora.AddMinutes(60) });

        if (await unidadeDeTrabalho.TentarConfirmarAsync(ct) is { } recusa)
        {
            // Dois pedidos com o mesmo token (duplo clique, repetição): o índice único recusa o segundo, e a sessão terminou na mesma.
            unidadeDeTrabalho.DescartarAlteracoes();

            if (!await tokensRevogados.EstaRevogadoAsync(jti, ct))
                throw new InvalidOperationException("A base de dados recusou o fim de sessão.", recusa);
        }
    }

    /// <inheritdoc />
    public Task<bool> TokenRevogadoAsync(string jti, CancellationToken ct) => tokensRevogados.EstaRevogadoAsync(jti, ct);
}
