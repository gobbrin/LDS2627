using System.Security.Cryptography;
using System.Text;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;

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
    ValidadorEmailAcademico validadorEmail) : IAutenticacaoService
{
    // P-02: validade da ligação de confirmação.
    private const int HorasDeValidadeConfirmacao = 24;

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

        // API-11: o 409 revela que o email já tem conta (enumeração de contas), ao contrário do reenvio.
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
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        // DC-43 / IN-03 (fica para m4): a Brevo recusar o envio lança HttpRequestException, que não é
        // apanhada aqui e chega ao cliente como 500, em vez do 503.
        try
        {
            await servicoEmail.EnviarConfirmacaoAsync(normalizado, token, ct);
        }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
        {
            // Não marcar como confirmado nem revelar o token; permitir reenvio após configurar SMTP.
            return new Erro(
                TipoErro.ServicoIndisponivel,
                "Conta criada, mas o envio do email falhou. Contacta o suporte para reenviar a confirmação.");
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

        // Só a ligação mais recente serve: uma ligação antiga deixa de confirmar a conta.
        await confirmacoes.RemoverPorUsarAsync(utilizador.Id, ct);

        var token = NovoToken();

        confirmacoes.Adicionar(new ConfirmacaoConta
        {
            UtilizadorId = utilizador.Id,
            TokenHash = Sha256(token),
            ExpiraEm = agora.AddHours(HorasDeValidadeConfirmacao)
        });

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        try
        {
            await servicoEmail.EnviarConfirmacaoAsync(normalizado, token, ct);
        }
        catch (Exception ex)
        {
            // A falha não é revelada ao cliente (resposta neutra); só fica no Debug, não no log da aplicação.
            System.Diagnostics.Debug.WriteLine("[SMTP ERRO] " + ex);
        }

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

        if (utilizador is null || !hashPalavraPasse.Verificar(utilizador, palavraPasse))
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

        // Limpeza: um token expirado já é recusado pela validação da data.
        await tokensRevogados.RemoverExpiradosAsync(agora, ct);

        if (!string.IsNullOrWhiteSpace(tokenDispositivo) && utilizadorId is Guid id)
            await dispositivos.DesativarAsync(id, tokenDispositivo.Trim(), ct);

        tokensRevogados.Adicionar(new TokenRevogado { Jti = jti, ExpiraEm = expiraEm ?? agora.AddMinutes(60) });

        await unidadeDeTrabalho.ConfirmarAsync(ct);
    }

    /// <inheritdoc />
    public Task<bool> TokenRevogadoAsync(string jti, CancellationToken ct) => tokensRevogados.EstaRevogadoAsync(jti, ct);
}
