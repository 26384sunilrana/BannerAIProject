export interface CountryDto {
  isoCode: string;
  name: string;
  regionName?: string;
  phoneCode?: string;
  isActive: boolean;
}

export interface StateDto {
  id: number;
  countryCode: string;
  code: string;
  name: string;
  regionType?: string;
  isActive: boolean;
}

export interface DistrictDto {
  id: number;
  stateId: number;
  code: string;
  name: string;
  regionType?: string;
  isActive: boolean;
}

export interface AddressData {
  countryCode?: string;
  stateId?: number;
  districtId?: number;
  address?: string;
  city?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
}

export interface ShopAddress extends AddressData {
  countryName?: string;
  stateName?: string;
  districtName?: string;
}

export interface AddressLookupResponse<T> {
  success: boolean;
  data: T[];
  count: number;
}
