namespace CampusCarpool.Domain.Entities;

public sealed class TokenRevogado
{
    public Guid Id { get; set; } = Guid.NewGuid(); public string Jti { get; set; } = ""; public DateTime ExpiraEm { get; set; }
}
