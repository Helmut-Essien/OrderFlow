namespace OrderFlow.Application.Common.Exceptions;

public abstract class AppException(string message) : Exception(message);

public sealed class UnauthorizedAppException(string message) : AppException(message);

public sealed class ConflictAppException(string message) : AppException(message);

public sealed class NotFoundAppException(string message) : AppException(message);

public sealed class ForbiddenAppException(string message) : AppException(message);
