'use client';

import React from 'react';
import Link from 'next/link';
import { ShopDto } from '@/types/shop';

interface ShopCardProps {
  shop: ShopDto;
  onEdit: (shop: ShopDto) => void;
  onDeactivate: (shop: ShopDto) => void;
}

export const ShopCard: React.FC<ShopCardProps> = ({ shop, onEdit, onDeactivate }) => {
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Inactive':
        return 'bg-yellow-100 text-yellow-800';
      case 'Archived':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const location = [shop.city, shop.stateName, shop.countryName].filter(Boolean).join(', ');

  return (
    <div className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
      {/* Header */}
      <div className="flex justify-between items-start mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-bold text-gray-900">{shop.name}</h3>
          {location && <p className="text-sm text-gray-600 mt-1">{location}</p>}
        </div>
        <span className={`px-3 py-1 rounded-full text-xs font-semibold ${getStatusColor(shop.status)}`}>
          {shop.status}
        </span>
      </div>

      {/* Description */}
      {shop.description && <p className="text-sm text-gray-700 mb-4 line-clamp-2">{shop.description}</p>}

      {/* Address */}
      {shop.address && <p className="text-xs text-gray-500 mb-4">{shop.address}</p>}

      {/* Contact Info */}
      {(shop.phoneNumber || shop.website) && (
        <div className="mb-4 pb-4 border-t border-gray-200 pt-4">
          {shop.phoneNumber && <p className="text-sm text-gray-700">☎ {shop.phoneNumber}</p>}
          {shop.website && (
            <p className="text-sm text-blue-600 hover:underline">
              <a href={shop.website} target="_blank" rel="noopener noreferrer">
                {shop.website}
              </a>
            </p>
          )}
        </div>
      )}

      {/* Stats */}
      {shop.childShopsCount > 0 && (
        <div className="mb-4 text-sm text-gray-600">
          📍 {shop.childShopsCount} sub-shop{shop.childShopsCount !== 1 ? 's' : ''}
        </div>
      )}

      {/* Actions */}
      <div className="flex gap-2">
        <Link
          href={`/shops/${shop.id}`}
          className="flex-1 px-3 py-2 bg-blue-50 text-blue-600 rounded-lg text-sm font-semibold hover:bg-blue-100 text-center"
        >
          View
        </Link>
        <button
          onClick={() => onEdit(shop)}
          className="flex-1 px-3 py-2 bg-gray-50 text-gray-700 rounded-lg text-sm font-semibold hover:bg-gray-100"
        >
          Edit
        </button>
        {shop.status === 'Active' && (
          <button
            onClick={() => onDeactivate(shop)}
            className="px-3 py-2 bg-red-50 text-red-600 rounded-lg text-sm font-semibold hover:bg-red-100"
          >
            Deactivate
          </button>
        )}
      </div>
    </div>
  );
};
