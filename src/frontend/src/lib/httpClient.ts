/**
 * Type-Safe HTTP Client Utility for Culinary Blog
 * Handles base URL configuration, request/response interceptors, auth tokens, and structured error throwing
 */

import {
  ApiError,
  NetworkError,
  TimeoutError,
  UnauthorizedError,
  ForbiddenError,
  NotFoundError,
  ValidationError,
  ProblemDetails,
} from './errors';

export const API_BASE_URL =
  (typeof window === 'undefined' ? process.env.API_INTERNAL_URL : undefined) ||
  process.env.NEXT_PUBLIC_API_URL ||
  'http://localhost:5080/api/v1';

export interface HttpRequestOptions extends Omit<RequestInit, 'body'> {
  params?: Record<string, string | number | boolean | undefined | null>;
  token?: string;
  body?: unknown;
  timeoutMs?: number;
}

export interface HttpResponse<T> {
  data: T;
  status: number;
  headers: Headers;
  ok: boolean;
}

export class HttpClient {
  private baseURL: string;
  private defaultTimeout: number;

  constructor(baseURL: string = API_BASE_URL, defaultTimeout = 15000) {
    this.baseURL = baseURL;
    this.defaultTimeout = defaultTimeout;
  }

  /**
   * Retrieve current stored access token safely
   */
  private getAuthToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('accessToken');
    }
    return null;
  }

  /**
   * Build complete URL with query parameters
   */
  private buildUrl(path: string, params?: Record<string, string | number | boolean | undefined | null>): string {
    const isAbsolute = path.startsWith('http://') || path.startsWith('https://');
    let url = isAbsolute ? path : `${this.baseURL}${path.startsWith('/') ? path : `/${path}`}`;

    if (params) {
      const searchParams = new URLSearchParams();
      for (const [key, val] of Object.entries(params)) {
        if (val !== undefined && val !== null) {
          searchParams.append(key, String(val));
        }
      }
      const qs = searchParams.toString();
      if (qs) {
        url += (url.includes('?') ? '&' : '?') + qs;
      }
    }

    return url;
  }

  /**
   * Core request dispatcher with interceptors
   */
  public async request<T = unknown>(path: string, options: HttpRequestOptions = {}): Promise<T> {
    const { params, token, body, timeoutMs = this.defaultTimeout, headers: customHeaders, ...restOptions } = options;
    const url = this.buildUrl(path, params);

    // Request Interceptor: headers & auth
    const headers = new Headers(customHeaders);
    if (!headers.has('Accept')) {
      headers.set('Accept', 'application/json');
    }

    const authToken = token || this.getAuthToken();
    if (authToken && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${authToken}`);
    }

    let serializedBody: BodyInit | undefined = undefined;
    if (body !== undefined && body !== null) {
      if (typeof FormData !== 'undefined' && body instanceof FormData) {
        serializedBody = body;
        // Do not set Content-Type header so browser sets multipart boundary
      } else if (typeof body === 'string' || body instanceof Blob) {
        serializedBody = body;
      } else {
        if (!headers.has('Content-Type')) {
          headers.set('Content-Type', 'application/json');
        }
        serializedBody = JSON.stringify(body);
      }
    }

    // Timeout controller
    const controller = new AbortController();
    const timeoutTimer = setTimeout(() => controller.abort(), timeoutMs);

    try {
      const response = await fetch(url, {
        ...restOptions,
        headers,
        body: serializedBody,
        signal: controller.signal,
      });

      clearTimeout(timeoutTimer);

      // Handle 204 No Content
      if (response.status === 204) {
        return {} as T;
      }

      // Check Content-Type for JSON parsing
      const contentType = response.headers.get('content-type') || '';
      const isJson = contentType.includes('application/json') || contentType.includes('application/problem+json');

      let parsedData: any = null;
      if (isJson) {
        parsedData = await response.json().catch(() => null);
      } else {
        parsedData = await response.text().catch(() => null);
      }

      // Response Interceptor: Error handling
      if (!response.ok) {
        const status = response.status;
        const problem = typeof parsedData === 'object' && parsedData !== null ? (parsedData as Partial<ProblemDetails>) : {};

        const title = problem.title || `Lỗi HTTP ${status}`;
        const detail = problem.detail || (typeof parsedData === 'string' ? parsedData : undefined);

        if (problem.errors && Object.keys(problem.errors).length > 0) {
          throw new ValidationError(problem.errors, title, detail, parsedData);
        }

        switch (status) {
          case 400:
            throw new ValidationError({}, title, detail, parsedData);
          case 401:
            throw new UnauthorizedError(detail);
          case 403:
            throw new ForbiddenError(detail);
          case 404:
            throw new NotFoundError(detail);
          default:
            throw new ApiError(status, title, detail, problem.code, parsedData);
        }
      }

      // Automatically unwrap standard API wrapper { data: T } if present
      if (parsedData && typeof parsedData === 'object' && 'data' in parsedData && Object.keys(parsedData).length <= 3) {
        return parsedData.data as T;
      }

      return parsedData as T;
    } catch (err: any) {
      clearTimeout(timeoutTimer);

      if (err.name === 'AbortError') {
        throw new TimeoutError(timeoutMs);
      }

      if (err instanceof ApiError || err.name === 'ValidationError' || err.name === 'UnauthorizedError') {
        throw err;
      }

      if (err instanceof TypeError && err.message.toLowerCase().includes('fetch')) {
        throw new NetworkError();
      }

      throw err;
    }
  }

  // --------------------------------------------------------------------
  // Convenience HTTP Verbs
  // --------------------------------------------------------------------

  public get<T>(path: string, options?: Omit<HttpRequestOptions, 'method'>): Promise<T> {
    return this.request<T>(path, { ...options, method: 'GET' });
  }

  public post<T>(path: string, body?: unknown, options?: Omit<HttpRequestOptions, 'method' | 'body'>): Promise<T> {
    return this.request<T>(path, { ...options, method: 'POST', body });
  }

  public put<T>(path: string, body?: unknown, options?: Omit<HttpRequestOptions, 'method' | 'body'>): Promise<T> {
    return this.request<T>(path, { ...options, method: 'PUT', body });
  }

  public patch<T>(path: string, body?: unknown, options?: Omit<HttpRequestOptions, 'method' | 'body'>): Promise<T> {
    return this.request<T>(path, { ...options, method: 'PATCH', body });
  }

  public delete<T>(path: string, options?: Omit<HttpRequestOptions, 'method'>): Promise<T> {
    return this.request<T>(path, { ...options, method: 'DELETE' });
  }
}

/**
 * Singleton Default API Client
 */
export const httpClient = new HttpClient();
export default httpClient;
