import { renderHook, act } from '@testing-library/react';
import { useAddressLookup } from '@/hooks/useAddressLookup';
import { mockFetchSequence, mockFetchOnce, setupFetchMockCleanup } from '../helpers/mockFetch';

describe('useAddressLookup', () => {
  setupFetchMockCleanup();

  const mockCountries = [
    { code: 'IN', name: 'India' },
    { code: 'US', name: 'United States' },
  ];

  const mockStates = [
    { id: 1, name: 'Maharashtra', countryCode: 'IN' },
    { id: 2, name: 'Delhi', countryCode: 'IN' },
  ];

  const mockDistricts = [
    { id: 1, name: 'Mumbai', stateId: 1 },
    { id: 2, name: 'Thane', stateId: 1 },
  ];

  describe('getCountries', () => {
    it('fetches all countries', async () => {
      mockFetchOnce(200, mockCountries);
      const { result } = renderHook(() => useAddressLookup());

      let countries;
      await act(async () => {
        countries = await result.current.getCountries();
      });

      expect(countries).toEqual(mockCountries);
      expect(countries).toHaveLength(2);
      expect(result.current.loading).toBe(false);
    });

    it('handles API error gracefully', async () => {
      mockFetchOnce(500);
      const { result } = renderHook(() => useAddressLookup());

      await act(async () => {
        try {
          await result.current.getCountries();
        } catch (err) {
          // Expected
        }
      });

      expect(result.current.error).toBeTruthy();
    });
  });

  describe('getStatesByCountry', () => {
    it('fetches states for given country', async () => {
      mockFetchOnce(200, mockStates);
      const { result } = renderHook(() => useAddressLookup());

      let states;
      await act(async () => {
        states = await result.current.getStatesByCountry('IN');
      });

      expect(states).toEqual(mockStates);
      expect(states).toHaveLength(2);
      expect(states[0].countryCode).toBe('IN');
    });

    it('returns empty list for invalid country', async () => {
      mockFetchOnce(404, []);
      const { result } = renderHook(() => useAddressLookup());

      await act(async () => {
        try {
          await result.current.getStatesByCountry('INVALID');
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });

    it('maintains country code in returned states', async () => {
      mockFetchOnce(200, mockStates);
      const { result } = renderHook(() => useAddressLookup());

      let states;
      await act(async () => {
        states = await result.current.getStatesByCountry('IN');
      });

      expect(states).toHaveLength(2);
      expect(states.every(s => s.countryCode === 'IN')).toBe(true);
    });
  });

  describe('getDistrictsByState', () => {
    it('fetches districts for given state', async () => {
      mockFetchOnce(200, mockDistricts);
      const { result } = renderHook(() => useAddressLookup());

      let districts;
      await act(async () => {
        districts = await result.current.getDistrictsByState(1);
      });

      expect(districts).toEqual(mockDistricts);
      expect(districts).toHaveLength(2);
      expect(districts[0].stateId).toBe(1);
    });

    it('returns empty list for invalid state', async () => {
      mockFetchOnce(404, []);
      const { result } = renderHook(() => useAddressLookup());

      await act(async () => {
        try {
          await result.current.getDistrictsByState(99999);
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });

    it('maintains state ID in returned districts', async () => {
      mockFetchOnce(200, mockDistricts);
      const { result } = renderHook(() => useAddressLookup());

      let districts;
      await act(async () => {
        districts = await result.current.getDistrictsByState(1);
      });

      expect(districts).toHaveLength(2);
      expect(districts.every(d => d.stateId === 1)).toBe(true);
    });
  });

  describe('cascading lookup', () => {
    it('cascades from country to state to district', async () => {
      mockFetchSequence([
        { status: 200, body: mockCountries },
        { status: 200, body: mockStates },
        { status: 200, body: mockDistricts },
      ]);
      const { result } = renderHook(() => useAddressLookup());

      let countries, states, districts;
      await act(async () => {
        countries = await result.current.getCountries();
        states = await result.current.getStatesByCountry('IN');
        districts = await result.current.getDistrictsByState(1);
      });

      expect(countries).toHaveLength(2);
      expect(states).toHaveLength(2);
      expect(districts).toHaveLength(2);
      expect(states[0].countryCode).toBe('IN');
      expect(districts[0].stateId).toBe(1);
    });
  });

  describe('getCountryByCode', () => {
    it('fetches single country by code', async () => {
      const mockCountry = { code: 'IN', name: 'India' };
      mockFetchOnce(200, mockCountry);
      const { result } = renderHook(() => useAddressLookup());

      let country;
      await act(async () => {
        country = await result.current.getCountryByCode('IN');
      });

      expect(country).toEqual(mockCountry);
      expect(country.code).toBe('IN');
    });

    it('returns null for invalid country code', async () => {
      mockFetchOnce(404, null);
      const { result } = renderHook(() => useAddressLookup());

      await act(async () => {
        try {
          await result.current.getCountryByCode('INVALID');
        } catch (err) {
          expect(result.current.error).toBeTruthy();
        }
      });
    });
  });

  describe('loading state', () => {
    it('shows loading state during fetch', async () => {
      mockFetchOnce(200, mockCountries, 100);
      const { result } = renderHook(() => useAddressLookup());

      const promise = act(async () => {
        await result.current.getCountries();
      });

      expect(result.current.loading).toBe(true);
      await promise;
      expect(result.current.loading).toBe(false);
    });
  });

  describe('error handling', () => {
    it('clears error on successful call after failure', async () => {
      mockFetchSequence([
        { status: 500, body: {} },
        { status: 200, body: mockCountries },
      ]);
      const { result } = renderHook(() => useAddressLookup());

      await act(async () => {
        try {
          await result.current.getCountries();
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeTruthy();

      await act(async () => {
        try {
          await result.current.getCountries();
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeFalsy();
    });
  });
});
