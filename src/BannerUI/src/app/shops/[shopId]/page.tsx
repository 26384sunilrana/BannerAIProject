'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { ShopDto } from '@/types/shop';
import { useShops } from '@/hooks/useShops';
import Link from 'next/link';

export default function ShopDetailPage() {
  const router = useRouter();
  const params = useParams();
  const shopId = params.shopId as string;
  const { loading, getShopById } = useShops();
  const [shop, setShop] = useState<ShopDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadShop();
  }, []);

  const loadShop = async () => {
    try {
      const data = await getShopById(shopId);
      setShop(data);
    } catch (err) {
      setError('Failed to load shop details');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading shop details...</div>;
  }

  if (!shop) {
    return <div className="text-center py-8 text-red-600">Shop not found</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{shop.name}</h1>
          <p className="text-gray-600 mt-1">{shop.description}</p>
        </div>
        <div className="flex gap-2">
          <Link
            href={`/shops/${shopId}/edit`}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700"
          >
            Edit
          </Link>
          <button
            onClick={() => router.back()}
            className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg font-semibold hover:bg-gray-50"
          >
            Back
          </button>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-800">{error}</div>}

      {/* Status Badge */}
      <div className="inline-block">
        <span
          className={`px-3 py-1 rounded-full text-sm font-semibold ${
            shop.status === 'Active'
              ? 'bg-green-100 text-green-800'
              : shop.status === 'Inactive'
                ? 'bg-yellow-100 text-yellow-800'
                : 'bg-gray-100 text-gray-800'
          }`}
        >
          {shop.status}
        </span>
      </div>

      {/* Location Information */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Location</h2>
        <div className="grid grid-cols-2 gap-4">
          {shop.countryName && (
            <div>
              <p className="text-sm text-gray-600">Country</p>
              <p className="text-lg font-semibold text-gray-900">{shop.countryName}</p>
            </div>
          )}
          {shop.stateName && (
            <div>
              <p className="text-sm text-gray-600">State</p>
              <p className="text-lg font-semibold text-gray-900">{shop.stateName}</p>
            </div>
          )}
          {shop.districtName && (
            <div>
              <p className="text-sm text-gray-600">District</p>
              <p className="text-lg font-semibold text-gray-900">{shop.districtName}</p>
            </div>
          )}
          {shop.city && (
            <div>
              <p className="text-sm text-gray-600">City</p>
              <p className="text-lg font-semibold text-gray-900">{shop.city}</p>
            </div>
          )}
          {shop.address && (
            <div className="col-span-2">
              <p className="text-sm text-gray-600">Street Address</p>
              <p className="text-gray-900">{shop.address}</p>
            </div>
          )}
          {shop.postalCode && (
            <div>
              <p className="text-sm text-gray-600">Postal Code</p>
              <p className="text-gray-900">{shop.postalCode}</p>
            </div>
          )}
        </div>
      </div>

      {/* Contact Information */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Contact</h2>
        <div className="space-y-3">
          {shop.phoneNumber && (
            <p>
              <span className="text-gray-600">Phone:</span> <span className="text-gray-900">{shop.phoneNumber}</span>
            </p>
          )}
          {shop.website && (
            <p>
              <span className="text-gray-600">Website:</span>{' '}
              <a href={shop.website} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
                {shop.website}
              </a>
            </p>
          )}
        </div>
      </div>

      {/* Geo-Coordinates */}
      {(shop.latitude || shop.longitude) && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-bold text-gray-900 mb-4">Geo-Location</h2>
          <div className="grid grid-cols-2 gap-4">
            {shop.latitude && (
              <div>
                <p className="text-sm text-gray-600">Latitude</p>
                <p className="text-gray-900">{shop.latitude.toFixed(4)}</p>
              </div>
            )}
            {shop.longitude && (
              <div>
                <p className="text-sm text-gray-600">Longitude</p>
                <p className="text-gray-900">{shop.longitude.toFixed(4)}</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Sub-Shops */}
      {shop.childShopsCount > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-bold text-gray-900 mb-4">
            Sub-Shops ({shop.childShopsCount})
          </h2>
          <p className="text-gray-600">This shop has {shop.childShopsCount} sub-location(s).</p>
        </div>
      )}

      {/* Metadata */}
      <div className="bg-gray-50 rounded-lg p-4 text-sm text-gray-600">
        <p>Created: {new Date(shop.createdAt).toLocaleDateString()}</p>
        <p>Last Updated: {new Date(shop.updatedAt).toLocaleDateString()}</p>
      </div>
    </div>
  );
}
