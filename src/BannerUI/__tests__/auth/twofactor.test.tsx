import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

const replace = jest.fn()
jest.mock('next/navigation', () => ({
  useRouter: () => ({ replace, push: jest.fn() }),
  useSearchParams: () => new URLSearchParams(''),
  usePathname: () => '/account',
}))

const login = jest.fn()
const loginWithCode = jest.fn()
jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({ user: null, ready: true, login, loginWithCode, register: jest.fn(), logout: jest.fn(), hasRole: () => false }),
}))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))
jest.mock('qrcode', () => ({ __esModule: true, default: { toDataURL: jest.fn().mockResolvedValue('data:image/png;base64,QR') } }))

import { apiClient } from '@/api/client'
import { LoginForm } from '@/app/login/LoginForm'
import { TwoFactorSection } from '@/components/auth/TwoFactorSection'
import { authService } from '@/api/authService'
import { decodeToken, getAccessToken, clearSession } from '@/lib/session'

const api = apiClient as jest.Mocked<typeof apiClient>

beforeEach(() => {
  jest.clearAllMocks()
  clearSession()
})

const token = (claims: Record<string, unknown>) => {
  const part = (o: unknown) => Buffer.from(JSON.stringify(o)).toString('base64url')
  return `${part({ alg: 'none' })}.${part({ sub: 'u1', email: 'a@example.com', exp: 4102444800, ...claims })}.sig`
}

describe('the sign-in service', () => {
  it('returns a challenge, and keeps no session, when a code is needed', async () => {
    api.post.mockResolvedValue({ message: 'Enter the code', requiresTwoFactor: true, challenge: 'chal-1' })

    const result = await authService.login({ email: 'a@example.com', password: 'Password123!' })

    expect(result).toEqual(expect.objectContaining({ requiresTwoFactor: true, challenge: 'chal-1' }))
    expect(getAccessToken()).toBeNull()
  })

  it('keeps the session once the code is accepted', async () => {
    api.post.mockResolvedValue({ message: 'ok', tokens: { accessToken: token({}), expiresIn: 900, tokenType: 'Bearer' } })

    await authService.completeTwoFactor('chal-1', ' 123456 ')

    expect(api.post).toHaveBeenCalledWith('/authentication/login/two-factor', { challenge: 'chal-1', code: '123456' })
    expect(getAccessToken()).not.toBeNull()
  })

  it('reads the claim that says an administrator still has to set it up', () => {
    expect(decodeToken(token({ two_factor_setup_required: 'true' }))?.twoFactorSetupRequired).toBe(true)
    expect(decodeToken(token({}))?.twoFactorSetupRequired).toBeUndefined()
  })
})

describe('LoginForm: the second step', () => {
  it('asks for the code after the password and finishes the sign-in with it', async () => {
    login.mockResolvedValue({ challenge: 'chal-1' })
    loginWithCode.mockResolvedValue(undefined)
    render(<LoginForm />)

    await userEvent.type(screen.getByLabelText('Email'), 'a@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Password123!')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByLabelText('Code')).toBeInTheDocument()
    expect(screen.queryByLabelText('Password')).toBeNull()
    expect(replace).not.toHaveBeenCalled()

    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Enter the code from your authenticator app.')

    await userEvent.type(screen.getByLabelText('Code'), '123456')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() => expect(loginWithCode).toHaveBeenCalledWith('chal-1', '123456'))
    expect(replace).toHaveBeenCalledWith('/dashboard')
  })

  it('shows why a code was refused, and lets the person start again', async () => {
    login.mockResolvedValue({ challenge: 'chal-1' })
    loginWithCode.mockRejectedValue({ response: { data: { message: 'That code is not right.' } } })
    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'a@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Password123!')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    await userEvent.type(await screen.findByLabelText('Code'), '000000')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('That code is not right.')

    await userEvent.click(screen.getByRole('button', { name: 'Use a different account' }))
    expect(screen.getByLabelText('Email')).toBeInTheDocument()
  })
})

describe('TwoFactorSection', () => {
  const off = { enabled: false, recoveryCodesLeft: 0, required: false }

  it('walks through the set-up: password, picture and secret, first code, recovery codes', async () => {
    api.get.mockResolvedValue(off)
    api.post.mockImplementation(async (path: string) => {
      if (path.endsWith('/setup')) return { secret: 'ABCD EFGH IJKL', uri: 'otpauth://totp/BannerAI:a@example.com?secret=ABCDEFGHIJKL' }
      if (path.endsWith('/enable')) return { codes: ['AAAAA-BBBBB', 'CCCCC-DDDDD'] }
      throw new Error(path)
    })
    render(<TwoFactorSection />)
    expect(await screen.findByTestId('two-factor-status')).toHaveTextContent('It is off.')

    await userEvent.click(screen.getByRole('button', { name: 'Set up two-step sign-in' }))
    await userEvent.type(screen.getByLabelText('Your password'), 'Password123!')
    await userEvent.click(screen.getByRole('button', { name: 'Continue' }))

    expect(await screen.findByTestId('two-factor-secret')).toHaveTextContent('ABCD EFGH IJKL')
    expect(screen.getByAltText('QR picture for the authenticator app')).toHaveAttribute('src', 'data:image/png;base64,QR')
    expect(api.post).toHaveBeenCalledWith('/account/two-factor/setup', { password: 'Password123!' })

    await userEvent.type(screen.getByLabelText('Code from the app'), '123456')
    api.get.mockResolvedValue({ enabled: true, recoveryCodesLeft: 2, required: false })
    await userEvent.click(screen.getByRole('button', { name: 'Turn it on' }))

    const codes = await screen.findByTestId('recovery-codes')
    expect(codes).toHaveTextContent('AAAAA-BBBBB')
    expect(codes).toHaveTextContent('CCCCC-DDDDD')
    expect(api.post).toHaveBeenCalledWith('/account/two-factor/enable', { code: '123456' })

    await userEvent.click(screen.getByRole('button', { name: 'I have saved them' }))
    expect(await screen.findByTestId('two-factor-status')).toHaveTextContent('It is on. 2 recovery codes left.')
  })

  it('shows why the password or the first code was refused', async () => {
    api.get.mockResolvedValue(off)
    api.post.mockRejectedValue({ response: { data: { message: 'Your password is not right.' } } })
    render(<TwoFactorSection />)
    await userEvent.click(await screen.findByRole('button', { name: 'Set up two-step sign-in' }))
    await userEvent.type(screen.getByLabelText('Your password'), 'nope')

    await userEvent.click(screen.getByRole('button', { name: 'Continue' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Your password is not right.')
  })

  it('switches it off with the password and a code, and makes new recovery codes', async () => {
    api.get.mockResolvedValue({ enabled: true, recoveryCodesLeft: 4, required: false })
    api.post.mockImplementation(async (path: string) => (path.endsWith('/recovery-codes') ? { codes: ['NEWNE-WCODE'] } : { message: 'off' }))
    render(<TwoFactorSection />)
    expect(await screen.findByTestId('two-factor-status')).toHaveTextContent('It is on. 4 recovery codes left.')

    await userEvent.click(screen.getByRole('button', { name: 'New recovery codes' }))
    await userEvent.type(screen.getByLabelText('Your password'), 'Password123!')
    await userEvent.type(screen.getByLabelText('Code from the app'), '111111')
    await userEvent.click(screen.getByRole('button', { name: 'Make new recovery codes' }))
    expect(await screen.findByTestId('recovery-codes')).toHaveTextContent('NEWNE-WCODE')
    expect(api.post).toHaveBeenCalledWith('/account/two-factor/recovery-codes', { password: 'Password123!', code: '111111' })
    await userEvent.click(screen.getByRole('button', { name: 'I have saved them' }))

    await userEvent.click(await screen.findByRole('button', { name: 'Switch it off' }))
    await userEvent.type(screen.getByLabelText('Your password'), 'Password123!')
    await userEvent.type(screen.getByLabelText('Code from the app'), '222222')
    api.get.mockResolvedValue(off)
    await userEvent.click(screen.getByRole('button', { name: 'Switch it off' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/account/two-factor/disable', { password: 'Password123!', code: '222222' }))
    expect(await screen.findByTestId('two-factor-status')).toHaveTextContent('It is off.')
  })

  it('does not offer to switch it off to an administrator, and says it is required', async () => {
    api.get.mockResolvedValue({ enabled: true, recoveryCodesLeft: 8, required: true })
    render(<TwoFactorSection />)

    expect(await screen.findByText('Administrators cannot switch it off.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Switch it off' })).toBeNull()
  })

  it('tells an administrator who has to set it up that nothing else is open', async () => {
    api.get.mockResolvedValue({ enabled: false, recoveryCodesLeft: 0, required: true })
    render(<TwoFactorSection forced />)

    expect(await screen.findByTestId('two-factor-required')).toHaveTextContent('nothing else is open until it is on')
  })
})
