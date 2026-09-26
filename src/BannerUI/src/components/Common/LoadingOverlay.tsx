'use client'

import React from 'react'

export interface LoadingOverlayProps {
  isVisible: boolean
  message?: string
  progress?: number
}

export function LoadingOverlay({ isVisible, message, progress }: LoadingOverlayProps) {
  if (!isVisible) return null

  return (
    <div className="fixed inset-0 bg-black bg-opacity-30 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl p-8 max-w-sm w-full mx-4">
        <div className="flex flex-col items-center gap-4">
          <div className="relative w-12 h-12">
            <div className="absolute inset-0 rounded-full border-4 border-gray-200" />
            <div className="absolute inset-0 rounded-full border-4 border-blue-600 border-t-transparent animate-spin" />
          </div>

          {message && (
            <p className="text-gray-700 text-center font-medium">{message}</p>
          )}

          {typeof progress === 'number' && (
            <div className="w-full">
              <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
                <div
                  className="h-full bg-blue-600 transition-all duration-300"
                  style={{ width: `${progress}%` }}
                />
              </div>
              <p className="text-xs text-gray-500 text-center mt-2">{Math.round(progress)}%</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
