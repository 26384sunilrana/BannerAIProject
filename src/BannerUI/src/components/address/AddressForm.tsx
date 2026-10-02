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
  const { error: lookupError, getCountries, getStatesByCountry, getDistrictsByState } = useAddressLookup();

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
  const [formError, setFormError] = useState<string | null>(null);

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
      setLoading(false);
    };
    loadStates();
  }, [formData.countryCode, getStatesByCountry]);

  // Load districts when state changes
  useEffect(() => {
    const loadDistricts = async () => {
      if (!formData.stateId) {
        setDistricts([]);
        return;
      }
      setLoading(true);
      const data = await getDistrictsByState(formData.stateId);
      setDistricts(data);
      setLoading(false);
    };
    loadDistricts();
  }, [formData.stateId, getDistrictsByState]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    const isNumber = type === 'number' || name === 'stateId' || name === 'districtId';

    setFormData((prev) => {
      const next = { ...prev, [name]: isNumber ? (value ? Number(value) : undefined) : value || undefined };
      // choosing another country or state invalidates what was chosen under the old one
      if (name === 'countryCode') {
        next.stateId = undefined;
        next.districtId = undefined;
      }
      if (name === 'stateId') next.districtId = undefined;
      return next;
    });
    if (name === 'countryCode') {
      setStates([]);
      setDistricts([]);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.countryCode) {
      setFormError('Choose a country');
      return;
    }
    if (!formData.stateId) {
      setFormError('Choose a state');
      return;
    }
    setFormError(null);
    onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-4 max-w-md">
      {(formError || lookupError) && (
        <p role="alert" className="text-sm text-red-700">
          {formError || lookupError}
        </p>
      )}
      {/* Country */}
      <div>
        <label htmlFor="addr-country" className="block text-sm font-medium text-gray-700 mb-1">Country *</label>
        <select
          id="addr-country"
          name="countryCode"
          value={formData.countryCode || ''}
          onChange={handleChange}
          required
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Select Country</option>
          {countries.map((country) => (
            <option key={country.isoCode} value={country.isoCode}>
              {country.name}
            </option>
          ))}
        </select>
      </div>

      {/* State */}
      <div>
        <label htmlFor="addr-state" className="block text-sm font-medium text-gray-700 mb-1">State *</label>
        <select
          id="addr-state"
          name="stateId"
          value={formData.stateId || ''}
          onChange={handleChange}
          required
          disabled={!formData.countryCode || states.length === 0}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
        >
          <option value="">Select State</option>
          {states.map((state) => (
            <option key={state.id} value={state.id}>
              {state.name} {state.regionType ? `(${state.regionType})` : ''}
            </option>
          ))}
        </select>
      </div>

      {/* District */}
      <div>
        <label htmlFor="addr-district" className="block text-sm font-medium text-gray-700 mb-1">District</label>
        <select
          id="addr-district"
          name="districtId"
          value={formData.districtId || ''}
          onChange={handleChange}
          disabled={!formData.stateId || districts.length === 0}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
        >
          <option value="">Select District</option>
          {districts.map((district) => (
            <option key={district.id} value={district.id}>
              {district.name}
            </option>
          ))}
        </select>
      </div>

      {/* City */}
      <div>
        <label htmlFor="addr-city" className="block text-sm font-medium text-gray-700 mb-1">City</label>
        <input
          id="addr-city"
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
        <label htmlFor="addr-street-address" className="block text-sm font-medium text-gray-700 mb-1">Street Address</label>
        <input
          id="addr-street-address"
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
        <label htmlFor="addr-postal-code" className="block text-sm font-medium text-gray-700 mb-1">Postal Code</label>
        <input
          id="addr-postal-code"
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
              <label htmlFor="addr-latitude" className="block text-sm font-medium text-gray-700 mb-1">Latitude</label>
              <input
                id="addr-latitude"
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
              <label htmlFor="addr-longitude" className="block text-sm font-medium text-gray-700 mb-1">Longitude</label>
              <input
                id="addr-longitude"
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
