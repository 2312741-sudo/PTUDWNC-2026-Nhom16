import {
  AppError,
  ApiError,
  ValidationError,
  UnauthorizedError,
  ForbiddenError,
  NotFoundError,
  NetworkError,
  TimeoutError,
  parseApiError,
  getErrorMessage,
  isApiError,
  isValidationError,
} from '../errors';

describe('Error Classes & Utilities', () => {
  it('AppError creates base application error', () => {
    const err = new AppError('Có lỗi xảy ra');
    expect(err.message).toBe('Có lỗi xảy ra');
    expect(err.name).toBe('AppError');
    expect(err.isAppError).toBe(true);
  });

  it('ApiError sets status code and details', () => {
    const err = new ApiError(500, 'Server Error', 'Database down', 'DB_ERROR');
    expect(err.status).toBe(500);
    expect(err.title).toBe('Server Error');
    expect(err.detail).toBe('Database down');
    expect(err.code).toBe('DB_ERROR');
    expect(isApiError(err)).toBe(true);
  });

  it('ValidationError parses field-level errors correctly', () => {
    const errors = {
      Email: ['Email không hợp lệ.', 'Email đã tồn tại.'],
      Password: ['Mật khẩu tối thiểu 8 ký tự.'],
    };
    const err = new ValidationError(errors, 'Validation failed');
    expect(err.status).toBe(400);
    expect(isValidationError(err)).toBe(true);
    expect(err.getFieldError('Email')).toBe('Email không hợp lệ.');
    expect(err.getFieldError('password')).toBe('Mật khẩu tối thiểu 8 ký tự.');
    expect(err.getFieldError('Unknown')).toBeUndefined();

    const allMsgs = err.getAllErrorMessages();
    expect(allMsgs.length).toBe(3);
    expect(allMsgs[0]).toContain('Email: Email không hợp lệ.');
  });

  it('Specialized HTTP errors set appropriate status codes', () => {
    expect(new UnauthorizedError().status).toBe(401);
    expect(new ForbiddenError().status).toBe(403);
    expect(new NotFoundError().status).toBe(404);
  });

  it('NetworkError and TimeoutError instantiate properly', () => {
    const netErr = new NetworkError();
    expect(netErr.name).toBe('NetworkError');

    const timeoutErr = new TimeoutError(5000);
    expect(timeoutErr.name).toBe('TimeoutError');
    expect(timeoutErr.message).toContain('5s');
  });

  it('parseApiError correctly converts ProblemDetails object', () => {
    const problem = {
      status: 400,
      title: 'Dữ liệu sai',
      errors: {
        Name: ['Tên không được để trống'],
      },
    };
    const parsed = parseApiError(problem);
    expect(isValidationError(parsed)).toBe(true);
    if (isValidationError(parsed)) {
      expect(parsed.getFieldError('Name')).toBe('Tên không được để trống');
    }
  });

  it('getErrorMessage extracts friendly string across error types', () => {
    expect(getErrorMessage('Chuỗi lỗi')).toBe('Chuỗi lỗi');
    expect(getErrorMessage(new Error('Lỗi chung'))).toBe('Lỗi chung');
    expect(getErrorMessage(new ApiError(404, 'Không tìm thấy', 'Bài viết không tồn tại'))).toBe(
      'Bài viết không tồn tại'
    );
    expect(getErrorMessage(null, 'Mặc định')).toBe('Mặc định');
  });
});
