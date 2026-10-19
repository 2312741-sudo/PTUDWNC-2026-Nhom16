/**
 * Lớp tiện ích chuẩn hóa xử lý lỗi cho toàn bộ Frontend Culinary Blog
 * Hỗ trợ định dạng RFC 7807 Problem Details từ Backend .NET
 */

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export class ApiException extends Error {
  public status: number;
  public code?: string;
  public errors?: Record<string, string[]>;
  public problemDetails?: ProblemDetails;

  constructor(message: string, status: number = 500, code?: string, errors?: Record<string, string[]>, problemDetails?: ProblemDetails) {
    super(message);
    this.name = 'ApiException';
    this.status = status;
    this.code = code;
    this.errors = errors;
    this.problemDetails = problemDetails;
  }
}

/**
 * Trích xuất thông điệp lỗi thân thiện với người dùng từ phản hồi API hoặc đối tượng ngoại lệ
 */
export function parseApiError(error: unknown): string {
  if (!error) return 'Đã xảy ra lỗi không xác định. Vui lòng thử lại.';

  if (typeof error === 'string') return error;

  if (error instanceof ApiException) {
    return error.message;
  }

  if (error instanceof Error) {
    // Phát hiện lỗi kết nối mạng
    if (error.message.includes('fetch failed') || error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
      return 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại kết nối mạng của bạn.';
    }
    return error.message;
  }

  // Đối tượng JSON Problem Details
  if (typeof error === 'object') {
    const errObj = error as Record<string, any>;
    if (errObj.detail && typeof errObj.detail === 'string') return errObj.detail;
    if (errObj.message && typeof errObj.message === 'string') return errObj.message;
    if (errObj.title && typeof errObj.title === 'string') return errObj.title;
    if (errObj.error && typeof errObj.error === 'string') return errObj.error;
  }

  return 'Đã xảy ra lỗi trong quá trình xử lý yêu cầu.';
}

/**
 * Kiểm tra xem một ngoại lệ có phải là lỗi xác thực (401 Unauthorized) hay không
 */
export function isUnauthorizedError(error: unknown): boolean {
  if (error instanceof ApiException) {
    return error.status === 401;
  }
  if (typeof error === 'object' && error !== null) {
    const errObj = error as Record<string, any>;
    return errObj.status === 401;
  }
  return false;
}

/**
 * Kiểm tra xem một ngoại lệ có phải là lỗi cấm quyền (403 Forbidden) hay không
 */
export function isForbiddenError(error: unknown): boolean {
  if (error instanceof ApiException) {
    return error.status === 403;
  }
  if (typeof error === 'object' && error !== null) {
    const errObj = error as Record<string, any>;
    return errObj.status === 403;
  }
  return false;
}
