import React from 'react'
import { render, screen, waitFor, within, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/banners',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))

let mockZone = { timeZoneId: 'Asia/Kolkata', source: 'city', ownTimeZoneId: null as string | null, loaded: true }
jest.mock('@/hooks/useShopTimeZone', () => ({ useShopTimeZone: () => mockZone, forgetShopTimeZone: jest.fn() }))
jest.mock('@/context/AuthContext', () => ({ useAuth: () => ({ user: { id: 'u1', email: 'o@example.com', name: 'O', shopId: 'shop-1', roles: ['ShopOwner'] } }) }))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))
jest.mock('@/api/workflowService', () => {
  const actual = jest.requireActual('@/api/workflowService')
  return { ...actual, bannerListService: { setSchedule: jest.fn(), getCalendar: jest.fn() } }
})
jest.mock('@/components/media/MediaPickerDialog', () => ({
  MediaPickerDialog: ({ onSelect }: { onSelect: (item: unknown) => void }) => (
    <button onClick={() => onSelect({ id: 'logo-1', fileName: 'logo.png', url: 'http://x/logo.png' })}>pick logo</button>
  ),
}))

import { apiClient } from '@/api/client'
import { bannerListService } from '@/api/workflowService'
import { defaultBoardService } from '@/api/defaultBoardService'
import { SchedulePicker } from '@/app/banners/SchedulePicker'
import CalendarPage from '@/app/banners/calendar/page'
import DefaultBoardPage from '@/app/default-board/page'
import { DefaultBoard } from '@/components/Display/DefaultBoard'
import { TimeZoneSelect } from '@/components/location/TimeZoneSelect'
import { LocationColumn } from '@/components/location/LocationColumn'
import { addDays, layoutWeek, mondayOf, weekDates, weekRange } from '@/lib/calendarLayout'
import { BannerSummary } from '@/types/workflow'

const api = apiClient as jest.Mocked<typeof apiClient>
const lists = bannerListService as jest.Mocked<typeof bannerListService>

const banner = (overrides: Partial<BannerSummary> = {}): BannerSummary =>
  ({ id: 'b1', shopId: 's', name: 'Lunch', description: '', width: 100, height: 50, isPublished: false, componentCount: 0, createdAt: '', updatedAt: '', publishStartAt: null, publishEndAt: null, ...overrides }) as BannerSummary

beforeEach(() => {
  jest.clearAllMocks()
  mockZone = { timeZoneId: 'Asia/Kolkata', source: 'city', ownTimeZoneId: null, loaded: true }
})

describe('calendarLayout', () => {
  it('finds the Monday of a week and the seven dates', () => {
    expect(mondayOf('2030-01-07')).toBe('2030-01-07')
    expect(mondayOf('2030-01-09')).toBe('2030-01-07')
    expect(mondayOf('2030-01-13')).toBe('2030-01-07') // a Sunday belongs to the week before it ends
    expect(weekDates('2030-01-07')).toEqual(['2030-01-07', '2030-01-08', '2030-01-09', '2030-01-10', '2030-01-11', '2030-01-12', '2030-01-13'])
    expect(addDays('2030-01-31', 1)).toBe('2030-02-01')
    expect(addDays('2030-01-01', -1)).toBe('2029-12-31')
  })

  it('asks for the week as the shop clock sees it', () => {
    expect(weekRange('2030-01-07', 'Asia/Kolkata')).toEqual({ from: '2030-01-06T18:30:00.000Z', to: '2030-01-13T18:30:00.000Z' })
  })

  it('places a stretch in its day, in minutes on the shop clock', () => {
    const dates = weekDates('2030-01-07')
    const columns = layoutWeek([{ bannerId: 'b', name: 'Lunch', startUtc: '2030-01-07T06:30:00Z', endUtc: '2030-01-07T08:30:00Z', published: true }], dates, 'Asia/Kolkata')

    expect(columns['2030-01-07']).toEqual([{ bannerId: 'b', name: 'Lunch', published: true, top: 12 * 60, length: 120, continuesBefore: false, continuesAfter: false }])
    expect(columns['2030-01-08']).toEqual([])
  })

  it('cuts a stretch that crosses midnight into two days', () => {
    const dates = weekDates('2030-01-07')
    // 22:00 to 02:00 on the shop clock
    const columns = layoutWeek([{ bannerId: 'b', name: 'Night', startUtc: '2030-01-07T16:30:00Z', endUtc: '2030-01-07T20:30:00Z', published: false }], dates, 'Asia/Kolkata')

    expect(columns['2030-01-07'][0]).toMatchObject({ top: 22 * 60, length: 120, continuesAfter: true, continuesBefore: false, published: false })
    expect(columns['2030-01-08'][0]).toMatchObject({ top: 0, length: 120, continuesBefore: true, continuesAfter: false })
  })

  it('spreads a multi-day banner over each day it touches, and keeps the order', () => {
    const dates = weekDates('2030-01-07')
    const columns = layoutWeek(
      [
        { bannerId: 'b', name: 'Long', startUtc: '2030-01-07T18:30:00Z', endUtc: '2030-01-09T18:30:00Z', published: true }, // all of Tue and Wed
        { bannerId: 'a', name: 'Early', startUtc: '2030-01-08T00:30:00Z', endUtc: '2030-01-08T01:30:00Z', published: true },
      ],
      dates,
      'Asia/Kolkata'
    )

    expect(columns['2030-01-07']).toEqual([])
    expect(columns['2030-01-08'].map((b) => b.name)).toEqual(['Long', 'Early'])
    expect(columns['2030-01-08'][0]).toMatchObject({ top: 0, length: 1440 })
    expect(columns['2030-01-09'][0]).toMatchObject({ top: 0, length: 1440 })
    expect(columns['2030-01-10']).toEqual([])
  })
})

describe('SchedulePicker', () => {
  const open = async (b = banner()) => {
    render(<SchedulePicker banner={b} needsReapprovalOnChange={false} onSaved={jest.fn()} />)
    await userEvent.click(screen.getByRole('button', { name: b.publishStartAt ? 'Change schedule' : 'Set schedule' }))
  }

  it('reads the times as the shop clock and sends the UTC moments', async () => {
    lists.setSchedule.mockResolvedValue({ bannerId: 'b1', startAt: '', endAt: '', requiresReapproval: false })
    const onSaved = jest.fn()
    render(<SchedulePicker banner={banner()} needsReapprovalOnChange={false} onSaved={onSaved} />)
    await userEvent.click(screen.getByRole('button', { name: 'Set schedule' }))

    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-07T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-14T09:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))

    await waitFor(() => expect(lists.setSchedule).toHaveBeenCalledWith('b1', '2035-01-07T03:30:00.000Z', '2035-01-14T03:30:00.000Z', null))
    expect(onSaved).toHaveBeenCalledWith('Schedule saved.')
  })

  it('sends daily hours and weekdays when asked', async () => {
    lists.setSchedule.mockResolvedValue({ bannerId: 'b1', startAt: '', endAt: '', requiresReapproval: true })
    const onSaved = jest.fn()
    render(<SchedulePicker banner={banner()} needsReapprovalOnChange={false} onSaved={onSaved} />)
    await userEvent.click(screen.getByRole('button', { name: 'Set schedule' }))
    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-07T00:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-14T00:00' } })

    await userEvent.click(screen.getByLabelText('Only at certain hours each day'))
    fireEvent.change(screen.getByLabelText('From'), { target: { value: '12:00' } })
    fireEvent.change(screen.getByLabelText('Until'), { target: { value: '14:30' } })
    await userEvent.click(screen.getByRole('button', { name: 'Monday to Friday' }))
    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))

    await waitFor(() =>
      expect(lists.setSchedule).toHaveBeenCalledWith('b1', '2035-01-06T18:30:00.000Z', '2035-01-13T18:30:00.000Z', { startMinutes: 720, endMinutes: 870, days: 62 })
    )
    expect(onSaved).toHaveBeenCalledWith('Schedule saved. The banner needs to be approved again.')
  })

  it('shows what is saved in the shop clock, with the daily hours', async () => {
    await open(banner({
      publishStartAt: '2035-01-07T03:30:00Z', publishEndAt: '2035-01-14T03:30:00Z',
      dailyStartMinutes: 540, dailyEndMinutes: 720, activeDays: 62,
    }))

    expect(screen.getByLabelText('Starts')).toHaveValue('2035-01-07T09:00')
    expect(screen.getByLabelText('Ends')).toHaveValue('2035-01-14T09:00')
    expect(screen.getByLabelText('Only at certain hours each day')).toBeChecked()
    expect(screen.getByLabelText('From')).toHaveValue('09:00')
    expect(screen.getByLabelText('Until')).toHaveValue('12:00')
    expect(screen.getByLabelText('Monday')).toBeChecked()
    expect(screen.getByLabelText('Saturday')).not.toBeChecked()
    expect(screen.getByTestId('schedule-daily')).toHaveTextContent('9:00 AM – 12:00 PM, Monday to Friday')
  })

  it('says which clock the times are on, and warns when no time zone is set', async () => {
    mockZone = { timeZoneId: 'UTC', source: 'default', ownTimeZoneId: null, loaded: true }
    await open()

    expect(screen.getByTestId('schedule-zone-note')).toHaveTextContent('UTC (UTC+00:00)')
    expect(screen.getByTestId('schedule-zone-note')).toHaveTextContent('No time zone is set for the shop yet')
  })

  it('checks the dates and hours before asking the server', async () => {
    await open()
    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Choose when the banner starts')

    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-14T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-07T09:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))
    expect(await screen.findByText('The end must be after the start.')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-21T09:00' } })
    await userEvent.click(screen.getByLabelText('Only at certain hours each day'))
    fireEvent.change(screen.getByLabelText('From'), { target: { value: '10:00' } })
    fireEvent.change(screen.getByLabelText('Until'), { target: { value: '10:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))
    expect(await screen.findByText('The hours cannot start and end at the same time.')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Until'), { target: { value: '12:00' } })
    for (const day of ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']) await userEvent.click(screen.getByLabelText(day))
    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))
    expect(await screen.findByText('Choose at least one day of the week.')).toBeInTheDocument()
    expect(lists.setSchedule).not.toHaveBeenCalled()
  })

  it('explains hours that run past midnight', async () => {
    await open()
    await userEvent.click(screen.getByLabelText('Only at certain hours each day'))

    fireEvent.change(screen.getByLabelText('From'), { target: { value: '22:00' } })
    fireEvent.change(screen.getByLabelText('Until'), { target: { value: '02:00' } })

    expect(screen.getByText(/run past midnight/)).toBeInTheDocument()
  })

  it('shows the reason when the server refuses (an overlap)', async () => {
    lists.setSchedule.mockRejectedValue({ response: { data: { message: "Schedule overlaps banner 'Morning'. Choose hours that do not overlap another banner." } } })
    await open()
    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-07T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-14T09:00' } })

    await userEvent.click(screen.getByRole('button', { name: 'Save schedule' }))

    expect(await screen.findByText(/overlaps banner 'Morning'/)).toBeInTheDocument()
  })
})

describe('Schedule calendar page', () => {
  it('draws the banners of the week in the shop clock, faded when not approved', async () => {
    jest.useFakeTimers({ now: new Date('2030-01-09T05:00:00Z'), advanceTimers: true })
    lists.getCalendar.mockResolvedValue({
      timeZoneId: 'Asia/Kolkata',
      entries: [
        { bannerId: 'b1', name: 'Lunch', startUtc: '2030-01-08T06:30:00Z', endUtc: '2030-01-08T08:30:00Z', published: true },
        { bannerId: 'b2', name: 'Draft', startUtc: '2030-01-09T03:30:00Z', endUtc: '2030-01-09T05:30:00Z', published: false },
      ],
    })

    render(<CalendarPage />)

    expect(await screen.findByTestId('block-Lunch')).toBeInTheDocument()
    expect(lists.getCalendar).toHaveBeenCalledWith('2030-01-06T18:30:00.000Z', '2030-01-13T18:30:00.000Z')
    expect(within(screen.getByTestId('day-2030-01-08')).getByTestId('block-Lunch')).toHaveStyle({ top: `${12 * 28}px`, opacity: '1' })
    expect(within(screen.getByTestId('day-2030-01-09')).getByTestId('block-Draft')).toHaveStyle({ opacity: '0.55' })
    expect(screen.getByTestId('block-Draft')).toHaveAttribute('title', expect.stringContaining('not approved yet'))
    expect(screen.getByTestId('calendar-range')).toHaveTextContent('Jan 7 – Jan 13, 2030')
    jest.useRealTimers()
  })

  it('moves between weeks and says when a week is empty', async () => {
    jest.useFakeTimers({ now: new Date('2030-01-09T05:00:00Z'), advanceTimers: true })
    lists.getCalendar.mockResolvedValue({ timeZoneId: 'Asia/Kolkata', entries: [] })
    render(<CalendarPage />)
    expect(await screen.findByTestId('calendar-empty')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Next week →' }))
    await waitFor(() => expect(lists.getCalendar).toHaveBeenLastCalledWith('2030-01-13T18:30:00.000Z', '2030-01-20T18:30:00.000Z'))
    expect(screen.getByTestId('calendar-range')).toHaveTextContent('Jan 14 – Jan 20, 2030')

    await userEvent.click(screen.getByRole('button', { name: 'This week' }))
    await waitFor(() => expect(lists.getCalendar).toHaveBeenLastCalledWith('2030-01-06T18:30:00.000Z', '2030-01-13T18:30:00.000Z'))
    jest.useRealTimers()
  })

  it('shows why the calendar could not load', async () => {
    lists.getCalendar.mockRejectedValue({ response: { data: { message: 'No access' } } })
    render(<CalendarPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('No access')
  })
})

describe('TimeZoneSelect', () => {
  it('shows the chosen zone with its offset, and opens the list only when asked', async () => {
    const onChange = jest.fn()
    render(<TimeZoneSelect id="tz" label="Time zone" value="Asia/Kolkata" onChange={onChange} inheritLabel="Same as my city" />)

    expect(screen.getByTestId('tz-summary')).toHaveTextContent('Asia/Kolkata (UTC+05:30)')
    expect(screen.queryByRole('combobox')).toBeNull()

    await userEvent.click(screen.getByRole('button', { name: 'Change time zone' }))
    await userEvent.selectOptions(screen.getByLabelText('Time zone'), 'Europe/London')
    expect(onChange).toHaveBeenCalledWith('Europe/London')
  })

  it('shows what it falls back to when nothing is chosen, and can go back to the default', async () => {
    const onChange = jest.fn()
    const { rerender } = render(<TimeZoneSelect id="tz" label="Time zone" value="" onChange={onChange} inheritLabel="Same as my city (Asia/Kolkata)" />)
    expect(screen.getByTestId('tz-summary')).toHaveTextContent('Same as my city (Asia/Kolkata)')
    expect(screen.queryByRole('button', { name: 'Use the default' })).toBeNull()

    rerender(<TimeZoneSelect id="tz" label="Time zone" value="Europe/London" onChange={onChange} inheritLabel="Same as my city (Asia/Kolkata)" />)
    await userEvent.click(screen.getByRole('button', { name: 'Use the default' }))
    expect(onChange).toHaveBeenCalledWith('')
  })

  it('offers modern names, such as Asia/Kolkata and not only Asia/Calcutta', async () => {
    render(<TimeZoneSelect id="tz" label="Time zone" value="" onChange={jest.fn()} inheritLabel="Not set" alwaysOpen />)

    expect(screen.getByRole('option', { name: 'Asia/Kolkata' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Asia/Calcutta' })).toBeNull()
  })
})

describe('Places: time zone of a city', () => {
  const item = { id: '7', uniqueId: 'CTY-AAAA-BBBB', name: 'Mumbai', code: null, parentId: '5', isActive: true, childCount: 0, shopCount: 0, timeZoneId: 'Asia/Kolkata' }
  const props = { title: 'Cities', noun: 'City', addKind: 'name' as const, selectedId: null, busy: false, onSelect: jest.fn(), onAdd: jest.fn(), onRename: jest.fn(), onToggle: jest.fn(), onDelete: jest.fn() }

  it('shows it, and changes it', async () => {
    const onSetTimeZone = jest.fn().mockResolvedValue(true)
    render(<LocationColumn {...props} items={[item, { ...item, id: '8', name: 'Pune', timeZoneId: null }]} onSetTimeZone={onSetTimeZone} />)

    expect(screen.getByTestId('city-Mumbai')).toHaveTextContent('Time zone: Asia/Kolkata')
    expect(screen.getByTestId('city-Pune')).toHaveTextContent('No time zone set')

    await userEvent.click(within(screen.getByTestId('city-Pune')).getByRole('button', { name: 'Time zone' }))
    await userEvent.selectOptions(screen.getByLabelText('Time zone of Pune'), 'Asia/Kolkata')
    await userEvent.click(screen.getByRole('button', { name: 'Save time zone' }))

    await waitFor(() => expect(onSetTimeZone).toHaveBeenCalledWith(expect.objectContaining({ id: '8' }), 'Asia/Kolkata'))
  })

  it('has no time zone controls for levels that do not carry one', () => {
    render(<LocationColumn {...props} items={[item]} />)

    expect(screen.queryByRole('button', { name: 'Time zone' })).toBeNull()
    expect(screen.queryByTestId('item-time-zone')).toBeNull()
  })
})

describe('DefaultBoard', () => {
  it('is the standard board without a design', () => {
    render(<DefaultBoard shopName="Olive Mart" frozenTime={new Date(2030, 0, 7, 10, 30)} />)

    expect(screen.getByText('Olive Mart')).toBeInTheDocument()
    expect(screen.queryByTestId('default-board-message')).toBeNull()
    expect(screen.queryByTestId('default-board-logo')).toBeNull()
    expect(screen.getByTestId('default-board').className).toContain('bg-gradient')
  })

  it('shows the shop message, logo and colours', () => {
    render(<DefaultBoard shopName="Olive Mart" frozenTime={new Date(2030, 0, 7, 10, 30)} look={{ message: 'Back at 2 pm', background: '#112233', textColor: '#ffeeaa', logoUrl: 'http://x/logo.png' }} />)

    const board = screen.getByTestId('default-board')
    expect(board.style.background).toContain('rgb(17, 34, 51)')
    expect(board.style.color).toBe('rgb(255, 238, 170)')
    expect(board.className).not.toContain('bg-gradient')
    expect(screen.getByTestId('default-board-message')).toHaveTextContent('Back at 2 pm')
    expect(screen.getByTestId('default-board-logo')).toHaveAttribute('src', 'http://x/logo.png')
  })
})

describe('Default board designer', () => {
  const board = { message: 'Open 9 to 9', background: null, textColor: null, logoMediaFileId: null, logoUrl: null, shopName: 'Olive Mart' }

  it('loads the saved design and shows it in the preview', async () => {
    api.get.mockResolvedValue(board)
    render(<DefaultBoardPage />)

    expect(await screen.findByLabelText('Message')).toHaveValue('Open 9 to 9')
    expect(within(screen.getByTestId('board-preview')).getByTestId('default-board-message')).toHaveTextContent('Open 9 to 9')
    expect(screen.getByLabelText('Use my own colours')).not.toBeChecked()
  })

  it('saves the message, colours and logo', async () => {
    api.get.mockResolvedValue(board)
    api.put.mockResolvedValue({ ...board, message: 'Back at 2 pm', background: '#112233', textColor: '#ffffff', logoMediaFileId: 'logo-1', logoUrl: '/api/media/logo-1/download?x=1' })
    render(<DefaultBoardPage />)
    await screen.findByLabelText('Message')

    await userEvent.clear(screen.getByLabelText('Message'))
    await userEvent.type(screen.getByLabelText('Message'), 'Back at 2 pm')
    await userEvent.click(screen.getByLabelText('Use my own colours'))
    fireEvent.change(screen.getByLabelText('Background'), { target: { value: '#112233' } })
    await userEvent.click(screen.getByRole('button', { name: 'Choose a logo from my files' }))
    await userEvent.click(screen.getByText('pick logo'))
    expect(within(screen.getByTestId('board-preview')).getByTestId('default-board-logo')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Save default board' }))

    await waitFor(() =>
      expect(api.put).toHaveBeenCalledWith('/shops/shop-1/default-board', { message: 'Back at 2 pm', background: '#112233', textColor: '#ffffff', logoMediaFileId: 'logo-1' })
    )
    expect(await screen.findByText(/default board was saved/)).toBeInTheDocument()
  })

  it('sends nothing for the colours or logo when they are switched off or removed', async () => {
    api.get.mockResolvedValue({ ...board, background: '#112233', textColor: '#ffffff', logoMediaFileId: 'logo-1', logoUrl: 'http://x/logo.png' })
    api.put.mockResolvedValue(board)
    render(<DefaultBoardPage />)
    await screen.findByLabelText('Message')

    await userEvent.click(screen.getByLabelText('Use my own colours'))
    await userEvent.click(screen.getByRole('button', { name: 'Remove logo' }))
    await userEvent.click(screen.getByRole('button', { name: 'Save default board' }))

    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/shops/shop-1/default-board', { message: 'Open 9 to 9', background: null, textColor: null, logoMediaFileId: null }))
  })

  it('shows why a save was refused', async () => {
    api.get.mockResolvedValue(board)
    api.put.mockRejectedValue({ response: { data: { message: 'The logo must be one of your uploaded pictures.' } } })
    render(<DefaultBoardPage />)
    await screen.findByLabelText('Message')

    await userEvent.click(screen.getByRole('button', { name: 'Save default board' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('The logo must be one of your uploaded pictures.')
  })

  it('turns a link to the logo into a full address on the API', async () => {
    api.get.mockResolvedValue({ ...board, logoMediaFileId: 'l', logoUrl: '/api/media/l/download?expires=1&sig=x' })

    const loaded = await defaultBoardService.get('shop-1')

    expect(loaded.logoUrl).toBe('http://api.test/api/media/l/download?expires=1&sig=x')
  })
})
