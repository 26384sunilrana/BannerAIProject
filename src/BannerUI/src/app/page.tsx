'use client'

import { useEffect } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/context/AuthContext'

export default function Home() {
  const { user, ready } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (ready && user) router.replace('/dashboard')
  }, [ready, user, router])

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-50 px-4">
      <div className="text-center max-w-xl">
        <h1 className="text-4xl font-bold text-gray-900 mb-4">Banner AI</h1>
        <p className="text-xl text-gray-600 mb-8">
          Design digital banners with drag and drop, send them for approval, and publish them to your shop display.
        </p>

        <div className="flex flex-wrap justify-center gap-3">
          <Link
            href="/register"
            className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium"
          >
            Create your shop account
          </Link>
          <Link
            href="/login"
            className="px-6 py-3 bg-white text-gray-900 border border-gray-300 rounded-lg hover:bg-gray-100 transition font-medium"
          >
            Sign in
          </Link>
        </div>
      </div>
    </div>
  )
}
