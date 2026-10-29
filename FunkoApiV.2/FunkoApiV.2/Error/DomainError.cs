namespace FunkoApiV._2.Error;

public abstract record DomainError(string Message) {
    public override string ToString() => $"{GetType().Name}: {Message}";
}