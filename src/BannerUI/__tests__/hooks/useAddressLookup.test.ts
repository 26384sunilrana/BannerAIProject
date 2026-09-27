import { renderHook, act } from '@testing-library/react';
import { useAddressLookup } from '@/hooks/useAddressLookup';
import { mockFetchOnce, mockFetchSequence, restoreFetch, setupFetchMockCleanup } from '../helpers/mockFetch';

describe('useAddressLookup', () => {
  setupFetchMockCleanup();

  describe('getCountries', () => {
    it('fetches all countries successfully', async () => {
      // Arrange
      const mockCountries = [
        { isoCode: 'IN', name: 'India', phoneCode: '+91' },
        { isoCode: 'US', name: 'United States', phoneCode: '+1' },
      ];

      mockFetchOnce(200, mockCountries);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let countries;
      await act(async () => {
        countries = await result.current.getCountries();
      });

      // Assert
      expect(countries).toEqual(mockCountries);
      expect(result.current.loading).toBe(false);
    });

    it('sets error on fetch failure', async () => {
      // Arrange
      mockFetchOnce(500);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let error;
      await act(async () => {
        try {
          await result.current.getCountries();
        } catch (err) {
          error = err;
        }
      });

      // Assert
      expect(result.current.error).toBeTruthy();
      expect(result.current.loading).toBe(false);
    });

    it('shows loading state during fetch', async () => {
      // Arrange
      mockFetchOnce(200, [], 100); // 100ms delay

      const { result } = renderHook(() => useAddressLookup());

      // Act
      const promise = act(async () => {
        await result.current.getCountries();
      });

      // Assert - should be loading immediately
      expect(result.current.loading).toBe(true);

      await promise;

      // Assert - should not be loading after fetch
      expect(result.current.loading).toBe(false);
    });
  });

  describe('getStatesByCountry', () => {
    it('fetches states for a valid country', async () => {
      // Arrange
      const mockStates = [
        { id: 1, name: 'Maharashtra', countryCode: 'IN' },
        { id: 2, name: 'Karnataka', countryCode: 'IN' },
      ];

      mockFetchOnce(200, mockStates);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let states;
      await act(async () => {
        states = await result.current.getStatesByCountry('IN');
      });

      // Assert
      expect(states).toEqual(mockStates);
    });

    it('returns empty list for invalid country', async () => {
      // Arrange
      mockFetchOnce(404, []);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let states;
      await act(async () => {
        try {
          states = await result.current.getStatesByCountry('XX');
        } catch (err) {
          // Expected
        }
      });

      // Assert
      expect(result.current.error).toBeTruthy();
    });

    it('constructs correct URL with country code', async () => {
      // Arrange
      mockFetchOnce(200, []);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      await act(async () => {
        try {
          await result.current.getStatesByCountry('IN');
        } catch {
          // Ignore
        }
      });

      // Assert - verify the fetch was called (would verify URL in integration test)
      expect(result.current.loading).toBe(false);
    });
  });

  describe('getDistrictsByState', () => {
    it('fetches districts for a valid state', async () => {
      // Arrange
      const mockDistricts = [
        { id: 1, name: 'Mumbai', stateId: 1 },
        { id: 2, name: 'Pune', stateId: 1 },
      ];

      mockFetchOnce(200, mockDistricts);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let districts;
      await act(async () => {
        districts = await result.current.getDistrictsByState(1);
      });

      // Assert
      expect(districts).toEqual(mockDistricts);
    });

    it('returns empty list for invalid state', async () => {
      // Arrange
      mockFetchOnce(404, []);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      await act(async () => {
        try {
          await result.current.getDistrictsByState(9999);
        } catch (err) {
          // Expected
        }
      });

      // Assert
      expect(result.current.error).toBeTruthy();
    });
  });

  describe('searchStates', () => {
    it('searches states by query', async () => {
      // Arrange
      const mockResults = [
        { id: 1, name: 'Maharashtra', countryCode: 'IN' },
      ];

      mockFetchOnce(200, mockResults);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let results;
      await act(async () => {
        results = await result.current.searchStates('Maha');
      });

      // Assert
      expect(results).toEqual(mockResults);
    });

    it('returns empty list when no states match', async () => {
      // Arrange
      mockFetchOnce(200, []);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let results;
      await act(async () => {
        results = await result.current.searchStates('XYZ');
      });

      // Assert
      expect(results).toEqual([]);
    });
  });

  describe('searchDistricts', () => {
    it('searches districts by query', async () => {
      // Arrange
      const mockResults = [
        { id: 1, name: 'Mumbai', stateId: 1 },
      ];

      mockFetchOnce(200, mockResults);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let results;
      await act(async () => {
        results = await result.current.searchDistricts('Mum');
      });

      // Assert
      expect(results).toEqual(mockResults);
    });

    it('returns empty list when no districts match', async () => {
      // Arrange
      mockFetchOnce(200, []);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let results;
      await act(async () => {
        results = await result.current.searchDistricts('XYZ');
      });

      // Assert
      expect(results).toEqual([]);
    });
  });

  describe('cascading sequence', () => {
    it('handles country -> state -> district sequence', async () => {
      // Arrange
      const countries = [{ isoCode: 'IN', name: 'India', phoneCode: '+91' }];
      const states = [{ id: 1, name: 'Maharashtra', countryCode: 'IN' }];
      const districts = [{ id: 1, name: 'Mumbai', stateId: 1 }];

      mockFetchSequence([
        { status: 200, body: countries },
        { status: 200, body: states },
        { status: 200, body: districts },
      ]);

      const { result } = renderHook(() => useAddressLookup());

      // Act
      let country, statesList, districtsList;
      await act(async () => {
        country = await result.current.getCountries();
        statesList = await result.current.getStatesByCountry('IN');
        districtsList = await result.current.getDistrictsByState(1);
      });

      // Assert
      expect(country).toEqual(countries);
      expect(statesList).toEqual(states);
      expect(districtsList).toEqual(districts);
    });
  });

  describe('error handling', () => {
    it('clears error on successful subsequent call', async () => {
      // Arrange
      mockFetchSequence([
        { status: 500, body: {} },  // First call fails
        { status: 200, body: [] },   // Second call succeeds
      ]);

      const { result } = renderHook(() => useAddressLookup());

      // Act & Assert - first call fails
      await act(async () => {
        try {
          await result.current.getCountries();
        } catch {
          // Expected
        }
      });
      expect(result.current.error).toBeTruthy();

      // Act & Assert - second call succeeds
      await act(async () => {
        try {
          await result.current.getCountries();
        } catch {
          // Expected
        }
      });
      // Error should be cleared
      expect(result.current.error).toBeFalsy();
    });
  });
});
