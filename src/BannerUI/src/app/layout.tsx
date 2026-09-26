import type { Metadata } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'Banner Editor',
  description: 'Create and edit banners with drag-and-drop components',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  )
}
