import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AddressForm } from '@/components/address/AddressForm'

const countries = [
  { isoCode: 'IN', name: 'India', isActive: true },
  { isoCode: 'US', name: 'United States', isActive: true },
]
const states = [
  { id: 1, countryCode: 'IN', code: 'MH', name: 'Maharashtra', regionType: 'State', isActive: true },
  { id: 2, countryCode: 'IN', code: 'DL', name: 'Delhi', isActive: true },
]
const districts = [
  { id: 10, stateId: 1, code: 'MUM', name: 'Mumbai', isActive: true },
  { id: 11, stateId: 1, code: 'THN', name: 'Thane', isActive: true },
]

/** Answers each address lookup the way the API does, and records what was asked. */
function mockApi(overrides: Record<string, number> = {}) {
  const asked: string[] = []
  global.fetch = jest.fn(async (url: string) => {
    asked.push(url.replace(/^.*\/api/, ''))
    const status = Object.entries(overrides).find(([part]) => url.includes(part))?.[1] ?? 200
    const body = url.endsWith('/address/countries')
      ? { data: countries }
      : /\/countries\/\w+\/states$/.test(url)
        ? { data: states }
        : /\/states\/\d+\/districts$/.test(url)
          ? { data: districts }
          : { data: [] }
    return { ok: status < 400, status, statusText: 'x', json: async () => body }
  }) as unknown as typeof fetch
  return asked
}

describe('AddressForm', () => {
  const onSubmit = jest.fn()
  const originalFetch = global.fetch

  beforeEach(() => jest.clearAllMocks())
  afterEach(() => {
    global.fetch = originalFetch
  })

  it('loads countries and then the states of the chosen country', async () => {
    const asked = mockApi()
    render(<AddressForm onSubmit={onSubmit} />)

    expect(await screen.findByRole('option', { name: 'India' })).toBeInTheDocument()
    expect(asked).toContain('/address/countries')
    // India is the default, so its states load straight away
    expect(await screen.findByRole('option', { name: /Maharashtra/ })).toBeInTheDocument()
    expect(asked).toContain('/address/countries/IN/states')
  })

  it('loads districts when a state is chosen, and clears them when the country changes', async () => {
    mockApi()
    render(<AddressForm onSubmit={onSubmit} />)
    await screen.findByRole('option', { name: /Maharashtra/ })

    expect(screen.getByLabelText(/district/i)).toBeDisabled()
    fireEvent.change(screen.getByLabelText(/state/i), { target: { value: '1' } })
    expect(await screen.findByRole('option', { name: 'Mumbai' })).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/country/i), { target: { value: 'US' } })
    await waitFor(() => expect(screen.queryByRole('option', { name: 'Mumbai' })).toBeNull())
    expect(screen.getByLabelText(/state/i)).toHaveValue('')
  })

  it('keeps the saved state and district when editing', async () => {
    mockApi()
    render(<AddressForm initialData={{ countryCode: 'IN', stateId: 1, districtId: 11, city: 'Thane', address: '1 Main Road' }} onSubmit={onSubmit} />)

    await screen.findByRole('option', { name: 'Thane' })
    await waitFor(() => expect(screen.getByLabelText(/state/i)).toHaveValue('1'))
    expect(screen.getByLabelText(/district/i)).toHaveValue('11')
    expect(screen.getByLabelText(/^city/i)).toHaveValue('Thane')
  })

  it('asks for a state before submitting', async () => {
    mockApi()
    render(<AddressForm onSubmit={onSubmit} />)
    await screen.findByRole('option', { name: /Maharashtra/ })

    fireEvent.click(await screen.findByRole('button', { name: 'Save Address' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Choose a state')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits ids as numbers', async () => {
    mockApi()
    render(<AddressForm onSubmit={onSubmit} />)
    await screen.findByRole('option', { name: /Maharashtra/ })

    fireEvent.change(screen.getByLabelText(/state/i), { target: { value: '1' } })
    await screen.findByRole('option', { name: 'Mumbai' })
    fireEvent.change(screen.getByLabelText(/district/i), { target: { value: '10' } })
    await userEvent.type(screen.getByLabelText(/^city/i), 'Mumbai')
    await userEvent.type(screen.getByLabelText(/postal code/i), '400001')
    fireEvent.click(await screen.findByRole('button', { name: 'Save Address' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ countryCode: 'IN', stateId: 1, districtId: 10, city: 'Mumbai', postalCode: '400001' })
    )
  })

  it('leaves the postal code optional', async () => {
    mockApi()
    render(<AddressForm onSubmit={onSubmit} />)
    await screen.findByRole('option', { name: /Maharashtra/ })

    fireEvent.change(screen.getByLabelText(/state/i), { target: { value: '2' } })
    fireEvent.click(await screen.findByRole('button', { name: 'Save Address' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
    expect(onSubmit.mock.calls[0][0].postalCode || undefined).toBeUndefined()
  })

  it('shows geo fields only when asked for', async () => {
    mockApi()
    const { unmount } = render(<AddressForm onSubmit={onSubmit} />)
    expect(screen.getByLabelText(/latitude/i)).toBeInTheDocument()
    unmount()

    render(<AddressForm onSubmit={onSubmit} includeGeo={false} />)
    expect(screen.queryByLabelText(/latitude/i)).toBeNull()
  })

  it('says so when the lookups fail', async () => {
    mockApi({ '/address/countries': 500 })
    render(<AddressForm onSubmit={onSubmit} />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/failed to fetch/i)
  })
})
