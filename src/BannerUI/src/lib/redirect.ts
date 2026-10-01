/** Only follow return addresses inside this app, never another site. */
export function safeReturnUrl(value: string | null | undefined, fallback = '/dashboard'): string {
  if (!value) return fallback
  if (!value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return fallback
  if (value.startsWith('/login') || value.startsWith('/register')) return fallback
  return value
}
