export interface ShopLocation {
  countryCode?: string;
  countryName?: string;
  stateId?: number;
  stateName?: string;
  districtId?: number;
  districtName?: string;
  address?: string;
  city?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
}

export interface ShopDto extends ShopLocation {
  id: string;
  /** SHP-XXXX-XXXX, given when the shop takes a subscription. */
  uniqueId?: string | null;
  cityId?: number | null;
  groupId?: number | null;
  cityUniqueId?: string | null;
  groupName?: string | null;
  groupUniqueId?: string | null;
  name: string;
  description?: string;
  parentShopId?: string;
  childShopsCount: number;
  status: 'Active' | 'Inactive' | 'Archived';
  phoneNumber?: string;
  website?: string;
  ownerUserId?: string;
  ownerUserName?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateShopPayload {
  name: string;
  description?: string;
  parentShopId?: string;
  address?: string;
  city?: string;
  countryCode?: string;
  stateId?: number;
  districtId?: number;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  phoneNumber?: string;
  website?: string;
  ownerUserId?: string;
}

export interface UpdateShopPayload {
  name: string;
  description?: string;
  address?: string;
  city?: string;
  countryCode?: string;
  stateId?: number;
  districtId?: number;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  phoneNumber?: string;
  website?: string;
  status: 'Active' | 'Inactive' | 'Archived';
}

export interface ShopsListResponse {
  pageNumber: number;
  pageSize: number;
  items: ShopDto[];
  total: number;
}
