export interface CountryDto {
  ISOCode: string;
  Name: string;
  RegionName?: string;
  PhoneCode?: string;
  IsActive: boolean;
}

export interface StateDto {
  Id: number;
  CountryCode: string;
  Code: string;
  Name: string;
  RegionType?: string;
  IsActive: boolean;
}

export interface DistrictDto {
  Id: number;
  StateId: number;
  Code: string;
  Name: string;
  RegionType?: string;
  IsActive: boolean;
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
