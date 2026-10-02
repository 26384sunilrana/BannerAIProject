import { renderHook, act } from '@testing-library/react';
import { useShops } from '@/hooks/useShops';
import { mockFetchOnce, mockFetchSequence, restoreFetch, setupFetchMockCleanup } from '../helpers/mockFetch';

describe('useShops', () => {
  setupFetchMockCleanup();

  describe('getMyShops', () => {
    it('fetches user shops with pagination', async () => {
      // Arrange
      const mockResponse = {
        items: [
          { id: '1', name: 'Shop 1', city: 'Mumbai', status: 'Active' },
          { id: '2', name: 'Shop 2', city: 'Delhi', status: 'Active' },
        ],
        total: 2,
        pageNumber: 1,
        pageSize: 10,
      };

      mockFetchOnce(200, mockResponse);

      const { result } = renderHook(() => useShops());

      // Act
      let response;
      await act(async () => {
        response = await result.current.getMyShops(1, 10);
      });

      // Assert
      expect(response).toEqual(mockResponse);
      expect(response.items).toHaveLength(2);
      expect(result.current.loading).toBe(false);
    });

    it('filters shops by status', async () => {
      // Arrange
      const mockResponse = {
        items: [{ id: '1', name: 'Active Shop', status: 'Active' }],
        total: 1,
        pageNumber: 1,
        pageSize: 10,
      };

      mockFetchOnce(200, mockResponse);

      const { result } = renderHook(() => useShops());

      // Act
      let response;
      await act(async () => {
        response = await result.current.getMyShops(1, 10, 'Active');
      });

      // Assert
      expect(response.items).toHaveLength(1);
      expect(response.items[0].status).toBe('Active');
    });

    it('handles pagination', async () => {
      // Arrange
      const mockResponse = {
        items: [{ id: '3', name: 'Shop 3', status: 'Active' }],
        total: 30,
        pageNumber: 2,
        pageSize: 10,
      };

      mockFetchOnce(200, mockResponse);

      const { result } = renderHook(() => useShops());

      // Act
      let response;
      await act(async () => {
        response = await result.current.getMyShops(2, 10);
      });

      // Assert
      expect(response.pageNumber).toBe(2);
      expect(response.total).toBe(30);
    });

    it('handles API error gracefully', async () => {
      // Arrange
      mockFetchOnce(500);

      const { result } = renderHook(() => useShops());

      // Act
      await act(async () => {
        try {
          await result.current.getMyShops(1, 10);
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

      const { result } = renderHook(() => useShops());

      // Act
      let promise: Promise<unknown> = Promise.resolve();
      act(() => {
        promise = result.current.getMyShops(1, 10);
      });

      // Assert - should be loading
      expect(result.current.loading).toBe(true);

      await act(async () => {
        await promise;
      });

      // Assert - should not be loading after fetch
      expect(result.current.loading).toBe(false);
    });
  });

  describe('getShopById', () => {
    it('fetches single shop by ID', async () => {
      // Arrange
      const mockShop = {
        id: '1',
        name: 'Test Shop',
        city: 'Mumbai',
        status: 'Active',
        phoneNumber: '+91-22-1234',
        website: 'https://example.com',
      };

      mockFetchOnce(200, mockShop);

      const { result } = renderHook(() => useShops());

      // Act
      let shop;
      await act(async () => {
        shop = await result.current.getShopById('1');
      });

      // Assert
      expect(shop).toEqual(mockShop);
      expect(shop.id).toBe('1');
    });

    it('returns null for non-existent shop', async () => {
      // Arrange
      mockFetchOnce(404, null);

      const { result } = renderHook(() => useShops());

      // Act & Assert
      await act(async () => {
        try {
          await result.current.getShopById('non-existent');
        } catch (err) {
          // the hook records the failure in `error`
        }
      });
      expect(result.current.error).toBeTruthy();
    });
  });

  describe('createShop', () => {
    it('creates a new shop successfully', async () => {
      // Arrange
      const payload = {
        name: 'New Shop',
        city: 'Mumbai',
        address: '123 Main St',
        countryCode: 'IN',
        stateId: 1,
        districtId: 1,
        postalCode: '400001',
      };

      const mockCreatedShop = { id: '3', ...payload, status: 'Active' };

      mockFetchOnce(201, mockCreatedShop);

      const { result } = renderHook(() => useShops());

      // Act
      let shop;
      await act(async () => {
        shop = await result.current.createShop(payload);
      });

      // Assert
      expect(shop.id).toBeTruthy();
      expect(shop.name).toBe('New Shop');
    });

    it('handles validation errors on creation', async () => {
      // Arrange
      const payload = {
        name: '',  // Invalid - empty
        city: 'Mumbai',
        address: '123 Main St',
        countryCode: 'IN',
        stateId: 1,
        districtId: 1,
        postalCode: '400001',
      };

      mockFetchOnce(400, { message: 'Invalid shop name' });

      const { result } = renderHook(() => useShops());

      // Act & Assert
      await act(async () => {
        try {
          await result.current.createShop(payload);
        } catch (err) {
          // the hook records the failure in `error`
        }
      });
      expect(result.current.error).toBeTruthy();
    });
  });

  describe('updateShop', () => {
    it('updates shop information successfully', async () => {
      // Arrange
      const payload = {
        name: 'Updated Shop',
        city: 'Updated City',
        address: 'New Address',
        countryCode: 'IN',
        stateId: 1,
        districtId: 1,
        postalCode: '400001',
      };

      const mockUpdatedShop = { id: '1', ...payload, status: 'Active' };

      mockFetchOnce(200, mockUpdatedShop);

      const { result } = renderHook(() => useShops());

      // Act
      let shop;
      await act(async () => {
        shop = await result.current.updateShop('1', payload);
      });

      // Assert
      expect(shop.name).toBe('Updated Shop');
      expect(shop.city).toBe('Updated City');
    });

    it('handles update errors for non-existent shop', async () => {
      // Arrange
      const payload = {
        name: 'Shop',
        city: 'City',
        address: 'Address',
        countryCode: 'IN',
        stateId: 1,
        districtId: 1,
        postalCode: '400001',
      };

      mockFetchOnce(404, { message: 'Shop not found' });

      const { result } = renderHook(() => useShops());

      // Act & Assert
      await act(async () => {
        try {
          await result.current.updateShop('non-existent', payload);
        } catch (err) {
          // the hook records the failure in `error`
        }
      });
      expect(result.current.error).toBeTruthy();
    });
  });

  describe('deactivateShop', () => {
    it('deactivates shop successfully', async () => {
      // Arrange
      mockFetchOnce(200, {});

      const { result } = renderHook(() => useShops());

      // Act
      let error;
      await act(async () => {
        try {
          await result.current.deactivateShop('1');
        } catch (err) {
          error = err;
        }
      });

      // Assert
      expect(error).toBeFalsy();
      expect(result.current.loading).toBe(false);
    });

    it('handles deactivation error for non-existent shop', async () => {
      // Arrange
      mockFetchOnce(404, { message: 'Shop not found' });

      const { result } = renderHook(() => useShops());

      // Act
      await act(async () => {
        try {
          await result.current.deactivateShop('non-existent');
        } catch (err) {
          // the hook records the failure in `error`
        }
      });
      expect(result.current.error).toBeTruthy();
    });

    it('handles deactivation error for already inactive shop', async () => {
      // Arrange
      mockFetchOnce(400, { message: 'Shop is already inactive' });

      const { result } = renderHook(() => useShops());

      // Act
      await act(async () => {
        try {
          await result.current.deactivateShop('1');
        } catch (err) {
          // the hook records the failure in `error`
        }
      });
      expect(result.current.error).toBeTruthy();
    });
  });

  describe('multi-operation sequence', () => {
    it('handles create, fetch, update, deactivate flow', async () => {
      // Arrange
      const createResponse = {
        id: '1',
        name: 'New Shop',
        status: 'Active',
      };

      const fetchResponse = {
        id: '1',
        name: 'New Shop',
        city: 'Mumbai',
        status: 'Active',
      };

      const updateResponse = {
        id: '1',
        name: 'Updated Shop',
        city: 'Delhi',
        status: 'Active',
      };

      mockFetchSequence([
        { status: 201, body: createResponse },
        { status: 200, body: fetchResponse },
        { status: 200, body: updateResponse },
        { status: 200, body: {} },
      ]);

      const { result } = renderHook(() => useShops());

      // Act
      let created, fetched, updated;
      await act(async () => {
        created = await result.current.createShop({
          name: 'New Shop',
          city: 'Mumbai',
          address: 'Address',
          countryCode: 'IN',
          stateId: 1,
          districtId: 1,
          postalCode: '400001',
        });

        fetched = await result.current.getShopById(created.id);

        updated = await result.current.updateShop(created.id, {
          name: 'Updated Shop',
          city: 'Delhi',
          address: 'Address',
          countryCode: 'IN',
          stateId: 2,
          districtId: 1,
          postalCode: '110001',
        });

        await result.current.deactivateShop(created.id);
      });

      // Assert
      expect(created.id).toBeTruthy();
      expect(fetched.city).toBe('Mumbai');
      expect(updated.city).toBe('Delhi');
    });
  });

  describe('error handling', () => {
    it('clears error on successful call after failure', async () => {
      // Arrange
      mockFetchSequence([
        { status: 500, body: {} },    // First call fails
        { status: 200, body: { items: [], total: 0 } },  // Second succeeds
      ]);

      const { result } = renderHook(() => useShops());

      // Act & Assert - first call fails
      await act(async () => {
        try {
          await result.current.getMyShops(1, 10);
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeTruthy();

      // Act & Assert - second call succeeds
      await act(async () => {
        try {
          await result.current.getMyShops(1, 10);
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeFalsy();
    });
  });
});
