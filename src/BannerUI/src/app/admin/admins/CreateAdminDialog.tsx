'use client'

import React, { useState } from 'react'
import { adminService } from '@/api/adminService'
import { getErrorMessage } from '@/api/client'

export function CreateAdminDialog({
  onClose,
  onSuccess,
  onError,
}: {
  onClose: () => void
  onSuccess: () => void
  onError: (error: string) => void
}) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [fieldError, setFieldError] = useState<Record<string, string>>({})

  const validateForm = () => {
    const errors: Record<string, string> = {}

    if (!email.trim()) {
      errors.email = 'Email is required'
    } else if (!email.includes('@')) {
      errors.email = 'Please enter a valid email'
    }

    if (!password.trim()) {
      errors.password = 'Password is required'
    } else if (password.length < 10) {
      errors.password = 'Password must be at least 10 characters'
    }

    if (!firstName.trim()) {
      errors.firstName = 'First name is required'
    }

    if (!lastName.trim()) {
      errors.lastName = 'Last name is required'
    }

    setFieldError(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) {
      return
    }

    setIsSubmitting(true)
    try {
      await adminService.createAdmin({
        email: email.trim().toLowerCase(),
        password,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
      })
      onSuccess()
    } catch (err) {
      onError(getErrorMessage(err, 'Could not create administrator'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50 z-50">
      <div className="bg-white rounded-lg shadow-lg p-6 max-w-md w-full mx-4">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Create Administrator</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value)
                if (fieldError.email) setFieldError({ ...fieldError, email: '' })
              }}
              placeholder="admin@example.com"
              className="w-full rounded-lg border border-gray-300 p-2 text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
              disabled={isSubmitting}
            />
            {fieldError.email && <p className="mt-1 text-xs text-red-600">{fieldError.email}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password (min 10 characters)</label>
            <input
              type="password"
              value={password}
              onChange={(e) => {
                setPassword(e.target.value)
                if (fieldError.password) setFieldError({ ...fieldError, password: '' })
              }}
              placeholder="••••••••••"
              className="w-full rounded-lg border border-gray-300 p-2 text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
              disabled={isSubmitting}
            />
            {fieldError.password && <p className="mt-1 text-xs text-red-600">{fieldError.password}</p>}
            {password && (
              <p
                className={`mt-1 text-xs ${
                  password.length >= 10 ? 'text-green-600' : 'text-amber-600'
                }`}
              >
                {password.length} characters
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input
                type="text"
                value={firstName}
                onChange={(e) => {
                  setFirstName(e.target.value)
                  if (fieldError.firstName) setFieldError({ ...fieldError, firstName: '' })
                }}
                placeholder="John"
                className="w-full rounded-lg border border-gray-300 p-2 text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                disabled={isSubmitting}
              />
              {fieldError.firstName && <p className="mt-1 text-xs text-red-600">{fieldError.firstName}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input
                type="text"
                value={lastName}
                onChange={(e) => {
                  setLastName(e.target.value)
                  if (fieldError.lastName) setFieldError({ ...fieldError, lastName: '' })
                }}
                placeholder="Doe"
                className="w-full rounded-lg border border-gray-300 p-2 text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                disabled={isSubmitting}
              />
              {fieldError.lastName && <p className="mt-1 text-xs text-red-600">{fieldError.lastName}</p>}
            </div>
          </div>

          <div className="flex gap-3 justify-end pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50"
            >
              {isSubmitting ? 'Creating...' : 'Create Administrator'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
