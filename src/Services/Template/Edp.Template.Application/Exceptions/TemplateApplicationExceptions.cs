namespace Edp.Template.Application.Exceptions;

public sealed class TemplateNotFoundException(string message) : Exception(message);

public sealed class TemplateVersionNotFoundException(string message) : Exception(message);

public sealed class TemplateCodeConflictException(string message) : Exception(message);

public sealed class TemplateConcurrencyConflictException(string message) : Exception(message);

public sealed class TemplateOperationNotAllowedException(string message) : Exception(message);

public sealed class TemplateFileValidationException(string message) : Exception(message);
