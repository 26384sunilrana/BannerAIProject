import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

let search = ''
const replace = jest.fn()
jest.mock('next/navigation', () => ({
  useRouter: () => ({ replace, push: jest.fn() }),
  useSearchParams: () => new URLSearchParams(search),
  usePathname: () => '/subscription',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))

let signedIn = true
jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: signedIn ? { id: 'ME-1', email: 'me@example.com', name: 'Me', shopId: 'shop-1', roles: ['ShopOwner'] } : null,
    login: jest.fn(),
    loginWithCode: jest.fn(),
  }),
}))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))
jest.mock('@/api/subscriptionService', () => ({
  subscriptionService: { getPlans: jest.fn(), getCurrent: jest.fn(), renewNow: jest.fn(), setAutoRenew: jest.fn(), subscribe: jest.fn(), changePlan: jest.fn(), changeBillingPeriod: jest.fn() },
}))
jest.mock('@/api/paymentService', () => ({
  paymentService: { getConfig: jest.fn(), startCheckout: jest.fn(), confirm: jest.fn() },
}))
jest.mock('@/api/providerService', () => ({
  providerService: { getStatus: jest.fn(), sendTestEmail: jest.fn(), sendTestSms: jest.fn() },
}))
const driverOpen = jest.fn()
jest.mock('@/lib/checkout', () => {
  const actual = jest.requireActual('@/lib/checkout/types')
  return {
    CheckoutCancelled: actual.CheckoutCancelled,
    getCheckoutDriver: (provider: string | null | undefined) => (provider === 'Razorpay' ? { provider: 'Razorpay', open: driverOpen } : null),
  }
})

import { apiClient } from '@/api/client'
import { subscriptionService } from '@/api/subscriptionService'
import { paymentService } from '@/api/paymentService'
import { providerService } from '@/api/providerService'
import { CheckoutCancelled } from '@/lib/checkout/types'
import { ForgotPasswordForm } from '@/app/forgot-password/ForgotPasswordForm'
import { ResetPasswordForm } from '@/app/reset-password/ResetPasswordForm'
import { VerifyEmailView } from '@/app/verify-email/VerifyEmailView'
import { VerifyEmailBanner } from '@/components/layout/VerifyEmailBanner'
import AdminProvidersPage from '@/app/admin/providers/page'
import SubscriptionPage from '@/app/subscription/page'
import { LoginForm } from '@/app/login/LoginForm'
import { formatMoney } from '@/lib/pricing'

const api = apiClient as jest.Mocked<typeof apiClient>
const subs = subscriptionService as jest.Mocked<typeof subscriptionService>
const pay = paymentService as jest.Mocked<typeof paymentService>
const providers = providerService as jest.Mocked<typeof providerService>

beforeEach(() => {
  jest.clearAllMocks()
  search = ''
  signedIn = true
})

// ---------------------------------------------------------------------------------------------------------------- forgot password

describe('Forgot password', () => {
  it('asks for an address first', async () => {
    render(<ForgotPasswordForm />)
    await userEvent.click(screen.getByRole('button', { name: /send the link/i }))
    expect(screen.getByRole('alert')).toHaveTextContent(/enter the e-mail address/i)
    expect(api.post).not.toHaveBeenCalled()
  })

  it('sends the address and shows the same message whatever the answer says', async () => {
    api.post.mockResolvedValue({ message: 'If an account with that e-mail address exists, a link to reset the password has been sent' })
    render(<ForgotPasswordForm />)

    await userEvent.type(screen.getByLabelText('Email'), '  me@example.com ')
    await userEvent.click(screen.getByRole('button', { name: /send the link/i }))

    expect(api.post).toHaveBeenCalledWith('/authentication/forgot-password', { email: 'me@example.com' })
    expect(await screen.findByRole('status')).toHaveTextContent(/if an account with that e-mail address exists/i)
  })

  it('shows an error when the call fails', async () => {
    api.post.mockRejectedValue({ response: { data: { message: 'Too many requests. Please wait a minute and try again.' } } })
    render(<ForgotPasswordForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'me@example.com')
    await userEvent.click(screen.getByRole('button', { name: /send the link/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent(/too many requests/i)
  })

  it('is reachable from the sign-in page', () => {
    render(<LoginForm />)
    expect(screen.getByRole('link', { name: /forgot your password/i })).toHaveAttribute('href', '/forgot-password')
  })
})

// ---------------------------------------------------------------------------------------------------------------- reset password

describe('Reset password', () => {
  it('explains a link without a token', () => {
    render(<ResetPasswordForm />)
    expect(screen.getByRole('alert')).toHaveTextContent(/not complete/i)
    expect(screen.getByRole('link', { name: /ask for a new link/i })).toHaveAttribute('href', '/forgot-password')
  })

  it('takes the token out of the address bar', () => {
    search = 'token=abc123'
    const spy = jest.spyOn(window.history, 'replaceState')
    render(<ResetPasswordForm />)
    expect(spy).toHaveBeenCalled()
    spy.mockRestore()
  })

  it('checks length and matching before calling the server', async () => {
    search = 'token=abc123'
    render(<ResetPasswordForm />)

    await userEvent.type(screen.getByLabelText('New password'), 'short')
    await userEvent.type(screen.getByLabelText('Confirm new password'), 'short')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(screen.getByRole('alert')).toHaveTextContent(/at least 8 characters/i)

    await userEvent.clear(screen.getByLabelText('New password'))
    await userEvent.type(screen.getByLabelText('New password'), 'LongEnough1!')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(screen.getByRole('alert')).toHaveTextContent(/do not match/i)
    expect(api.post).not.toHaveBeenCalled()
  })

  it('changes the password with the token from the link and offers sign-in', async () => {
    search = 'token=abc123'
    api.post.mockResolvedValue({ message: 'Your password was changed. Please sign in with the new password.' })
    render(<ResetPasswordForm />)

    await userEvent.type(screen.getByLabelText('New password'), 'BrandNew123!')
    await userEvent.type(screen.getByLabelText('Confirm new password'), 'BrandNew123!')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))

    expect(api.post).toHaveBeenCalledWith('/authentication/reset-password', { token: 'abc123', newPassword: 'BrandNew123!', confirmPassword: 'BrandNew123!' })
    expect(await screen.findByRole('status')).toHaveTextContent(/password was changed/i)
    expect(screen.getAllByRole('link', { name: /sign in/i })[0]).toHaveAttribute('href', '/login')
  })

  it('shows the server reason when the link is no good', async () => {
    search = 'token=old'
    api.post.mockRejectedValue({ response: { data: { message: 'This link is not valid or has expired' } } })
    render(<ResetPasswordForm />)
    await userEvent.type(screen.getByLabelText('New password'), 'BrandNew123!')
    await userEvent.type(screen.getByLabelText('Confirm new password'), 'BrandNew123!')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent(/not valid or has expired/i)
  })
})

// ---------------------------------------------------------------------------------------------------------------- verify e-mail

describe('Verify e-mail', () => {
  it('uses the link once and says it worked', async () => {
    search = 'token=verify-me'
    api.post.mockResolvedValue({ message: 'Your e-mail address is verified' })
    render(<VerifyEmailView />)

    expect(await screen.findByRole('status')).toHaveTextContent(/verified/i)
    expect(api.post).toHaveBeenCalledTimes(1)
    expect(api.post).toHaveBeenCalledWith('/authentication/verify-email', { token: 'verify-me' })
  })

  it('says so when the link has expired', async () => {
    search = 'token=old'
    api.post.mockRejectedValue({ response: { data: { message: 'This link is not valid or has expired' } } })
    render(<VerifyEmailView />)
    expect(await screen.findByRole('alert')).toHaveTextContent(/not valid or has expired/i)
  })

  it('explains a link without a token, without calling the server', () => {
    render(<VerifyEmailView />)
    expect(screen.getByRole('alert')).toHaveTextContent(/not complete/i)
    expect(api.post).not.toHaveBeenCalled()
  })
})

describe('The verify-your-e-mail reminder', () => {
  it('shows only when mail can be sent and the address is not verified', async () => {
    api.get.mockResolvedValue({ emailVerified: false, emailEnabled: true })
    render(<VerifyEmailBanner />)
    expect(await screen.findByTestId('verify-email-banner')).toHaveTextContent(/verify your e-mail/i)
  })

  it.each([
    [{ emailVerified: true, emailEnabled: true }],
    [{ emailVerified: false, emailEnabled: false }],
  ])('stays away for %j', async (status) => {
    api.get.mockResolvedValue(status)
    render(<VerifyEmailBanner />)
    await waitFor(() => expect(api.get).toHaveBeenCalled())
    expect(screen.queryByTestId('verify-email-banner')).toBeNull()
  })

  it('stays away when nobody is signed in, and when the call fails', async () => {
    signedIn = false
    render(<VerifyEmailBanner />)
    expect(api.get).not.toHaveBeenCalled()

    signedIn = true
    api.get.mockRejectedValue(new Error('offline'))
    render(<VerifyEmailBanner />)
    await waitFor(() => expect(api.get).toHaveBeenCalled())
    expect(screen.queryByTestId('verify-email-banner')).toBeNull()
  })

  it('sends the link again on request', async () => {
    api.get.mockResolvedValue({ emailVerified: false, emailEnabled: true })
    api.post.mockResolvedValue({ message: 'A new link was sent to your e-mail address' })
    render(<VerifyEmailBanner />)

    await userEvent.click(await screen.findByRole('button', { name: /send the link again/i }))

    expect(api.post).toHaveBeenCalledWith('/authentication/resend-verification', {})
    expect(await screen.findByText(/a new link was sent/i)).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------------------------------------------- providers screen

describe('Providers screen', () => {
  const status = [
    { name: 'payments' as const, provider: 'Razorpay', configured: false, missing: ['Payments:Razorpay:KeySecret'] },
    { name: 'sms' as const, provider: 'None', configured: false, missing: [] },
    { name: 'email' as const, provider: 'Smtp', configured: true, missing: [] },
  ]

  it('shows what is connected and which settings are still empty', async () => {
    providers.getStatus.mockResolvedValue(status)
    render(<AdminProvidersPage />)

    const payments = await screen.findByTestId('provider-payments')
    expect(payments).toHaveTextContent('Not connected')
    expect(payments).toHaveTextContent('Payments:Razorpay:KeySecret')
    expect(screen.getByTestId('provider-email')).toHaveTextContent('Connected')
  })

  it('sends a test e-mail (to the administrator by default) and reports the result', async () => {
    providers.getStatus.mockResolvedValue(status)
    providers.sendTestEmail.mockResolvedValue({ sent: true })
    render(<AdminProvidersPage />)

    await userEvent.click(await screen.findByRole('button', { name: /send test e-mail/i }))

    expect(providers.sendTestEmail).toHaveBeenCalledWith('')
    expect(await screen.findByText(/test e-mail was sent/i)).toBeInTheDocument()
  })

  it('says why a test text was not sent', async () => {
    providers.getStatus.mockResolvedValue(status)
    providers.sendTestSms.mockResolvedValue({ sent: false, reason: 'SMS is not set up' })
    render(<AdminProvidersPage />)

    await screen.findByTestId('provider-sms')
    await userEvent.click(screen.getByRole('button', { name: /send test text/i }))
    expect(await screen.findByText(/enter the phone number/i)).toBeInTheDocument()
    expect(providers.sendTestSms).not.toHaveBeenCalled()

    await userEvent.type(screen.getByLabelText('Phone number'), '9876543210')
    await userEvent.click(screen.getByRole('button', { name: /send test text/i }))
    expect(providers.sendTestSms).toHaveBeenCalledWith('9876543210')
    expect(await screen.findByText('SMS is not set up')).toBeInTheDocument()
  })

  it('reports a failed status call', async () => {
    providers.getStatus.mockRejectedValue(new Error('boom'))
    render(<AdminProvidersPage />)
    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load/i)
  })
})

// ---------------------------------------------------------------------------------------------------------------- subscription screen

describe('Subscription screen with and without online payment', () => {
  const plan = { id: 'plan-1', name: 'Gold', description: 'Gold plan', monthlyPrice: 49, annualPrice: 499, currency: 'INR', isActive: true }
  const current = {
    id: 'sub-1', shopId: 'shop-1', planId: 'plan-1', planName: 'Gold', status: 2, billingPeriod: 2, currentPrice: 499, currency: 'INR',
    startDate: '2026-11-01T00:00:00Z', renewalDate: new Date(Date.now() + 60 * 86400000).toISOString(), autoRenew: true,
  }

  beforeEach(() => {
    subs.getPlans.mockResolvedValue([plan])
    subs.getCurrent.mockResolvedValue(current)
    subs.renewNow.mockResolvedValue()
  })

  it('without a provider keeps the Renew button and the automatic-renewal switch', async () => {
    pay.getConfig.mockResolvedValue({ enabled: false, provider: null, currency: 'INR', country: 'IN' })
    render(<SubscriptionPage />)

    await userEvent.click(await screen.findByRole('button', { name: /renew early/i }))

    expect(subs.renewNow).toHaveBeenCalledWith('sub-1')
    expect(pay.startCheckout).not.toHaveBeenCalled()
    expect(screen.getByLabelText(/renew automatically/i)).toBeInTheDocument()
  })

  it('when the payment call fails the screen still works as before', async () => {
    pay.getConfig.mockRejectedValue(new Error('offline'))
    render(<SubscriptionPage />)
    expect(await screen.findByRole('button', { name: /renew early/i })).toBeInTheDocument()
  })

  it('with a provider pays through the provider, then confirms with what the form returned', async () => {
    pay.getConfig.mockResolvedValue({ enabled: true, provider: 'Razorpay', currency: 'INR', country: 'IN' })
    const session = { provider: 'Razorpay', orderId: 'order_1', amount: 499, amountMinor: 49900, currency: 'INR', publicKeyId: 'rzp_test' }
    pay.startCheckout.mockResolvedValue(session)
    driverOpen.mockResolvedValue({ orderId: 'order_1', paymentId: 'pay_1', signature: 'sig' })
    pay.confirm.mockResolvedValue()
    render(<SubscriptionPage />)

    await userEvent.click(await screen.findByRole('button', { name: /pay and renew/i }))

    await waitFor(() => expect(pay.confirm).toHaveBeenCalledWith('sub-1', { orderId: 'order_1', paymentId: 'pay_1', signature: 'sig' }))
    expect(pay.startCheckout).toHaveBeenCalledWith('sub-1')
    expect(driverOpen).toHaveBeenCalledWith(session)
    expect(subs.renewNow).not.toHaveBeenCalled()
  })

  it('with a provider hides the automatic-renewal switch and says how renewal works', async () => {
    pay.getConfig.mockResolvedValue({ enabled: true, provider: 'Razorpay', currency: 'INR', country: 'IN' })
    render(<SubscriptionPage />)

    await screen.findByRole('button', { name: /pay and renew/i })
    expect(screen.queryByLabelText(/renew automatically/i)).toBeNull()
    expect(screen.getByText(/renewed by paying online/i)).toBeInTheDocument()
  })

  it('closing the payment form is not an error and confirms nothing', async () => {
    pay.getConfig.mockResolvedValue({ enabled: true, provider: 'Razorpay', currency: 'INR', country: 'IN' })
    pay.startCheckout.mockResolvedValue({ provider: 'Razorpay', orderId: 'o', amount: 499, amountMinor: 49900, currency: 'INR', publicKeyId: 'k' })
    driverOpen.mockRejectedValue(new CheckoutCancelled())
    render(<SubscriptionPage />)

    await userEvent.click(await screen.findByRole('button', { name: /pay and renew/i }))

    await waitFor(() => expect(driverOpen).toHaveBeenCalled())
    expect(pay.confirm).not.toHaveBeenCalled()
    expect(screen.queryByText(/did not work/i)).toBeNull()
  })

  it('shows the server message when the payment cannot be started', async () => {
    pay.getConfig.mockResolvedValue({ enabled: true, provider: 'Razorpay', currency: 'INR', country: 'IN' })
    pay.startCheckout.mockRejectedValue({ response: { data: { message: 'The payment service could not be reached. Please try again in a moment.' } } })
    render(<SubscriptionPage />)

    await userEvent.click(await screen.findByRole('button', { name: /pay and renew/i }))

    expect(await screen.findByText(/payment service could not be reached/i)).toBeInTheDocument()
  })

  it('a provider the web app has no form for falls back to the plain Renew button', async () => {
    pay.getConfig.mockResolvedValue({ enabled: true, provider: 'SomethingNew', currency: 'INR', country: 'IN' })
    render(<SubscriptionPage />)
    expect(await screen.findByRole('button', { name: /renew early/i })).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------------------------------------------- money

describe('Money in any currency', () => {
  it('is rupees by default, with Indian grouping', () => {
    expect(formatMoney(123456.5)).toMatch(/₹\s?1,23,456\.50/)
  })

  it('follows the currency it is given', () => {
    expect(formatMoney(499, 'USD')).toContain('$')
    expect(formatMoney(499, 'USD')).not.toContain('₹')
    expect(formatMoney(499, 'EUR')).toContain('€')
  })
})
