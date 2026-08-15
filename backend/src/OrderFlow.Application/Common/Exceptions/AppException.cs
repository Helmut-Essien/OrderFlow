namespace OrderFlow.Application.Common.Exceptions;

/// <summary>
/// Application-layer failure mapped to an HTTP status by <c>ExceptionHandlingMiddleware</c>.
/// </summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Missing or invalid credentials / JWT. Maps to 401.</summary>
public sealed class UnauthorizedAppException(string message) : AppException(message);

/// <summary>Unique-constraint or duplicate-resource conflict (e.g. SKU, email). Maps to 409.</summary>
public sealed class ConflictAppException(string message) : AppException(message);

/// <summary>
/// Optimistic concurrency failure (stale <c>expectedVersion</c> or insufficient stock). Maps to 409 with code <c>concurrency</c>.
/// </summary>
public sealed class ConcurrencyAppException(string message) : AppException(message);

/// <summary>Requested entity was not found in the current shop. Maps to 404.</summary>
public sealed class NotFoundAppException(string message) : AppException(message);

/// <summary>Authenticated but not allowed (plan cap, role). Maps to 403.</summary>
public sealed class ForbiddenAppException(string message) : AppException(message);
