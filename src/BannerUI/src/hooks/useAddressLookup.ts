import { useState, useCallback } from 'react';
import { authFetch } from '@/api/client';
import { CountryDto, StateDto, DistrictDto } from '@/types/address';


export const useAddressLookup = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch all countries
  const getCountries = useCallback(async (): Promise<CountryDto[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/address/countries`, {
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch countries');
      }

      const data = await response.json();
      return data.data || [];
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch countries';
      setError(message);
      return [];
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch states for a country
  const getStatesByCountry = useCallback(async (countryCode: string): Promise<StateDto[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(
        `/address/countries/${countryCode}/states`,
        {
          headers: { 'Content-Type': 'application/json' },
        }
      );

      if (!response.ok) {
        throw new Error('Failed to fetch states');
      }

      const data = await response.json();
      return data.data || [];
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch states';
      setError(message);
      return [];
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch districts for a state
  const getDistrictsByState = useCallback(async (stateId: number): Promise<DistrictDto[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(
        `/address/states/${stateId}/districts`,
        {
          headers: { 'Content-Type': 'application/json' },
        }
      );

      if (!response.ok) {
        throw new Error('Failed to fetch districts');
      }

      const data = await response.json();
      return data.data || [];
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch districts';
      setError(message);
      return [];
    } finally {
      setLoading(false);
    }
  }, []);

  // Search states
  const searchStates = useCallback(async (searchTerm: string): Promise<StateDto[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(
        `/address/states/search?searchTerm=${encodeURIComponent(searchTerm)}`,
        {
          headers: { 'Content-Type': 'application/json' },
        }
      );

      if (!response.ok) {
        throw new Error('Failed to search states');
      }

      const data = await response.json();
      return data.data || [];
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to search states';
      setError(message);
      return [];
    } finally {
      setLoading(false);
    }
  }, []);

  // Search districts
  const searchDistricts = useCallback(async (searchTerm: string): Promise<DistrictDto[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(
        `/address/districts/search?searchTerm=${encodeURIComponent(searchTerm)}`,
        {
          headers: { 'Content-Type': 'application/json' },
        }
      );

      if (!response.ok) {
        throw new Error('Failed to search districts');
      }

      const data = await response.json();
      return data.data || [];
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to search districts';
      setError(message);
      return [];
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    loading,
    error,
    getCountries,
    getStatesByCountry,
    getDistrictsByState,
    searchStates,
    searchDistricts,
  };
};
