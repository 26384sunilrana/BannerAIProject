import { useState, useCallback } from 'react';
import { CountryDto, StateDto, DistrictDto } from '@/types/address';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:5001/api';

export const useAddressLookup = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch all countries
  const getCountries = useCallback(async (): Promise<CountryDto[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/address/countries`, {
        credentials: 'include',
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
      const response = await fetch(
        `${API_BASE_URL}/address/countries/${countryCode}/states`,
        {
          credentials: 'include',
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
      const response = await fetch(
        `${API_BASE_URL}/address/states/${stateId}/districts`,
        {
          credentials: 'include',
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
      const response = await fetch(
        `${API_BASE_URL}/address/states/search?searchTerm=${encodeURIComponent(searchTerm)}`,
        {
          credentials: 'include',
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
      const response = await fetch(
        `${API_BASE_URL}/address/districts/search?searchTerm=${encodeURIComponent(searchTerm)}`,
        {
          credentials: 'include',
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
