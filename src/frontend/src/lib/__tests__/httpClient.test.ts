import { HttpClient } from '../httpClient';
import { ApiError, UnauthorizedError, ValidationError, NotFoundError } from '../errors';

describe('HttpClient Utility Class', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    if (typeof window !== 'undefined') {
      localStorage.clear();
    }
  });

  it('performs GET request and parses JSON response data', async () => {
    const mockData = [{ id: '1', name: 'Phở bò' }];
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ data: mockData }),
    });

    const client = new HttpClient('http://test-api/api/v1');
    const result = await client.get<any[]>('/recipes');

    expect(global.fetch).toHaveBeenCalledWith(
      'http://test-api/api/v1/recipes',
      expect.objectContaining({
        method: 'GET',
        headers: expect.any(Headers),
      })
    );
    expect(result).toEqual(mockData);
  });

  it('attaches auth token from options or localStorage', async () => {
    localStorage.setItem('accessToken', 'mock-token-xyz');

    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ success: true }),
    });

    const client = new HttpClient('http://test-api/api/v1');
    await client.get('/protected-route');

    const lastCallHeaders = (global.fetch as jest.Mock).mock.calls[0][1].headers as Headers;
    expect(lastCallHeaders.get('Authorization')).toBe('Bearer mock-token-xyz');
  });

  it('builds query parameters accurately in URL', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({}),
    });

    const client = new HttpClient('http://test-api/api/v1');
    await client.get('/recipes/search', {
      params: { q: 'phở bò', page: 2, pageSize: 10, empty: undefined },
    });

    const calledUrl = (global.fetch as jest.Mock).mock.calls[0][0];
    expect(calledUrl).toContain('q=ph%E1%BB%9F+b%C3%B2');
    expect(calledUrl).toContain('page=2');
    expect(calledUrl).toContain('pageSize=10');
    expect(calledUrl).not.toContain('empty');
  });

  it('throws UnauthorizedError on 401 response', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 401,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ title: 'Unauthorized', detail: 'Token invalid' }),
    });

    const client = new HttpClient('http://test-api/api/v1');
    await expect(client.get('/secret')).rejects.toThrow(UnauthorizedError);
  });

  it('throws ValidationError with field errors on 400 response with ProblemDetails', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 400,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({
        title: 'Validation failed',
        errors: { Title: ['Title is required'] },
      }),
    });

    const client = new HttpClient('http://test-api/api/v1');
    await expect(client.post('/recipes', {})).rejects.toThrow(ValidationError);
  });

  it('handles 204 No Content response gracefully', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 204,
      headers: new Headers(),
    });

    const client = new HttpClient('http://test-api/api/v1');
    const res = await client.delete('/recipes/123');
    expect(res).toEqual({});
  });
});
