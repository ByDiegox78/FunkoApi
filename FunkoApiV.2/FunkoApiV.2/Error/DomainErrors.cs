using System.Collections.Generic;

namespace FunkoApiV._2.Error;

public sealed record NotFound(string Message) : DomainError(Message) {
    public static NotFound FromId(int id, string resource = "Recurso") =>
        new($"{resource} con ID {id} no encontrado");
}
public sealed record ValidationError(string Message) : DomainError(Message);