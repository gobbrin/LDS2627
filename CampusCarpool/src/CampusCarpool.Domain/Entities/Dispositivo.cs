namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Dispositivo Android registado para receber notificações push (RF-F006-10, RF-F006-11).
/// </summary>
/// <remarks>
/// RF-F006-18 pede que o dispositivo deixe de receber push quando o utilizador termina a sessão;
/// o fim de sessão ainda não desativa o dispositivo (DEF-BE-021, fica para m4; depende da D-020).
/// </remarks>
public sealed class Dispositivo
{
    /// <summary>Identificador do registo.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Dono do dispositivo.</summary>
    public Guid UtilizadorId { get; set; }

    /// <summary>Token FCM (Firebase Cloud Messaging) do dispositivo.</summary>
    public string Token { get; set; } = "";

    /// <summary>Só os dispositivos ativos recebem push.</summary>
    public bool Ativo { get; set; } = true;
}
