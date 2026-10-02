import { useState, useCallback } from 'react';
import { authFetch } from '@/api/client';
import { ShopDto, CreateShopPayload, UpdateShopPayload, ShopsListResponse } from '@/types/shop';


export const useShops = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Get user's shops (My Shops)
  const getMyShops = useCallback(
    async (pageNumber = 1, pageSize = 10, status?: string): Promise<ShopsListResponse> => {
      setLoading(true);
      setError(null);
      try {
        const params = new URLSearchParams();
        params.append('pageNumber', pageNumber.toString());
        params.append('pageSize', pageSize.toString());
        if (status) params.append('status', status);

        const response = await authFetch(`/shops/my-shops?${params}`, {
          headers: { 'Content-Type': 'application/json' },
        });

        if (!response.ok) {
          throw new Error('Failed to fetch your shops');
        }

        const data = await response.json();
        return data;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to fetch shops';
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    []
  );

  // Every shop (administrators only)
  const getAllShops = useCallback(
    async (pageNumber = 1, pageSize = 10, status?: string): Promise<ShopsListResponse> => {
      setLoading(true);
      setError(null);
      try {
        const params = new URLSearchParams();
        params.append('pageNumber', pageNumber.toString());
        params.append('pageSize', pageSize.toString());
        if (status) params.append('status', status);

        const response = await authFetch(`/shops?${params}`, {
          headers: { 'Content-Type': 'application/json' },
        });

        if (!response.ok) {
          throw new Error('Failed to fetch shops');
        }

        return await response.json();
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to fetch shops';
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    []
  );

  // Get single shop by ID
  const getShopById = useCallback(async (shopId: string): Promise<ShopDto> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/shops/${shopId}`, {
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch shop');
      }

      const data = await response.json();
      return data;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch shop';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Create new shop
  const createShop = useCallback(async (payload: CreateShopPayload): Promise<ShopDto> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/shops`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to create shop');
      }

      const data = await response.json();
      return data;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create shop';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Update shop
  const updateShop = useCallback(
    async (shopId: string, payload: UpdateShopPayload): Promise<ShopDto> => {
      setLoading(true);
      setError(null);
      try {
        const response = await authFetch(`/shops/${shopId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Failed to update shop');
        }

        const data = await response.json();
        return data;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to update shop';
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    []
  );

  // Deactivate shop
  const deactivateShop = useCallback(async (shopId: string): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/shops/${shopId}/deactivate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to deactivate shop');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to deactivate shop';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    loading,
    error,
    getMyShops,
    getAllShops,
    getShopById,
    createShop,
    updateShop,
    deactivateShop,
  };
};
