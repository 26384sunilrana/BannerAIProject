'use client';

import React, { useState, useEffect } from 'react';
import { useAddressLookup } from '@/hooks/useAddressLookup';
import { CountryDto, StateDto, DistrictDto } from '@/types/address';

export interface AddressFormData {
  countryCode?: string;
  stateId?: number;
  districtId?: number;
  address?: string;
  city?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
}

interface AddressFormProps {
  initialData?: AddressFormData;
  onSubmit: (data: AddressFormData) => void;
  includeGeo?: boolean;
}

export const AddressForm: React.FC<AddressFormProps> = ({
  initialData,
  onSubmit,
  includeGeo = true,
}) => {
  const { getCountries, getStatesByCountry, getDistrictsByState } = useAddressLookup();

  const [countries, setCountries] = useState<CountryDto[]>([]);
  const [states, setStates] = useState<StateDto[]>([]);
  const [districts, setDistricts] = useState<DistrictDto[]>([]);

  const [formData, setFormData] = useState<AddressFormData>(
    initialData || {
      countryCode: 'IN',
      address: '',
      city: '',
      postalCode: '',
    }
  );

  const [loading, setLoading] = useState(false);

  // Load countries on mount
  useEffect(() => {
    const loadCountries = async () => {
      setLoading(true);
      const data = await getCountries();
      setCountries(data);
      setLoading(false);
    };
    loadCountries();
  }, [getCountries]);

  // Load states when country changes
  useEffect(() => {
    const loadStates = async () => {
      if (!formData.countryCode) return;
      setLoading(true);
      const data = await getStatesByCountry(formData.countryCode);
      setStates(data);
      setDistricts([]); // Clear districts
      setFormData((prev) => ({ ...prev, stateId: undefined, districtId: undefined }));
      setLoading(false);
    };
    loadStates();
  }, [formData.countryCode, getStatesByCountry]);

  // Load districts when state changes
  useEffect(() => {
    const loadDistricts = async () => {
      if (!formData.stateId) return;
      setLoading(true);
      const data = await getDistrictsByState(formData.stateId);
      setDistricts(data);
      setFormData((prev) => ({ ...prev, districtId: undefined }));
      setLoading(false);
    };
    loadDistricts();
  }, [formData.stateId, getDistrictsByState]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]:
        type === 'number' ? (value ? parseFloat(value) : undefined) : value || undefined,
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-md">
      {/* Country */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Country *</label>
        <select
          name="countryCode"
          value={formData.countryCode || ''}
          onChange={handleChange}
          required
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Select Country</option>
          {countries.map((country) => (
            <option key={country.ISOCode} value={country.ISOCode}>
              {country.Name}
            </option>
          ))}
        </select>
      </div>

      {/* State */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">State *</label>
        <select
          name="stateId"
          value={formData.stateId || ''}
          onChange={handleChange}
          required
          disabled={!formData.countryCode || states.length === 0}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
        >
          <option value="">Select State</option>
          {states.map((state) => (
            <option key={state.Id} value={state.Id}>
              {state.Name} {state.RegionType ? `(${state.RegionType})` : ''}
            </option>
          ))}
        </select>
      </div>

      {/* District */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">District</label>
        <select
          name="districtId"
          value={formData.districtId || ''}
          onChange={handleChange}
          disabled={!formData.stateId || districts.length === 0}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
        >
          <option value="">Select District</option>
          {districts.map((district) => (
            <option key={district.Id} value={district.Id}>
              {district.Name}
            </option>
          ))}
        </select>
      </div>

      {/* City */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">City</label>
        <input
          type="text"
          name="city"
          value={formData.city || ''}
          onChange={handleChange}
          placeholder="Enter city name"
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Address */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Street Address</label>
        <input
          type="text"
          name="address"
          value={formData.address || ''}
          onChange={handleChange}
          placeholder="Enter street address"
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Postal Code */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Postal Code</label>
        <input
          type="text"
          name="postalCode"
          value={formData.postalCode || ''}
          onChange={handleChange}
          placeholder="Enter postal code"
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Geo Coordinates */}
      {includeGeo && (
        <>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Latitude</label>
              <input
                type="number"
                name="latitude"
                value={formData.latitude || ''}
                onChange={handleChange}
                placeholder="0.0000"
                step="0.0001"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Longitude</label>
              <input
                type="number"
                name="longitude"
                value={formData.longitude || ''}
                onChange={handleChange}
                placeholder="0.0000"
                step="0.0001"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </>
      )}

      {/* Submit */}
      <button
        type="submit"
        disabled={loading}
        className="w-full px-4 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400"
      >
        {loading ? 'Loading...' : 'Save Address'}
      </button>
    </form>
  );
};
