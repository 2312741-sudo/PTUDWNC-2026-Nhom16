/**
 * Custom Error Classes & Error Handling Utilities for Culinary Blog Frontend
 * Implements structured error parsing for RFC 7807 / RFC 9457 Problem Details
 */

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  code?: string;
  [key: string]: unknown;
}

/**
 * Base Application Error
 */
export class AppError extends Error {
  public readonly isAppError = true;

  constructor(message: string) {
    super(message);
    this.name = 'AppError';
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

/**
 * API Error representing an HTTP or Server error
 */
export class ApiError extends AppError {
  public readonly status: number;
  public readonly title: string;
  public readonly detail?: string;
  public readonly code?: string;
  public readonly raw?: unknown;

  constructor(
    status: number,
    title: string,
    detail?: string,
    code?: string,
    raw?: unknown
  ) {
    super(detail || title || `Yêu cầu thất bại với mã trạng thái ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.code = code;
    this.raw = raw;
  }
}

/**
 * Validation Error with field-level error messages (HTTP 400 / 422)
 */
export class ValidationError extends ApiError {
  public readonly errors: Record<string, string[]>;

  constructor(
    errors: Record<string, string[]> = {},
    title = 'Dữ liệu không hợp lệ',
    detail?: string,
    raw?: unknown
  ) {
    super(400, title, detail, 'VALIDATION_ERROR', raw);
    this.name = 'ValidationError';
    this.errors = errors;
  }

  /**
   * Get first error message for a specific form field (case-insensitive)
   */
  public getFieldError(field: string): string | undefined {
    if (this.errors[field] && this.errors[field].length > 0) {
      return this.errors[field][0];
    }
    const target = field.toLowerCase();
    for (const [k, v] of Object.entries(this.errors)) {
      if (k.toLowerCase() === target && v && v.length > 0) {
        return v[0];
      }
    }
    return undefined;
  }

  /**
   * Get all errors formatted as an array of strings
   */
  public getAllErrorMessages(): string[] {
    const messages: string[] = [];
    for (const [field, errs] of Object.entries(this.errors)) {
      for (const err of errs) {
        messages.push(`${field}: ${err}`);
      }
    }
    return messages.length > 0 ? messages : [this.message];
  }
}

/**
 * Unauthorized Error (HTTP 401)
 */
export class UnauthorizedError extends ApiError {
  constructor(detail = 'Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.') {
    super(401, 'Chưa xác thực', detail, 'UNAUTHORIZED');
    this.name = 'UnauthorizedError';
  }
}

/**
 * Forbidden Error (HTTP 403)
 */
export class ForbiddenError extends ApiError {
  constructor(detail = 'Bạn không có quyền thực hiện thao tác này.') {
    super(403, 'Từ chối truy cập', detail, 'FORBIDDEN');
    this.name = 'ForbiddenError';
  }
}

/**
 * Not Found Error (HTTP 404)
 */
export class NotFoundError extends ApiError {
  constructor(detail = 'Tài nguyên được yêu cầu không tồn tại hoặc đã bị xóa.') {
    super(404, 'Không tìm thấy', detail, 'NOT_FOUND');
    this.name = 'NotFoundError';
  }
}

/**
 * Network / Offline Error
 */
export class NetworkError extends AppError {
  constructor(message = 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.') {
    super(message);
    this.name = 'NetworkError';
  }
}

/**
 * Request Timeout Error
 */
export class TimeoutError extends AppError {
  constructor(timeoutMs: number) {
    super(`Yêu cầu đã quá thời gian chờ (${timeoutMs / 1000}s). Vui lòng thử lại.`);
    this.name = 'TimeoutError';
  }
}

// ----------------------------------------------------------------------
// Type Guards & Utility Helpers
// ----------------------------------------------------------------------

export function isAppError(error: unknown): error is AppError {
  return error instanceof AppError;
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function isValidationError(error: unknown): error is ValidationError {
  return error instanceof ValidationError;
}

/**
 * Parse any error or HTTP response body into a strongly-typed ApiError / AppError
 */
export function parseApiError(error: unknown, fallbackStatus = 500): AppError {
  if (isAppError(error)) {
    return error;
  }

  // Network / fetch TypeError
  if (error instanceof TypeError && error.message.includes('fetch')) {
    return new NetworkError();
  }

  // Check if error is a ProblemDetails object or standard object
  if (typeof error === 'object' && error !== null) {
    const obj = error as Partial<ProblemDetails>;
    const status = typeof obj.status === 'number' ? obj.status : fallbackStatus;
    const title = obj.title || 'Lỗi hệ thống';
    const detail = obj.detail || (typeof (error as any).message === 'string' ? (error as any).message : undefined);

    if (obj.errors && Object.keys(obj.errors).length > 0) {
      return new ValidationError(obj.errors, title, detail, error);
    }

    if (status === 401) {
      return new UnauthorizedError(detail);
    }
    if (status === 403) {
      return new ForbiddenError(detail);
    }
    if (status === 404) {
      return new NotFoundError(detail);
    }

    return new ApiError(status, title, detail, obj.code, error);
  }

  if (typeof error === 'string') {
    return new AppError(error);
  }

  return new AppError('Đã xảy ra lỗi không xác định.');
}

/**
 * Extract a user-friendly error message from any error value
 */
export function getErrorMessage(error: unknown, fallback = 'Đã xảy ra lỗi. Vui lòng thử lại.'): string {
  if (!error) return fallback;

  if (error instanceof ValidationError) {
    const msgs = error.getAllErrorMessages();
    return msgs.length > 0 ? msgs.join('. ') : error.message;
  }

  if (error instanceof ApiError) {
    return error.detail || error.title || error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === 'string') {
    return error;
  }

  return fallback;
}
