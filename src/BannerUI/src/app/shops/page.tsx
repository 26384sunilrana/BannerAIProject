'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { ShopDto } from '@/types/shop';
import { useShops } from '@/hooks/useShops';
import { ShopCard } from '@/components/shops/ShopCard';
import { useAuth } from '@/context/AuthContext';
import { Roles } from '@/lib/session';

export default function MyShopsPage() {
  const router = useRouter();
  const { loading, error, getMyShops, getAllShops, deactivateShop } = useShops();
  const { hasRole } = useAuth();
  const isAdmin = hasRole(Roles.Admin);
  const [shops, setShops] = useState<ShopDto[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalShops, setTotalShops] = useState(0);
  const pageSize = 6;

  useEffect(() => {
    loadShops();
  }, [statusFilter, pageNumber]);

  const loadShops = async () => {
    try {
      const response = await (isAdmin ? getAllShops : getMyShops)(pageNumber, pageSize, statusFilter || undefined);
      setShops(response.items);
      setTotalShops(response.total);
    } catch (err) {
      setMessage({ type: 'error', text: 'Failed to load shops' });
    }
  };

  const handleEdit = (shop: ShopDto) => {
    router.push(`/shops/${shop.id}/edit`);
  };

  const handleDeactivate = async (shop: ShopDto) => {
    if (!confirm(`Are you sure you want to deactivate "${shop.name}"?`)) return;

    try {
      await deactivateShop(shop.id);
      setMessage({ type: 'success', text: `"${shop.name}" has been deactivated` });
      await loadShops();
    } catch (err) {
      setMessage({ type: 'error', text: `Failed to deactivate shop: ${error}` });
    }
  };

  const totalPages = Math.ceil(totalShops / pageSize);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{isAdmin ? 'All shops' : 'My Shops'}</h1>
          <p className="text-gray-600 mt-1">{isAdmin ? 'Every shop on the platform' : 'Manage all your shop locations'}</p>
        </div>
        {isAdmin && (
          <Link
            href="/shops/new"
            className="px-6 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700"
          >
            + Create Shop
          </Link>
        )}
      </div>

      {/* Messages */}
      {message && (
        <div
          className={`p-4 rounded-lg ${
            message.type === 'success'
              ? 'bg-green-50 text-green-800 border border-green-200'
              : 'bg-red-50 text-red-800 border border-red-200'
          }`}
        >
          {message.text}
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-gray-600 text-sm">Total Shops</p>
          <p className="text-3xl font-bold text-gray-900">{totalShops}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-gray-600 text-sm">Active</p>
          <p className="text-3xl font-bold text-green-600">
            {shops.filter((s) => s.status === 'Active').length}
          </p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-gray-600 text-sm">Inactive</p>
          <p className="text-3xl font-bold text-yellow-600">
            {shops.filter((s) => s.status !== 'Active').length}
          </p>
        </div>
      </div>

      {/* Filter */}
      <div className="flex gap-2">
        <button
          onClick={() => {
            setStatusFilter('');
            setPageNumber(1);
          }}
          className={`px-4 py-2 rounded-lg font-medium ${
            statusFilter === ''
              ? 'bg-blue-600 text-white'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
          }`}
        >
          All
        </button>
        <button
          onClick={() => {
            setStatusFilter('Active');
            setPageNumber(1);
          }}
          className={`px-4 py-2 rounded-lg font-medium ${
            statusFilter === 'Active'
              ? 'bg-blue-600 text-white'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
          }`}
        >
          Active
        </button>
        <button
          onClick={() => {
            setStatusFilter('Inactive');
            setPageNumber(1);
          }}
          className={`px-4 py-2 rounded-lg font-medium ${
            statusFilter === 'Inactive'
              ? 'bg-blue-600 text-white'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
          }`}
        >
          Inactive
        </button>
      </div>

      {/* Shops Grid */}
      {loading ? (
        <div className="text-center py-12">Loading shops...</div>
      ) : shops.length === 0 ? (
        <div className="text-center py-12 bg-gray-50 rounded-lg">
          <p className="text-gray-600 mb-4">No shops found</p>
          {isAdmin && (
            <Link
              href="/shops/new"
              className="inline-block px-6 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700"
            >
              Create the first shop
            </Link>
          )}
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {shops.map((shop) => (
              <ShopCard
                key={shop.id}
                shop={shop}
                onEdit={handleEdit}
                onDeactivate={handleDeactivate}
              />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex justify-center items-center gap-2">
              <button
                onClick={() => setPageNumber(Math.max(1, pageNumber - 1))}
                disabled={pageNumber === 1}
                className="px-4 py-2 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Previous
              </button>
              <div className="flex gap-1">
                {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                  <button
                    key={page}
                    onClick={() => setPageNumber(page)}
                    className={`px-3 py-2 rounded-lg ${
                      pageNumber === page
                        ? 'bg-blue-600 text-white'
                        : 'border border-gray-300 hover:bg-gray-50'
                    }`}
                  >
                    {page}
                  </button>
                ))}
              </div>
              <button
                onClick={() => setPageNumber(Math.min(totalPages, pageNumber + 1))}
                disabled={pageNumber === totalPages}
                className="px-4 py-2 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
