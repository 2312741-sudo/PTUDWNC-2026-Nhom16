namespace CulinaryBlog.Application.Common;
public sealed class NotFoundException(string resource, Guid id) : Exception($"Không tìm thấy {resource} có ID {id}.");
public sealed class ConflictException(string message) : Exception(message);
public sealed class RequestValidationException(string message) : Exception(message);
