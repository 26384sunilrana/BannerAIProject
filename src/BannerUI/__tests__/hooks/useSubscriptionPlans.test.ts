import { renderHook, act } from '@testing-library/react';
import { useSubscriptionPlans } from '@/hooks/useSubscriptionPlans';
import { mockFetchOnce, mockFetchSequence, restoreFetch, setupFetchMockCleanup } from '../helpers/mockFetch';

describe('useSubscriptionPlans', () => {
  setupFetchMockCleanup();

  describe('getAllPlans', () => {
    it('fetches all subscription plans', async () => {
      // Arrange
      const mockResponse = {
        items: [
          { id: '1', name: 'Basic', monthlyPrice: 9.99, annualPrice: 99.99, status: 'Active' },
          { id: '2', name: 'Premium', monthlyPrice: 29.99, annualPrice: 299.99, status: 'Active' },
        ],
        total: 2,
      };

      mockFetchOnce(200, mockResponse);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      let response;
      await act(async () => {
        response = await result.current.getAllPlans();
      });

      // Assert
      expect(response).toEqual(mockResponse);
      expect(response.items).toHaveLength(2);
      expect(result.current.loading).toBe(false);
    });

    it('handles API error gracefully', async () => {
      // Arrange
      mockFetchOnce(500);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      await act(async () => {
        try {
          await result.current.getAllPlans();
        } catch (err) {
          // Expected
        }
      });

      // Assert
      expect(result.current.error).toBeTruthy();
      expect(result.current.loading).toBe(false);
    });

    it('shows loading state during fetch', async () => {
      // Arrange
      mockFetchOnce(200, { items: [], total: 0 }, 100);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      const promise = act(async () => {
        await result.current.getAllPlans();
      });

      // Assert - should be loading
      expect(result.current.loading).toBe(true);

      await promise;

      // Assert - should not be loading after fetch
      expect(result.current.loading).toBe(false);
    });
  });

  describe('getPlanById', () => {
    it('fetches single plan by ID', async () => {
      // Arrange
      const mockPlan = {
        id: '1',
        name: 'Professional',
        monthlyPrice: 49.99,
        annualPrice: 499.99,
        status: 'Active',
        features: ['Feature 1', 'Feature 2'],
      };

      mockFetchOnce(200, mockPlan);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      let plan;
      await act(async () => {
        plan = await result.current.getPlanById('1');
      });

      // Assert
      expect(plan).toEqual(mockPlan);
      expect(plan.id).toBe('1');
    });

    it('returns null for non-existent plan', async () => {
      // Arrange
      mockFetchOnce(404, null);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act & Assert
      await act(async () => {
        try {
          await result.current.getPlanById('non-existent');
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });
  });

  describe('createPlan', () => {
    it('creates a new subscription plan', async () => {
      // Arrange
      const payload = {
        name: 'Starter',
        description: 'Starter plan',
        monthlyPrice: 4.99,
        annualPrice: 49.99,
        features: ['Basic Feature'],
      };

      const mockCreatedPlan = {
        id: '3',
        ...payload,
        status: 'Active',
      };

      mockFetchOnce(201, mockCreatedPlan);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      let plan;
      await act(async () => {
        plan = await result.current.createPlan(payload);
      });

      // Assert
      expect(plan.id).toBeTruthy();
      expect(plan.name).toBe('Starter');
      expect(plan.monthlyPrice).toBe(4.99);
    });

    it('handles validation errors on creation', async () => {
      // Arrange
      const invalidPayload = {
        name: '',  // Invalid - empty
        monthlyPrice: 100,
        annualPrice: 50,  // Invalid - annual < monthly
        features: [],
      };

      mockFetchOnce(400, { message: 'Invalid plan data' });

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act & Assert
      await act(async () => {
        try {
          await result.current.createPlan(invalidPayload);
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });
  });

  describe('updatePlan', () => {
    it('updates plan information successfully', async () => {
      // Arrange
      const payload = {
        name: 'Updated Plan',
        monthlyPrice: 19.99,
        annualPrice: 199.99,
        features: ['Updated Feature'],
      };

      const mockUpdatedPlan = {
        id: '1',
        ...payload,
        status: 'Active',
      };

      mockFetchOnce(200, mockUpdatedPlan);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      let plan;
      await act(async () => {
        plan = await result.current.updatePlan('1', payload);
      });

      // Assert
      expect(plan.name).toBe('Updated Plan');
      expect(plan.monthlyPrice).toBe(19.99);
    });

    it('handles update errors for non-existent plan', async () => {
      // Arrange
      const payload = {
        name: 'Plan',
        monthlyPrice: 9.99,
        annualPrice: 99.99,
        features: [],
      };

      mockFetchOnce(404, { message: 'Plan not found' });

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act & Assert
      await act(async () => {
        try {
          await result.current.updatePlan('non-existent', payload);
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });
  });

  describe('deactivatePlan', () => {
    it('deactivates plan successfully', async () => {
      // Arrange
      mockFetchOnce(200, { status: 'Inactive' });

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      let error;
      await act(async () => {
        try {
          await result.current.deactivatePlan('1');
        } catch (err) {
          error = err;
        }
      });

      // Assert
      expect(error).toBeFalsy();
      expect(result.current.loading).toBe(false);
    });

    it('handles deactivation error for non-existent plan', async () => {
      // Arrange
      mockFetchOnce(404, { message: 'Plan not found' });

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      await act(async () => {
        try {
          await result.current.deactivatePlan('non-existent');
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });
  });

  describe('multi-operation sequence', () => {
    it('handles create, fetch, update, deactivate flow', async () => {
      // Arrange
      const createResponse = {
        id: '1',
        name: 'New Plan',
        status: 'Active',
        monthlyPrice: 9.99,
        annualPrice: 99.99,
      };

      const fetchResponse = {
        id: '1',
        name: 'New Plan',
        monthlyPrice: 9.99,
        annualPrice: 99.99,
        status: 'Active',
        features: ['Feature'],
      };

      const updateResponse = {
        id: '1',
        name: 'Updated Plan',
        monthlyPrice: 19.99,
        annualPrice: 199.99,
        status: 'Active',
      };

      mockFetchSequence([
        { status: 201, body: createResponse },
        { status: 200, body: fetchResponse },
        { status: 200, body: updateResponse },
        { status: 200, body: { status: 'Inactive' } },
      ]);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act
      let created, fetched, updated;
      await act(async () => {
        created = await result.current.createPlan({
          name: 'New Plan',
          monthlyPrice: 9.99,
          annualPrice: 99.99,
          features: [],
        });

        fetched = await result.current.getPlanById(created.id);

        updated = await result.current.updatePlan(created.id, {
          name: 'Updated Plan',
          monthlyPrice: 19.99,
          annualPrice: 199.99,
          features: ['Feature'],
        });

        await result.current.deactivatePlan(created.id);
      });

      // Assert
      expect(created.id).toBeTruthy();
      expect(fetched.name).toBe('New Plan');
      expect(updated.name).toBe('Updated Plan');
    });
  });

  describe('error handling', () => {
    it('clears error on successful call after failure', async () => {
      // Arrange
      mockFetchSequence([
        { status: 500, body: {} },
        { status: 200, body: { items: [], total: 0 } },
      ]);

      const { result } = renderHook(() => useSubscriptionPlans());

      // Act & Assert - first call fails
      await act(async () => {
        try {
          await result.current.getAllPlans();
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeTruthy();

      // Act & Assert - second call succeeds
      await act(async () => {
        try {
          await result.current.getAllPlans();
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeFalsy();
    });
  });
});
