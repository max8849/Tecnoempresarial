namespace AccessControl.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key) : base($"{entity} '{key}' no fue encontrado.") { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "No tienes permiso para realizar esta acción.") : base(message) { }
}

public class ValidationAppException : Exception
{
    public ValidationAppException(string message) : base(message) { }
}
