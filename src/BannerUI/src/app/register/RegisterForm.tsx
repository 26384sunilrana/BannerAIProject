'use client'

import React, { FormEvent, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { AuthCard, FormError } from '@/components/auth/AuthCard'
import { Button, Input } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { getErrorMessage } from '@/api/client'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export interface RegistrationValues {
  firstName: string
  lastName: string
  email: string
  password: string
  confirmPassword: string
  shopName: string
  city: string
  phoneNumber: string
}

type FieldErrors = Partial<Record<keyof RegistrationValues, string>>

export function validateRegistration(values: RegistrationValues): FieldErrors {
  const errors: FieldErrors = {}
  if (!values.firstName.trim()) errors.firstName = 'Enter your first name'
  if (!values.lastName.trim()) errors.lastName = 'Enter your last name'
  if (!EMAIL_PATTERN.test(values.email.trim())) errors.email = 'Enter a valid email address'
  if (values.password.length < 8) errors.password = 'Use at least 8 characters'
  if (values.confirmPassword !== values.password) errors.confirmPassword = 'Passwords do not match'
  if (!values.shopName.trim()) errors.shopName = 'Enter your shop name'
  return errors
}

export function RegisterForm() {
  const { register } = useAuth()
  const router = useRouter()

  const [values, setValues] = useState<RegistrationValues>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
    shopName: '',
    city: '',
    phoneNumber: '',
  })
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const set = (field: keyof RegistrationValues) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setValues((prev) => ({ ...prev, [field]: e.target.value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setFormError(null)

    const found = validateRegistration(values)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setBusy(true)
    try {
      await register({
        email: values.email,
        password: values.password,
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        shopName: values.shopName,
        city: values.city.trim() || undefined,
        phoneNumber: values.phoneNumber.trim() || undefined,
      })
      router.replace('/dashboard')
    } catch (err) {
      setFormError(getErrorMessage(err, 'Could not create your account. Please try again.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard
      title="Create your shop account"
      subtitle="You will be the owner of the shop and can add up to two sales executives."
      footer={
        <>
          Already registered?{' '}
          <Link href="/login" className="text-blue-600 hover:text-blue-800 font-medium">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={submit} noValidate className="space-y-4">
        <FormError message={formError} />
        <div className="grid grid-cols-2 gap-3">
          <Input id="firstName" label="First name" autoComplete="given-name" value={values.firstName} onChange={set('firstName')} error={errors.firstName} />
          <Input id="lastName" label="Last name" autoComplete="family-name" value={values.lastName} onChange={set('lastName')} error={errors.lastName} />
        </div>
        <Input id="email" type="email" label="Email" autoComplete="email" value={values.email} onChange={set('email')} error={errors.email} />
        <Input id="password" type="password" label="Password" autoComplete="new-password" helperText="At least 8 characters" value={values.password} onChange={set('password')} error={errors.password} />
        <Input id="confirmPassword" type="password" label="Confirm password" autoComplete="new-password" value={values.confirmPassword} onChange={set('confirmPassword')} error={errors.confirmPassword} />
        <Input id="shopName" label="Shop name" value={values.shopName} onChange={set('shopName')} error={errors.shopName} />
        <div className="grid grid-cols-2 gap-3">
          <Input id="city" label="City (optional)" autoComplete="address-level2" value={values.city} onChange={set('city')} />
          <Input id="phoneNumber" type="tel" label="Phone (optional)" autoComplete="tel" value={values.phoneNumber} onChange={set('phoneNumber')} />
        </div>
        <Button type="submit" isLoading={busy} className="w-full">
          Create account
        </Button>
      </form>
    </AuthCard>
  )
}
