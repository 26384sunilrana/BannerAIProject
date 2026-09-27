/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          primary: '#2563eb',
          secondary: '#1e40af',
          accent: '#f59e0b',
        },
      },
      spacing: {
        '128': '32rem',
      },
    },
  },
  plugins: [],
}
