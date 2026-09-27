/**
 * Mock fetch helper for testing hooks and components that make API calls.
 * Provides utilities to mock fetch() without adding external dependencies (no MSW, no jest-fetch-mock).
 */

interface MockResponse {
  ok: boolean;
  status: number;
  json(): Promise<any>;
}

interface FetchMockOptions {
  status?: number;
  body?: any;
  delay?: number;
}

let mockFetchFn: jest.Mock | null = null;

/**
 * Mock a single fetch call.
 * @param status HTTP status code (default 200)
 * @param body Response body (will be returned by .json())
 * @param delay Optional delay in ms before resolving
 *
 * @example
 * mockFetchOnce(200, { id: 1, name: 'Test' });
 * const data = await fetch('/api/test').then(r => r.json());
 * expect(data.name).toBe('Test');
 */
export function mockFetchOnce(
  status: number = 200,
  body: any = {},
  delay: number = 0
): void {
  const mockFetch = jest.fn(async () => {
    if (delay > 0) {
      await new Promise(resolve => setTimeout(resolve, delay));
    }

    return {
      ok: status >= 200 && status < 300,
      status,
      json: async () => body,
    } as MockResponse;
  });

  global.fetch = mockFetch as any;
  mockFetchFn = mockFetch;
}

/**
 * Mock a sequence of fetch calls.
 * Each call to fetch() will return the next response in the array.
 *
 * @example
 * mockFetchSequence([
 *   { status: 200, body: { items: [] } },
 *   { status: 404, body: { error: 'Not found' } },
 * ]);
 *
 * const first = await fetch('/api/test1').then(r => r.json());
 * const second = await fetch('/api/test2').then(r => r.json());
 */
export function mockFetchSequence(responses: FetchMockOptions[]): void {
  let callCount = 0;

  const mockFetch = jest.fn(async () => {
    const response = responses[callCount % responses.length];
    const { status = 200, body = {}, delay = 0 } = response;

    if (delay > 0) {
      await new Promise(resolve => setTimeout(resolve, delay));
    }

    callCount++;

    return {
      ok: status >= 200 && status < 300,
      status,
      json: async () => body,
    } as MockResponse;
  });

  global.fetch = mockFetch as any;
  mockFetchFn = mockFetch;
}

/**
 * Get the last mock fetch function to make assertions on it.
 * Useful for verifying the URL, method, or request body.
 *
 * @example
 * mockFetchOnce(200, { id: 1 });
 * await someHook.getUser(123);
 * expect(getMockFetch()).toHaveBeenCalledWith('/api/users/123', expect.anything());
 */
export function getMockFetch(): jest.Mock {
  if (!mockFetchFn) {
    throw new Error('No mock fetch set up. Call mockFetchOnce() or mockFetchSequence() first.');
  }
  return mockFetchFn;
}

/**
 * Restore the original fetch behavior (or clear the mock).
 * Call this in afterEach() to clean up after each test.
 *
 * @example
 * afterEach(() => {
 *   restoreFetch();
 * });
 */
export function restoreFetch(): void {
  // Reset to undefined; Jest will handle the rest
  global.fetch = undefined as any;
  mockFetchFn = null;
}

/**
 * Setup auto-cleanup for all tests in a describe block.
 * Call this once at the top of your describe block.
 *
 * @example
 * describe('MyHook', () => {
 *   setupFetchMockCleanup();
 *
 *   it('fetches data', async () => {
 *     mockFetchOnce(200, { data: 'test' });
 *     // ...
 *   });
 * });
 */
export function setupFetchMockCleanup(): void {
  afterEach(() => {
    restoreFetch();
  });
}
