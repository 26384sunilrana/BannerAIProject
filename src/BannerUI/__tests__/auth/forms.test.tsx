import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

const replace = jest.fn()
let search = ''

jest.mock('next/navigation', () => ({
  useRouter: () => ({ replace, push: jest.fn() }),
  useSearchParams: () => new URLSearchParams(search),
  usePathname: () => '/team',
}))

const login = jest.fn()
const register = jest.fn()
const mockAuth: { user: any; ready: boolean; hasRole: (r: string) => boolean } = {
  user: null,
  ready: true,
  hasRole: () => false,
}

jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({ ...mockAuth, login, register, logout: jest.fn() }),
}))

import { LoginForm } from '@/app/login/LoginForm'
import { RegisterForm, validateRegistration } from '@/app/register/RegisterForm'
import { RequireAuth } from '@/components/auth/RequireAuth'

beforeEach(() => {
  jest.clearAllMocks()
  search = ''
  mockAuth.user = null
  mockAuth.ready = true
  mockAuth.hasRole = () => false
})

describe('LoginForm', () => {
  it('asks for both fields before calling the API', async () => {
    render(<LoginForm />)

    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Enter your email and password.')
    expect(login).not.toHaveBeenCalled()
  })

  it('signs in and goes to the dashboard', async () => {
    login.mockResolvedValue(undefined)
    render(<LoginForm />)

    await userEvent.type(screen.getByLabelText('Email'), 'olive@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Password123!')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() => expect(replace).toHaveBeenCalledWith('/dashboard'))
    expect(login).toHaveBeenCalledWith({ email: 'olive@example.com', password: 'Password123!' })
  })

  it('returns to the page the visitor came from, but never to another site', async () => {
    login.mockResolvedValue(undefined)
    search = 'returnUrl=%2Fteam'
    const { unmount } = render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'a@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'x')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))
    await waitFor(() => expect(replace).toHaveBeenCalledWith('/team'))
    unmount()

    replace.mockClear()
    search = 'returnUrl=https%3A%2F%2Fevil.example.com'
    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'a@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'x')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))
    await waitFor(() => expect(replace).toHaveBeenCalledWith('/dashboard'))
  })

  it('shows the server message when sign in fails', async () => {
    login.mockRejectedValue({ response: { status: 401, data: { message: 'Invalid email or password' } } })
    render(<LoginForm />)

    await userEvent.type(screen.getByLabelText('Email'), 'a@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password')
    expect(replace).not.toHaveBeenCalled()
  })
})

describe('validateRegistration', () => {
  const valid = {
    firstName: 'Olive',
    lastName: 'Owner',
    email: 'olive@example.com',
    password: 'Password123!',
    confirmPassword: 'Password123!',
    shopName: 'Olive Mart',
    city: '',
    phoneNumber: '',
  }

  it('accepts a complete form', () => {
    expect(validateRegistration(valid)).toEqual({})
  })

  it('flags each problem', () => {
    const errors = validateRegistration({
      ...valid,
      firstName: ' ',
      email: 'nope',
      password: 'short',
      confirmPassword: 'different',
      shopName: '',
    })

    expect(Object.keys(errors).sort()).toEqual(['confirmPassword', 'email', 'firstName', 'password', 'shopName'])
  })
})

describe('RegisterForm', () => {
  const fill = async () => {
    await userEvent.type(screen.getByLabelText('First name'), 'Olive')
    await userEvent.type(screen.getByLabelText('Last name'), 'Owner')
    await userEvent.type(screen.getByLabelText('Email'), 'olive@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Password123!')
    await userEvent.type(screen.getByLabelText('Confirm password'), 'Password123!')
    await userEvent.type(screen.getByLabelText('Shop name'), '  Olive Mart ')
  }

  it('does not call the API while the form is invalid', async () => {
    render(<RegisterForm />)

    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(screen.getByText('Enter your shop name')).toBeInTheDocument()
    expect(register).not.toHaveBeenCalled()
  })

  it('registers the owner with a trimmed shop name and optional fields left out', async () => {
    register.mockResolvedValue(undefined)
    render(<RegisterForm />)

    await fill()
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    await waitFor(() => expect(replace).toHaveBeenCalledWith('/dashboard'))
    expect(register).toHaveBeenCalledWith(
      expect.objectContaining({ email: 'olive@example.com', shopName: '  Olive Mart ', city: undefined, phoneNumber: undefined })
    )
  })

  it('shows what the server says, such as a duplicate email', async () => {
    register.mockRejectedValue({ response: { status: 400, data: { message: 'Email already registered' } } })
    render(<RegisterForm />)

    await fill()
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Email already registered')
  })
})

describe('RequireAuth', () => {
  it('sends a visitor without a session to sign in', async () => {
    render(
      <RequireAuth>
        <p>secret</p>
      </RequireAuth>
    )

    await waitFor(() => expect(replace).toHaveBeenCalledWith('/login?returnUrl=%2Fteam'))
    expect(screen.queryByText('secret')).not.toBeInTheDocument()
  })

  it('shows the page to a signed-in user with the right role', () => {
    mockAuth.user = { id: '1', name: 'Olive', email: 'o@example.com', shopId: 's', roles: ['ShopOwner'] }
    mockAuth.hasRole = (role) => role === 'ShopOwner'

    render(
      <RequireAuth roles={['ShopOwner']}>
        <p>owner page</p>
      </RequireAuth>
    )

    expect(screen.getByText('owner page')).toBeInTheDocument()
    expect(replace).not.toHaveBeenCalled()
  })

  it('blocks a signed-in user whose role is not allowed', () => {
    mockAuth.user = { id: '2', name: 'Sam', email: 's@example.com', shopId: 's', roles: ['SalesExecutive'] }
    mockAuth.hasRole = (role) => role === 'SalesExecutive'

    render(
      <RequireAuth roles={['ShopOwner']}>
        <p>owner page</p>
      </RequireAuth>
    )

    expect(screen.queryByText('owner page')).not.toBeInTheDocument()
    expect(screen.getByText('Not available')).toBeInTheDocument()
  })
})
