import React from 'react';
import Link from 'next/link';

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navigation */}
      <nav className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex justify-between items-center">
            <div className="flex items-center gap-8">
              <h2 className="text-2xl font-bold text-gray-900">Admin Dashboard</h2>
              <div className="flex gap-6">
                <Link
                  href="/admin/subscription-plans"
                  className="text-gray-600 hover:text-gray-900 font-medium"
                >
                  Subscription Plans
                </Link>
                {/* Additional admin sections can go here */}
              </div>
            </div>
            <div className="text-sm text-gray-500">
              <Link href="/" className="text-blue-600 hover:text-blue-800">
                Back to App
              </Link>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8">{children}</main>
    </div>
  );
}
